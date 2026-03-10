using Privy.Utils;
using System.Threading.Tasks;

namespace Privy.Auth.OAuth
{
    internal class NativeAppleSignInFlow
    {
        public async Task<OAuthResultData> PerformFlow(string stateCode)
        {
            var taskSource = new TaskCompletionSource<OAuthResultData>();
            using var appleIDProvider = new ASAuthorizationAppleIDProvider((credentials, error) =>
            {
                if (error != null && error.Code != 0)
                {
                    taskSource.SetException(new PrivyAuthenticationException(error.Message,
                        AuthenticationError.OAuthVerificationFailed));
                    return;
                }

                taskSource.SetResult(new OAuthResultData
                { OAuthState = credentials.State, OAuthCode = credentials.AuthorizationCode });
            });
            appleIDProvider.RequestAppleID(stateCode);
            return await taskSource.Task;
        }
    }
}
