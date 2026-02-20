using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Privy
{
    internal class OAuthExternalBrowserFlow : IOAuthFlow
    {
        private TaskCompletionSource<OAuthResultData> _oauthFlowTaskSource;

        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            PrivyLogger.Debug("Performing OAuth flow");

            _oauthFlowTaskSource = new TaskCompletionSource<OAuthResultData>();

            Application.deepLinkActivated += OnDeepLinkActivated;

            try
            {
                PrivyLogger.Debug($"Attempting to open url: {oAuthUrl}");
                Application.OpenURL(oAuthUrl);
                return await _oauthFlowTaskSource.Task;
            }
            finally
            {
                Application.deepLinkActivated -= OnDeepLinkActivated;
            }
        }

        private void OnDeepLinkActivated(string url)
        {
            PrivyLogger.Debug($"Deeplink activated w/ url: {url}");
            var uri = new Uri(url);
            var result = OAuthResultData.ParseFromUri(uri);
            _oauthFlowTaskSource.SetResult(result);
        }
    }
}
