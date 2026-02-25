using System;
using System.Threading.Tasks;

namespace Privy
{
    internal interface IAuthDelegator
    {
        void SetAuthStateChangeCallback(Action<AuthState> callback);
        Task<bool> SendEmailCode(string email);
        Task<AuthState> LoginWithEmailCode(string email, string code);

        Task<string> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge, string redirectUri,
            string stateCode);

        Task<AuthState> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier, string stateCode,
            bool isRawFlow = false);

        Task<bool> SendSmsCode(string phoneNumber);
        Task<AuthState> LoginWithSmsCode(string phoneNumber, string code);
        Task<InternalPrivyUser> LinkSms(string phoneNumber, string code);
        Task<InternalPrivyUser> UnlinkSms(string phoneNumber);
        Task UpdateSmsPhoneNumber(string phoneNumber, string code);

        Task RestoreSession(); //Temp, should be private
        void Logout();
    }
}
