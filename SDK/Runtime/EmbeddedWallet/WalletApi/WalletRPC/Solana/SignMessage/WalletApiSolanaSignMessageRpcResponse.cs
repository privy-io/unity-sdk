using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiSolanaSignMessageRpcResponse
    {
        [JsonProperty("signature")]
        internal string Signature;
    }
}
