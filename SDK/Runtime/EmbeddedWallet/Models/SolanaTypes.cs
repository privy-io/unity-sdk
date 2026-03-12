namespace Privy.Wallets
{
    /// <summary>
    /// Represents a Solana cluster (network) with its CAIP-2 identifier and RPC URL.
    /// </summary>
    public class SolanaCluster
    {
        /// <summary>
        /// The CAIP-2 chain identifier (e.g. "solana:5eykt4UsFv8P8NJdTREpY1vzqKqZKvdp").
        /// </summary>
        public string Caip2 { get; }

        /// <summary>
        /// The RPC URL for this cluster.
        /// </summary>
        public string RpcUrl { get; }

        public SolanaCluster(string caip2, string rpcUrl)
        {
            Caip2 = caip2;
            RpcUrl = rpcUrl;
        }

        /// <summary>Solana Mainnet Beta.</summary>
        public static readonly SolanaCluster Mainnet = new SolanaCluster(
            "solana:5eykt4UsFv8P8NJdTREpY1vzqKqZKvdp",
            "https://api.mainnet-beta.solana.com"
        );

        /// <summary>Solana Devnet.</summary>
        public static readonly SolanaCluster Devnet = new SolanaCluster(
            "solana:EtWTRABZaYq6iMfeYKouRu166VU2xqa1",
            "https://api.devnet.solana.com"
        );

        /// <summary>Solana Testnet.</summary>
        public static readonly SolanaCluster Testnet = new SolanaCluster(
            "solana:4uhcVJyU9pJkvQyS88uRDiswHXSCkY3z",
            "https://api.testnet.solana.com"
        );

        /// <summary>
        /// Attempts to look up a <see cref="SolanaCluster"/> given its RPC URL.
        /// Returns <c>null</c> if the URL isn't recognised.
        /// </summary>
        public static SolanaCluster FromRpcUrl(string rpcUrl)
        {
            if (string.IsNullOrEmpty(rpcUrl)) return null;
            // compare ignoring trailing slashes
            string norm(string s) => s?.TrimEnd('/');
            rpcUrl = norm(rpcUrl);

            if (rpcUrl == norm(Mainnet.RpcUrl)) return Mainnet;
            if (rpcUrl == norm(Devnet.RpcUrl)) return Devnet;
            if (rpcUrl == norm(Testnet.RpcUrl)) return Testnet;
            return null;
        }
    }

    /// <summary>
    /// Options for sending a Solana transaction.
    /// </summary>
    public class SolanaSendOptions
    {
        /// <summary>
        /// If true, skip the preflight transaction checks.
        /// </summary>
        /// <remarks>
        /// These options are only meaningful when the SDK is executing in the
        /// on‑device (WebView) implementation. The TEE/wallet‑API path ignores
        /// them entirely.
        /// </remarks>
        public bool? SkipPreflight { get; set; }

        /// <summary>
        /// The commitment level to use for preflight checks (e.g. "confirmed", "finalized").
        /// </summary>
        public string PreflightCommitment { get; set; }

        /// <summary>
        /// Maximum number of retries to send the transaction.
        /// </summary>
        public int? MaxRetries { get; set; }

        /// <summary>
        /// The minimum slot at which to perform preflight.
        /// </summary>
        public int? MinContextSlot { get; set; }
    }
}
