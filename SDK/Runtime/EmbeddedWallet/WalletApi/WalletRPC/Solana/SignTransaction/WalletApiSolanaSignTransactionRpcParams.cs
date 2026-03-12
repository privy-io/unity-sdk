using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Privy.Wallets
{
    internal struct WalletApiSolanaSignTransactionRpcParams
    {
        [JsonProperty("transaction")]
        internal string Transaction;

        [JsonProperty("encoding")]
        internal TransactionEncoding Encoding;

        [JsonConverter(typeof(StringEnumConverter))]
        internal enum TransactionEncoding
        {
            [EnumMember(Value = "base64")]
            Base64
        }

        internal static WalletApiSolanaSignTransactionRpcParams FromString(string transaction) =>
            new()
            {
                Transaction = transaction,
                Encoding = TransactionEncoding.Base64
            };
    }
}
