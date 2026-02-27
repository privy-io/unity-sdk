using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiSolanaSignAndSendTransactionRpcResponse
    {
        [JsonProperty("hash")]
        internal string Hash;
    }
}
