using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy
{
    internal struct AppConfig
    {
        [JsonProperty("embedded_wallet_config")]
        internal EmbeddedWalletConfig EmbeddedWalletConfig;
    }

    internal struct EmbeddedWalletConfig
    {
        [JsonProperty("mode")]
        internal EmbeddedWalletMode Mode;

        // server may set this to true to force the SDK to avoid using an iframe/webview
        [JsonProperty("force_server_wallets")]
        internal bool ForceServerWallets;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum EmbeddedWalletMode
    {
        [EnumMember(Value = "legacy-embedded-wallets-only")]
        LegacyEmbeddedWalletsOnly,

        [EnumMember(Value = "user-controlled-server-wallets-only")]
        UserControlledServerWalletsOnly
    }
}
