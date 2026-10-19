using System;

namespace Privy.Wallets
{
    /// <summary>
    /// Represents an embedded Ethereum wallet within the Privy SDK.
    /// Provides access to wallet details and an RPC provider for blockchain interactions.
    /// </summary>
    public interface IEmbeddedEthereumWallet
    {
        string Id { get; }
        string Address { get; }
        string ChainId { get; }
        string RecoveryMethod { get; }
        int HdWalletIndex { get; }
        /// <summary>
        /// Whether the user can unilaterally sign with this wallet.
        /// </summary>
        bool UserCanSign => true;
        IEmbeddedEthereumWalletProvider RpcProvider { get; }
    }

    internal static class EmbeddedEthereumWalletExtensions
    {
        internal static bool IsOnDevice(this IEmbeddedEthereumWallet wallet)
        {
            return wallet.Id == null || wallet.RecoveryMethod != "privy-v2";
        }
    }
}
