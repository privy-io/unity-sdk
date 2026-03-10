using Privy.Auth;
using Privy.Auth.Models;
using Privy.Wallets.WalletApi;

namespace Privy.Wallets
{
    internal class EmbeddedWallet : IEmbeddedEthereumWallet
    {
        public string Id { get; }
        public string Address { get; }
        public string ChainId { get; }
        public string RecoveryMethod { get; }
        public int HdWalletIndex { get; }

        public IEmbeddedEthereumWalletProvider RpcProvider { get; } // Use IEmbeddedEthereumWalletProvider as the type

        public EmbeddedWallet(PrivyEmbeddedWalletAccount account, IEmbeddedEthereumWalletProvider rpcProvider)
        {
            Id = account.Id;
            Address = account.Address;
            ChainId = account.ChainId;
            RecoveryMethod = account.RecoveryMethod;
            HdWalletIndex = account.WalletIndex;
            RpcProvider = rpcProvider;
        }

        internal static EmbeddedWallet Create(PrivyEmbeddedWalletAccount account, WalletEntropy walletEntropy,
            EmbeddedWalletManager embeddedWalletManager, WalletApiRepository walletApiRepository,
            AuthDelegator authDelegator)
        {
            IRpcExecutor rpcExecutor = account.IsOnDevice
                ? new WebViewRPCExecutor(walletEntropy, account.WalletIndex, embeddedWalletManager)
                : new WalletApiRPCExecutor(walletApiRepository, authDelegator, account.Id);
            var rpcProvider = new EmbeddedEthereumWalletProvider(rpcExecutor);

            return new EmbeddedWallet(account, rpcProvider);
        }
    }
}
