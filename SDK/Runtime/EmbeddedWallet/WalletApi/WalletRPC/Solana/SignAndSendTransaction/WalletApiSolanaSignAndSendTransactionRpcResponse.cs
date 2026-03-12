using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiSolanaSignAndSendTransactionRpcResponse
    {
        [JsonProperty("hash")]
        internal string Hash;
    }
}
