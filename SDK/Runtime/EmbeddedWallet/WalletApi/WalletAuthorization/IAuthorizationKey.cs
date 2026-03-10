using System.Threading.Tasks;

namespace Privy.Wallets
{
    /// <summary>
    /// Keys used for authorizing requests to the Wallet API.
    /// </summary>
    internal interface IAuthorizationKey
    {
        /// <summary>
        /// Signs a byte sequence for authorization.
        /// </summary>
        /// <param name="message">The byte sequence to sign over</param>
        /// <returns>The signature of the message</returns>
        internal Task<byte[]> Signature(byte[] message);
    }
}
