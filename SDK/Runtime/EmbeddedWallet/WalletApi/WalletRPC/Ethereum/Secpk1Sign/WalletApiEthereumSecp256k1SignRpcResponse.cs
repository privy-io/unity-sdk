using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiEthereumSecp256k1SignRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}
