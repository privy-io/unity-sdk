using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiEthereumSignTypedDataV4RpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}
