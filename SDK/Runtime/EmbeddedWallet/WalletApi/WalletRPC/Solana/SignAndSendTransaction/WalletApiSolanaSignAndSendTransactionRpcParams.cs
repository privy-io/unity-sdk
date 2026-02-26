using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy
{
    internal struct WalletApiSolanaSignAndSendTransactionRpcParams
    {
        [JsonProperty("transaction")]
        internal string Transaction;

        [JsonProperty("encoding")]
        internal TransactionEncoding Encoding;

        [JsonProperty("skipPreflight", NullValueHandling = NullValueHandling.Ignore)]
        internal bool? SkipPreflight;

        [JsonProperty("preflightCommitment", NullValueHandling = NullValueHandling.Ignore)]
        internal string PreflightCommitment;

        [JsonProperty("maxRetries", NullValueHandling = NullValueHandling.Ignore)]
        internal int? MaxRetries;

        [JsonProperty("minContextSlot", NullValueHandling = NullValueHandling.Ignore)]
        internal int? MinContextSlot;

        [JsonConverter(typeof(StringEnumConverter))]
        internal enum TransactionEncoding
        {
            [EnumMember(Value = "base64")]
            Base64
        }

        internal static WalletApiSolanaSignAndSendTransactionRpcParams FromString(
            string transaction,
            SolanaSendOptions options = null) =>
            new()
            {
                Transaction = transaction,
                Encoding = TransactionEncoding.Base64,
                SkipPreflight = options?.SkipPreflight,
                PreflightCommitment = options?.PreflightCommitment,
                MaxRetries = options?.MaxRetries,
                MinContextSlot = options?.MinContextSlot
            };
    }
}
