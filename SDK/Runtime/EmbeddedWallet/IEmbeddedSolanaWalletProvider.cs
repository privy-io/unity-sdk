using System.Threading.Tasks;

namespace Privy.Wallets
{
    /// <summary>
    /// Interface for acting on an embedded Solana wallet.
    /// </summary>
    public interface IEmbeddedSolanaWalletProvider
    {
        /// <summary>
        /// Request a signature on a Base64 encoded message or transaction.
        /// </summary>
        /// <param name="message">Base64-encoded message bytes.</param>
        /// <returns>Base64-encoded signature of the message.</returns>
        Task<string> SignMessage(string message);

        /// <summary>
        /// Signs a Solana transaction without broadcasting it.
        /// </summary>
        /// <param name="transaction">Base64-encoded serialized transaction bytes.</param>
        /// <returns>Base64-encoded signed transaction.</returns>
        Task<string> SignTransaction(string transaction);

        /// <summary>
        /// Signs a Solana transaction and broadcasts it to the network.
        /// </summary>
        /// <param name="transaction">Base64-encoded serialized transaction bytes.</param>
        /// <param name="cluster">The Solana cluster to broadcast to.</param>
        /// <param name="options">
        /// Optional send options. Pass <c>null</c> to use cluster defaults.
        /// <para>
        /// <strong>Note:</strong> these options are only honoured when the
        /// provider is running in the on-device/WebView architecture; they are
        /// ignored entirely by the TEE/wallet-API service.
        /// </para>
        /// </param>
        /// <returns>Transaction signature (hash) returned by the Solana network.</returns>
        Task<string> SignAndSendTransaction(string transaction, SolanaCluster cluster, SolanaSendOptions options = null);
    }
}
