using System;
using System.Threading.Tasks;

namespace Privy.Auth.Sms
{
    internal class LoginWithSms : ILoginWithSms
    {
        private IAuthDelegator _authDelegator;

        public LoginWithSms(IAuthDelegator authDelegator)
        {
            _authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
        }

        public async Task<bool> SendCode(string phoneNumber)
        {
            return await _authDelegator.SendSmsCode(phoneNumber);
        }

        public async Task<AuthState> LoginWithCode(string phoneNumber, string code)
        {
            return await _authDelegator.LoginWithSmsCode(phoneNumber, code);
        }

        public async Task Link(string phoneNumber, string code)
        {
            await _authDelegator.LinkSms(phoneNumber, code);
        }

        public async Task Unlink(string phoneNumber)
        {
            await _authDelegator.UnlinkSms(phoneNumber);
        }

        public async Task UpdatePhoneNumber(string phoneNumber, string code)
        {
            await _authDelegator.UpdateSmsPhoneNumber(phoneNumber, code);
        }
    }
}
