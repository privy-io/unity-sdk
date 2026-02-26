namespace Privy
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
    }

    /// <summary>
    /// Options for sending a Solana transaction.
    /// </summary>
    public class SolanaSendOptions
    {
        /// <summary>
        /// If true, skip the preflight transaction checks.
        /// </summary>
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
