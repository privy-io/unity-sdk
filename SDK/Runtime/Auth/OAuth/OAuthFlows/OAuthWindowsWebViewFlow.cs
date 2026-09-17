using Privy.Wallets;
using System;
using System.Threading.Tasks;

namespace Privy.Auth.OAuth
{
    internal class OAuthWindowsWebViewFlow : IOAuthFlow
    {
        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            // The Windows embedded WebView handler intercepts the redirect URL and completes the flow.
            // We pass the redirect URI so the handler can detect OAuth callback hits.
            // We still pass the redirect URI through to server side via the init call.

            // Timeout set to 5 minutes to match other flows.
            var result = await WindowsWebViewHandler.RunOAuthFlow(oAuthUrl, redirectUri, TimeSpan.FromMinutes(5));
            return result;
        }
    }
}
