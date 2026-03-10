using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiCreateRequest
    {
        [JsonProperty("chain_type")]
        internal ChainType chainType;
    }
}
