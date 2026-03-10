using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiEthereumSendTransactionRpcResponse
    {
        [JsonProperty("transaction_id")]
        internal string TransactionId;

        [JsonProperty("hash")]
        internal string Hash;

        [JsonProperty("caip2")]
        internal string CAIP2;
    }
}
