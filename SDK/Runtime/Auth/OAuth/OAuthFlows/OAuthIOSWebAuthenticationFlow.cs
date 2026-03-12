using Privy.Utils;
using System;
using System.Threading.Tasks;

namespace Privy.Auth.OAuth
{
    internal class OAuthIOSWebAuthenticationFlow : IOAuthFlow
    {
        public async Task<OAuthResultData> PerformOAuthFlow(string oAuthUrl, string redirectUri)
        {
            var oauthFlowTaskSource = new TaskCompletionSource<OAuthResultData>();

            var redirectScheme = new Uri(redirectUri).Scheme;

            using var authSession = new ASWebAuthenticationSession(oAuthUrl, redirectScheme,
                (uri, error) =>
                {
                    if (error != null)
                    {
                        oauthFlowTaskSource.SetException(new PrivyAuthenticationException(error.Message,
                            AuthenticationError.OAuthVerificationFailed));
                        return;
                    }

                    var result = OAuthResultData.ParseFromUri(uri);
                    oauthFlowTaskSource.SetResult(result);
                });

            authSession.Start();
            return await oauthFlowTaskSource.Task;
        }
    }
}
