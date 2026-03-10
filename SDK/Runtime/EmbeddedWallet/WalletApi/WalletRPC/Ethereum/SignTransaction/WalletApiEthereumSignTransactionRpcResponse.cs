using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiEthereumSignTransactionRpcResponse
    {
        [JsonProperty("signed_transaction")]
        internal string SignedTransaction;
    }
}
