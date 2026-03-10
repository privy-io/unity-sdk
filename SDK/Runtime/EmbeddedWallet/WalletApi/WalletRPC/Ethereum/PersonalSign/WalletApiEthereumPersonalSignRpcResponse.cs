using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiEthereumPersonalSignRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}
