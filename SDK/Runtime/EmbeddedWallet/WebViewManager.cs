using System;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Privy.Config;
using Privy.Utils;

namespace Privy.Wallets
{
    internal class WebViewManager : IDisposable
    {
        private readonly IWebViewHandler _webViewHandler;
        private TaskCompletionSource<bool> _readyTcs = new TaskCompletionSource<bool>();
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        private bool _disposed;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _requestResponseMap =
            new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public WebViewManager(PrivyConfig privyConfig)
        {
            _webViewHandler = IWebViewHandler.GetPlatformWebViewHandler(this, privyConfig);

            _webViewHandler.LoadUrl(
                $"{PrivyEnvironment.BASE_URL}/apps/{privyConfig.AppId}/embedded-wallets?client_id={privyConfig.ClientId}");
        }

        internal void OnWebViewReady()
        {
            PrivyLogger.Debug("WebView is ready.");
            if (!_readyTcs.Task.IsCompleted)
            {
                _readyTcs.TrySetResult(true);
            }
        }

        // Handle messages received from the WebView
        internal void OnMessageReceived(string message)
        {
            PrivyLogger.Debug("Message from WebView: " + message);

            // Check for URL-encoding. Messages that are not raw JSON (don't start with '{')
            // and contain '%' characters are assumed to be URL-encoded (e.g. on Android).
            if (!message.TrimStart().StartsWith("{") && message.Contains("%"))
            {
                message = Uri.UnescapeDataString(message);
                PrivyLogger.Debug("Decoded message from WebView: " + message);
            }

            var messageResponse = JsonConvert.DeserializeObject<IframeResponse>(message);
            string id = messageResponse.Id;

            if (_requestResponseMap.TryGetValue(id, out var tcs))
            {
                tcs.TrySetResult(message);
                _requestResponseMap.TryRemove(id, out _);
            }
            else
            {
                // This would happen if the request already timed out before the response arrived.
                PrivyLogger.Error($"No matching task found for ID: {id}");
            }
        }

        private async Task<IframeResponse> SendRequest<TReq, TRes>(IframeRequest<TReq> request, double seconds = 30)
        {
            if (request.Event != PrivyEventType.Ready)
            {
                // For all requests except "ping ready", wait until the WebView is ready.
                await AwaitReady();
            }

            var tcs = new TaskCompletionSource<string>();
            _requestResponseMap[request.Id] = tcs;

            // Log only non-sensitive fields to avoid leaking access tokens in debug output.
            PrivyLogger.Debug($"Message to WebView: event={request.Event}, id={request.Id}");

            _webViewHandler.SendMessage(JsonConvert.SerializeObject(request));

            // Link the timeout to the dispose token so disposal cancels in-flight requests.
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(seconds), _disposeCts.Token);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _requestResponseMap.TryRemove(request.Id, out _);
                if (timeoutTask.IsCanceled || _disposeCts.IsCancellationRequested)
                {
                    throw new ObjectDisposedException(nameof(WebViewManager));
                }
                PrivyLogger.Error("Request timed out.");
                return new IframeResponseError
                {
                    Id = request.Id,
                    Event = request.Event,
                    Error = new ErrorDetails
                    {
                        Type = "timeout_error",
                        Message = "The request timed out."
                    }
                };
            }

            string responseString = await tcs.Task;
            var baseResponse = JsonConvert.DeserializeObject<IframeResponse>(responseString);

            // Use a structural check on the 'event' field instead of string.Contains("error"),
            // which would misfire on any response that happens to contain the word "error".
            if (baseResponse.Event == "error")
            {
                return JsonConvert.DeserializeObject<IframeResponseError>(responseString);
            }

            return JsonConvert.DeserializeObject<IframeResponseSuccess<TRes>>(responseString);
        }

        // Generic helper that builds and sends a typed request, handling the response pattern
        // common to all wallet methods: success → return, error → return, unexpected → throw.
        private async Task<IframeResponse> SendTypedRequest<TReq, TRes>(
            string eventType, TReq data, EmbeddedWalletError fallbackError, double timeout = 30)
        {
            var request = new IframeRequest<TReq>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = eventType,
                Data = data
            };
            var res = await SendRequest<TReq, TRes>(request, timeout);
            if (res is IframeResponseSuccess<TRes> || res is IframeResponseError)
            {
                return res;
            }
            throw new PrivyWalletException("Received an unexpected response", fallbackError);
        }

        internal async Task PingReadyUntilSuccessful()
        {
            // NOTE: Semaphore has been removed here, as this is only called once, and we want to
            // prevent WebGL issues since WebGL does not support multi-threading.
            // We use a TaskCompletionSource to know when the WebView is ready.
            if (_readyTcs.Task.IsCompleted)
            {
                PrivyLogger.Debug("Already ready");
                return;
            }

            PrivyLogger.Debug("Ping Ready until success invoked");

            while (!_readyTcs.Task.IsCompleted && !_disposeCts.IsCancellationRequested)
            {
                PrivyLogger.Debug("NOT COMPLETED");
                try
                {
                    var result = await PingReady();
                    if (result is IframeResponseSuccess<ReadyResponseData>)
                    {
                        PrivyLogger.Debug("Success Response Received: WebView is ready.");
                        _readyTcs.TrySetResult(true);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Dispose was requested; propagate so caller can observe
                    throw;
                }
                catch (Exception)
                {
                    // Fail silently so the loop continues.
                    PrivyLogger.Debug("Success response not received, pinging again");
                }

                if (!_readyTcs.Task.IsCompleted && !_disposeCts.IsCancellationRequested)
                {
                    await Task.Delay(100, _disposeCts.Token);
                }
            }

            PrivyLogger.Debug("Task completed");
        }

        private async Task<IframeResponse> PingReady()
        {
            var readyRequest = new IframeRequest<ReadyRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Ready,
                Data = new ReadyRequestData()
            };

            var res = await SendRequest<ReadyRequestData, ReadyResponseData>(readyRequest, 0.15);

            if (res is IframeResponseSuccess<ReadyResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                // Triggered on timeouts or error responses; the loop will re-trigger.
                return errorResponse;
            }
            else
            {
                return null;
            }
        }

        private async Task AwaitReady()
        {
            await _readyTcs.Task;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var kvp in _requestResponseMap)
            {
                kvp.Value.TrySetCanceled();
            }
            _requestResponseMap.Clear();

            _disposeCts.Cancel();
            _disposeCts.Dispose();

            if (!_readyTcs.Task.IsCompleted)
            {
                _readyTcs.TrySetCanceled();
            }
        }

        // Wallet Methods

        internal Task<IframeResponse> CreateEthereumWallet(string accessToken, string solanaAddress = null)
        {
            return SendTypedRequest<CreateEthereumWalletRequestData, CreateEthereumWalletResponseData>(
                PrivyEventType.CreateEthereumWallet,
                new CreateEthereumWalletRequestData { AccessToken = accessToken, SolanaAddress = solanaAddress },
                EmbeddedWalletError.CreateFailed,
                60);
        }

        internal Task<IframeResponse> CreateSolanaWallet(string accessToken, string ethereumAddress = null)
        {
            return SendTypedRequest<CreateSolanaWalletRequestData, CreateSolanaWalletResponseData>(
                PrivyEventType.CreateSolanaWallet,
                new CreateSolanaWalletRequestData { AccessToken = accessToken, EthereumAddress = ethereumAddress },
                EmbeddedWalletError.CreateFailed,
                60);
        }

        internal Task<IframeResponse> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex)
        {
            return SendTypedRequest<CreateAdditionalWalletRequestData, CreateAdditionalWalletResponseData>(
                PrivyEventType.CreateAdditional,
                new CreateAdditionalWalletRequestData
                {
                    AccessToken = accessToken,
                    ChainType = chainType,
                    EntropyId = walletEntropy.Id,
                    EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                    WalletIndex = hdWalletIndex
                },
                EmbeddedWalletError.CreateAdditionalFailed,
                60);
        }

        internal Task<IframeResponse> ConnectWallet(string accessToken, WalletEntropy walletEntropy)
        {
            return SendTypedRequest<ConnectWalletRequestData, ConnectWalletResponseData>(
                PrivyEventType.Connect,
                new ConnectWalletRequestData
                {
                    AccessToken = accessToken,
                    ChainType = ChainType.Ethereum,
                    EntropyId = walletEntropy.Id,
                    EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
                },
                EmbeddedWalletError.ConnectionFailed);
        }

        internal Task<IframeResponse> RecoverWallet(string accessToken, WalletEntropy walletEntropy)
        {
            return SendTypedRequest<RecoverWalletRequestData, RecoverWalletResponseData>(
                PrivyEventType.Recover,
                new RecoverWalletRequestData
                {
                    AccessToken = accessToken,
                    EntropyId = walletEntropy.Id,
                    EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
                },
                EmbeddedWalletError.RecoverFailed);
        }

        internal Task<IframeResponse> Request(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            return SendTypedRequest<RpcRequestData, RpcResponseData>(
                PrivyEventType.Rpc,
                new RpcRequestData
                {
                    AccessToken = accessToken,
                    ChainType = chainType,
                    EntropyId = walletEntropy.Id,
                    EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                    WalletIndex = hdWalletIndex,
                    Request = request
                },
                EmbeddedWalletError.RpcRequestFailed);
        }

        internal Task<IframeResponse> SignWithUserSigner(string accessToken, byte[] message)
        {
            return SendTypedRequest<UserSignerSignRequestData, UserSignerSignResponseData>(
                PrivyEventType.SignWithUserSigner,
                new UserSignerSignRequestData
                {
                    AccessToken = accessToken,
                    Message = Convert.ToBase64String(message)
                },
                EmbeddedWalletError.UserSignerRequestFailed);
        }
    }
}
