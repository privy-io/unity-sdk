using System.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Privy.Auth.OAuth
{
    internal interface IOAuthFlow
    {
        Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri);

        string TransformRedirectUrl(string redirectUrl) => redirectUrl;

        internal static IOAuthFlow GetPlatformOAuthFlow()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return new OAuthIOSWebAuthenticationFlow();
                case RuntimePlatform.WebGLPlayer:
                    return new OAuthWebGLPopupFlow();
                // For Windows (Editor or standalone), use the embedded WebView flow.
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return new OAuthWindowsWebViewFlow();
                // For other desktop platforms, use local HTTP listener (fallback)
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return new OAuthInEditorFlow();
                default:
                    return new OAuthExternalBrowserFlow();
            }
        }
    }
}
