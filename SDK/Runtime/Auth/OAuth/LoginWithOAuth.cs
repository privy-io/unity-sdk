using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Privy
{
    internal class LoginWithOAuth : ILoginWithOAuth
    {
        private readonly IAuthDelegator _authDelegator;
        private readonly IOAuthFlow _oAuthFlow = IOAuthFlow.GetPlatformOAuthFlow();

        public LoginWithOAuth(IAuthDelegator authDelegator)
        {
            _authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
        }

        public async Task<AuthState> LoginWithProvider(OAuthProvider provider, string redirectUri)
        {
            var (codeChallenge, codeVerifier) = PKCE.Generate();
            var stateCode = PKCE.GenerateStateCode();

            var result = await PromptOAuthCredentials(provider, redirectUri, codeChallenge, stateCode);

            return await _authDelegator.AuthenticateOAuthFlow(
                result.OAuthCode,
                codeVerifier,
                result.OAuthState,
                // Native apple login is a 'raw' flow per Privy API (code is unhashed)
                isRawFlow: IsNativeAppleFlow(provider)
            );
        }

        private async Task<OAuthResultData> PromptOAuthCredentials(OAuthProvider provider, string redirectUri,
            string codeChallenge, string stateCode)
        {
            var transformedRedirectUri = _oAuthFlow.TransformRedirectUrl(redirectUri);
            var oauthUrl =
                await _authDelegator.InitiateOAuthFlow(provider, codeChallenge, transformedRedirectUri, stateCode);

            if (IsNativeAppleFlow(provider))
                return await new NativeAppleSignInFlow().PerformFlow(stateCode);

            return await _oAuthFlow.PerformOAuthFlow(oauthUrl, redirectUri);
        }

        private static bool IsNativeAppleFlow(OAuthProvider provider)
        {
            return provider == OAuthProvider.Apple && Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }
}
