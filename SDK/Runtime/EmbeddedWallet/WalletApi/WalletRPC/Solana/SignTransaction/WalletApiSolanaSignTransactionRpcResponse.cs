using Newtonsoft.Json;

namespace Privy
{
    internal struct WalletApiSolanaSignTransactionRpcResponse
    {
        [JsonProperty("signed_transaction")]
        internal string SignedTransaction;

        [JsonProperty("encoding")]
        internal string Encoding;
    }
}
