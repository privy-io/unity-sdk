using System;
using System.Threading.Tasks;

namespace Privy.Auth.Sms
{
    internal class LoginWithSms : ILoginWithSms
    {
        private IAuthDelegator _authDelegator;
        private Func<IPrivyUser> _getUser;

        public LoginWithSms(IAuthDelegator authDelegator, Func<IPrivyUser> getUser)
        {
            _authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
            _getUser = getUser ?? throw new ArgumentNullException(nameof(getUser));
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
            return _getUser();
        }

        public async Task<IPrivyUser> Unlink(string phoneNumber)
        {
            await _authDelegator.UnlinkSms(phoneNumber);
            return _getUser();
        }

        public async Task<IPrivyUser> UpdatePhoneNumber(string phoneNumber, string code)
        {
            await _authDelegator.UpdateSmsPhoneNumber(phoneNumber, code);
            return _getUser();
        }
    }
}
