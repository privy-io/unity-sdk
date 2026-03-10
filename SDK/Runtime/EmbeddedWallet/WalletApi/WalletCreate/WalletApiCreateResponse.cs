using Newtonsoft.Json;

namespace Privy.Wallets
{
    internal struct WalletApiCreateResponse
    {
        [JsonProperty("id")]
        internal string Id;

        [JsonProperty("address")]
        internal string Address;

        [JsonProperty("chain_type")]
        internal ChainType ChainType;
    }
}
