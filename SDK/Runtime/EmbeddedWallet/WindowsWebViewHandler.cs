using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Privy.Auth.OAuth;
using Privy.Utils;

namespace Privy.Wallets
{
    internal class WindowsWebViewHandler : IWebViewHandler, IDisposable
    {
        private readonly WebViewManager _webViewManager;
        private bool _disposed;

        private static TaskCompletionSource<OAuthResultData> _oauthTcs;
        private static readonly object _oauthLock = new object();
        private static string _oauthRedirectUri;

        internal static bool IsOAuthFlowActive => _oauthTcs != null;

        // Delegates for native callbacks
        private delegate void NativeMessageCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string message);
        private delegate void NativeStatusCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string message);

        // Keep delegates alive to avoid GC
        private readonly NativeMessageCallback _onWalletMessageReceived;
        private readonly NativeMessageCallback _onOAuthMessageReceived;
        private readonly NativeStatusCallback _onPageLoaded;
        private readonly NativeStatusCallback _onError;

        internal WindowsWebViewHandler(WebViewManager webViewManager = null)
        {
            _webViewManager = webViewManager;

            _onWalletMessageReceived = OnWalletMessageReceived;
            _onOAuthMessageReceived = OnOAuthMessageReceived;
            _onPageLoaded = OnPageLoaded;
            _onError = OnError;

            // Initialize native WebView2 plugin (two isolated webviews)
            PrivyLogger.Debug("Initializing Windows WebView handler");
            PrivyWebView_Wallet_Initialize(_onWalletMessageReceived, _onPageLoaded, _onError);
            PrivyWebView_OAuth_Initialize(_onOAuthMessageReceived, _onPageLoaded, _onError);
        }

        internal async static Task<OAuthResultData> RunOAuthFlow(string url, string redirectUri, TimeSpan timeout)
        {
            // Create the TaskCompletionSource in a local variable so we can safely reference it
            // even if _oauthTcs is cleared by the completion path.
            var tcs = new TaskCompletionSource<OAuthResultData>();

            lock (_oauthLock)
            {
                if (_oauthTcs != null)
                {
                    throw new InvalidOperationException("An OAuth flow is already in progress.");
                }

                _oauthTcs = tcs;
                _oauthRedirectUri = redirectUri;
            }

            // Configure redirect URI for native callback interception.
            PrivyWebView_OAuth_SetRedirectUrl(redirectUri);

            // Show window only for OAuth flows.
            PrivyWebView_OAuth_ShowWindow();
            // Load the URL into the OAuth WebView (will be queued if WebView isn't ready)
            PrivyWebView_OAuth_LoadUrl(url);

            // Timeout safety
            var cancellation = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(tcs.Task, cancellation);

            // Only clear the shared reference if it still points at our tcs.
            lock (_oauthLock)
            {
                if (_oauthTcs == tcs)
                {
                    _oauthTcs = null;
                }
            }

            if (completedTask == cancellation)
            {
                tcs.TrySetException(new TimeoutException("OAuth flow timed out."));
            }

            return await tcs.Task;
        }

        public void LoadUrl(string url)
        {
            PrivyWebView_Wallet_LoadUrl(url);
        }

        public void SendMessage(string message)
        {
            // Dispatch a message into the WebView using same mechanism as other handlers.
            string jsDispatchEvent = $@"
                window.dispatchEvent(new MessageEvent('message', {{ data: {message} }}));
            ";
            PrivyWebView_Wallet_EvaluateJS(jsDispatchEvent);
        }

        private void OnPageLoaded(string url)
        {
            PrivyLogger.Debug($"Loaded URL: {url}");

            // Only attempt to ping the embedded wallet when we are on the embedded wallet host page.
            // Once the flow navigates to external sites (Google login, etc.), those pages will not respond to our ping.
            if (!url.Contains("/embedded-wallets"))
            {
                PrivyLogger.Debug("Page load is not the embedded wallet page; skipping ready ping.");
                return;
            }

            // Inject JS proxy to enable embedded wallet communication.
            var js = @"
                window.PRIVY_UNITY = true;

                window.UnityProxy = {
                    postMessage: function(message) {
                        window.chrome.webview.postMessage(message);
                    }
                };
            ";

            PrivyWebView_Wallet_EvaluateJS(js);
            _ =_webViewManager.PingReadyUntilSuccessful();
        }

        private void OnWalletMessageReceived(string message)
        {
            _webViewManager?.OnMessageReceived(message);
        }

        private void OnOAuthMessageReceived(string message)
        {
            if (!IsOAuthFlowActive)
            {
                PrivyLogger.Debug($"OAuth message received while no flow active, ignoring: {message}");
                return;
            }

            // Match configured redirect URI
            if (!string.IsNullOrEmpty(_oauthRedirectUri) &&
                message.StartsWith(_oauthRedirectUri, StringComparison.OrdinalIgnoreCase))
            {
                PrivyLogger.Debug($"OAuth redirect intercepted: {message}");

                try
                {
                    var uri = new Uri(message);
                    var result = OAuthResultData.ParseFromUri(uri);

                    if (string.IsNullOrEmpty(result?.OAuthCode))
                    {
                        PrivyLogger.Warning($"OAuth redirect received without authorization code: {uri.Query}");
                        lock (_oauthLock)
                        {
                            _oauthTcs?.TrySetException(
                                new PrivyAuthenticationException(
                                    "OAuth redirect received without authorization code. The user may already be logged in with a session that could not be restored.",
                                    AuthenticationError.OAuthVerificationFailed));
                            _oauthTcs = null;
                            _oauthRedirectUri = null;
                        }
                        return;
                    }

                    lock (_oauthLock)
                    {
                        _oauthTcs?.TrySetResult(result);
                        _oauthTcs = null;
                        _oauthRedirectUri = null;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    PrivyLogger.Error($"OAuth redirect parse failed: {ex}");
                    lock (_oauthLock)
                    {
                        _oauthTcs?.TrySetException(ex);
                        _oauthTcs = null;
                        _oauthRedirectUri = null;
                    }
                    return;
                }
            }

            // If this is a URL from the OAuth webflow and does not contain code, ignore it to prevent JSON parsing crashes.
            if (Uri.IsWellFormedUriString(message, UriKind.Absolute))
            {
                PrivyLogger.Debug($"OAuth flow URL ignored in message handler: {message}");
                return;
            }

            PrivyLogger.Debug($"Non-URL OAuth message received, ignoring: {message}");
        }

        private void OnError(string message)
        {
            if (message == "WEBVIEW_WINDOW_CLOSED")
            {
                lock (_oauthLock)
                {
                    if (_oauthTcs != null)
                    {
                        _oauthTcs.TrySetCanceled();
                        _oauthTcs = null;
                        _oauthRedirectUri = null;
                    }
                }

                PrivyLogger.Info("Windows WebView OAuth flow canceled by user closing window.");
                return;
            }

            PrivyLogger.Error($"Windows WebView error: {message}");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            PrivyWebView_Wallet_Destroy();
            PrivyWebView_OAuth_Destroy();
        }

        #region Native Plugin Imports

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Wallet WebView (hidden, persistent — hosts embedded wallet iframe)
        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_Wallet_Initialize(NativeMessageCallback onMessage,
            NativeStatusCallback onLoaded,
            NativeStatusCallback onError);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_Wallet_LoadUrl([MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_Wallet_EvaluateJS([MarshalAs(UnmanagedType.LPUTF8Str)] string js);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_Wallet_Destroy();

        // OAuth WebView (visible, transient — OAuth consent screens)
        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_Initialize(NativeMessageCallback onMessage,
            NativeStatusCallback onLoaded,
            NativeStatusCallback onError);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_SetRedirectUrl([MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_LoadUrl([MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_ShowWindow();

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_HideWindow();

        [DllImport("PrivyWebView", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrivyWebView_OAuth_Destroy();
#else
        private static void PrivyWebView_Wallet_Initialize(NativeMessageCallback onMessage,
            NativeStatusCallback onLoaded,
            NativeStatusCallback onError)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_Wallet_LoadUrl(string url)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_Wallet_EvaluateJS(string js)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_Wallet_Destroy()
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_Initialize(NativeMessageCallback onMessage,
            NativeStatusCallback onLoaded,
            NativeStatusCallback onError)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_SetRedirectUrl(string url)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_LoadUrl(string url)
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_ShowWindow()
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_HideWindow()
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }

        private static void PrivyWebView_OAuth_Destroy()
        {
            throw new NotImplementedException("Windows WebView is only supported on Windows.");
        }
#endif

        #endregion
    }
}
