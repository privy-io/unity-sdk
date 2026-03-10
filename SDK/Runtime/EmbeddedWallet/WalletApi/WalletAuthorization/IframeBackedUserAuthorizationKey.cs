using System.Threading.Tasks;
using Privy.Auth;

namespace Privy.Wallets
{
    /// <summary>
    /// A user's authorization key backed by the embedded iframe
    /// </summary>
    internal class IframeBackedUserAuthorizationKey : IAuthorizationKey
    {
        private readonly EmbeddedWalletManager _embeddedWalletManager;
        private readonly Privy.Auth.AuthDelegator _authDelegator;

        internal IframeBackedUserAuthorizationKey(EmbeddedWalletManager embeddedWalletManager,
            Privy.Auth.AuthDelegator authDelegator)
        {
            _embeddedWalletManager = embeddedWalletManager;
            _authDelegator = authDelegator;
        }

        async Task<byte[]> IAuthorizationKey.Signature(byte[] message)
        {
            string token = await _authDelegator.GetAccessToken();

            return await _embeddedWalletManager.SignWithUserSigner(token, message);
        }
    }
}
