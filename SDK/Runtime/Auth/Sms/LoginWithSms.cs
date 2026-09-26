using System;
using System.Threading.Tasks;
using Privy.Core;

namespace Privy.Auth.Sms
{
    internal class LoginWithSms : ILoginWithSms
    {
        private IAuthDelegator _authDelegator;
        private IPrivy _privy;

        public LoginWithSms(IAuthDelegator authDelegator, IPrivy privy)
        {
            _authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
            _privy = privy ?? throw new ArgumentNullException(nameof(privy));
        }

        public async Task<bool> SendCode(string phoneNumber)
        {
            return await _authDelegator.SendSmsCode(phoneNumber);
        }

        public async Task<AuthState> LoginWithCode(string phoneNumber, string code)
        {
            return await _authDelegator.LoginWithSmsCode(phoneNumber, code);
        }

        public async Task<IPrivyUser> Link(string phoneNumber, string code)
        {
            await _authDelegator.LinkSms(phoneNumber, code);
            return await _privy.GetUser();
        }

        public async Task<IPrivyUser> Unlink(string phoneNumber)
        {
            await _authDelegator.UnlinkSms(phoneNumber);
            return await _privy.GetUser();
        }

        public async Task<IPrivyUser> UpdatePhoneNumber(string phoneNumber, string code)
        {
            await _authDelegator.UpdateSmsPhoneNumber(phoneNumber, code);
            return await _privy.GetUser();
        }
    }
}
