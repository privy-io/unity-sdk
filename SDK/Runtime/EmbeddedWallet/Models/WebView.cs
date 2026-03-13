using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Privy.Wallets
{
    internal class IframeRequest<T>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    internal class ReadyRequestData
    {
    }

    internal class CreateEthereumWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [CanBeNull]
        [JsonProperty("solanaAddress")]
        public string SolanaAddress;
    }

    internal class CreateSolanaWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [CanBeNull]
        [JsonProperty("ethereumAddress")]
        public string EthereumAddress;
    }

    internal class CreateAdditionalWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;

        [JsonProperty("hdWalletIndex")]
        public int WalletIndex;
    }

    internal class ConnectWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;
    }

    internal class RecoverWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;
    }

    internal class RpcRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;

        [JsonProperty("hdWalletIndex")]
        public int WalletIndex;

        /// <summary>
        /// The details of the RPC request.
        /// </summary>
        /// <seealso cref="EthereumRpcRequestDetails"/>
        /// <seealso cref="SolanaRpcRequestDetails"/>
        [JsonProperty("request")]
        public IRpcRequestDetails Request;

        internal interface IRpcRequestDetails
        {
        }

        internal class EthereumRpcRequestDetails : IRpcRequestDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("params")]
            public string[] Params;
        }

        internal class SolanaRpcRequestDetails : IRpcRequestDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("params")]
            public ISolanaRpcRequestParams Params;
        }

        internal interface ISolanaRpcRequestParams { }

        internal class SolanaSignMessageRpcRequestParams : ISolanaRpcRequestParams
        {
            [JsonProperty("message")]
            public string Message;
        }

        internal class SolanaSignTransactionRpcRequestParams : ISolanaRpcRequestParams
        {
            [JsonProperty("transaction")]
            public string Transaction;
        }

        internal class SolanaSignAndSendTransactionRpcRequestParams : ISolanaRpcRequestParams
        {
            [JsonProperty("transaction")]
            public string Transaction;

            [JsonProperty("cluster")]
            public string Cluster;

            [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
            public SolanaSignAndSendOptionsParams Options;
        }

        internal class SolanaSignAndSendOptionsParams
        {
            [JsonProperty("skipPreflight", NullValueHandling = NullValueHandling.Ignore)]
            public bool? SkipPreflight;

            [JsonProperty("preflightCommitment", NullValueHandling = NullValueHandling.Ignore)]
            public string PreflightCommitment;

            [JsonProperty("maxRetries", NullValueHandling = NullValueHandling.Ignore)]
            public int? MaxRetries;

            [JsonProperty("minContextSlot", NullValueHandling = NullValueHandling.Ignore)]
            public int? MinContextSlot;
        }
    }

    internal class UserSignerSignRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        /// <summary>
        /// A base64 encoding of the bytes to sign over
        /// </summary>
        [JsonProperty("message")]
        public string Message;
    }

    //Responses

    //Base Class, used to parse event and id
    internal class IframeResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }
    }

    internal class IframeResponseSuccess<T> : IframeResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    internal class ErrorDetails
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }


    internal class IframeResponseError : IframeResponse
    {
        [JsonProperty("error")]
        public ErrorDetails Error { get; set; }
    }

    internal class ReadyResponseData
    {
        // Add specific properties for iframe ready data
    }


    internal class CreateEthereumWalletResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }

    internal class CreateSolanaWalletResponseData
    {
        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }
    }

    internal class CreateAdditionalWalletResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("hdWalletIndex")]
        public string WalletIndex { get; set; }
    }

    internal class ConnectWalletResponseData
    {
        [JsonProperty("entropyId")]
        public string EntropyId { get; set; }
    }

    internal class RecoverWalletResponseData
    {
        [JsonProperty("entropyId")]
        public string EntropyId { get; set; }
    }

    internal class RpcResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// The details of the RPC response.
        /// </summary>
        /// <seealso cref="EthereumRpcResponseDetails"/>
        /// <seealso cref="SolanaRpcResponseDetails"/>
        [JsonProperty("response")]
        [JsonConverter(typeof(RpcResponseDetailsConverter))]
        public IRpcResponseDetails Response;

        internal interface IRpcResponseDetails
        {
        }

        private class RpcResponseDetailsConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // Not necessary for a "response" type.
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                var jo = JObject.Load(reader);

                var method = jo.GetValue("method");

                // FIXME: Need a much better way to deal with polymorphic parsing here,
                // possibly higher up to take "chainType" into account.
                if (method != null && method.Type == JTokenType.String)
                {
                    var methodStr = method.Value<string>();
                    if (methodStr == "signMessage" ||
                        methodStr == "signTransaction" ||
                        methodStr == "signAndSendTransaction")
                    {
                        // Treat it as a Solana RPC response
                        return jo.ToObject<SolanaRpcResponseDetails>(serializer);
                    }
                }

                return jo.ToObject<EthereumRpcResponseDetails>(serializer);
            }

            public override bool CanConvert(Type objectType) => objectType == typeof(IRpcResponseDetails);
        }

        private class SolanaRpcResponseDetailsConverter : JsonConverter<SolanaRpcResponseDetails>
        {
            public override void WriteJson(JsonWriter writer, SolanaRpcResponseDetails value, JsonSerializer serializer)
            {
                // Not necessary for a "response" type.
                throw new NotImplementedException();
            }

            public override SolanaRpcResponseDetails ReadJson(JsonReader reader, Type objectType,
                SolanaRpcResponseDetails existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var jo = JObject.Load(reader);
                var method = jo.GetValue("method")?.Value<string>();

                ISolanaRpcResponseData data;
                switch (method)
                {
                    case "signMessage":
                        data = jo.GetValue("data")?.ToObject<SolanaSignMessageRpcResponseData>(serializer);
                        break;
                    case "signTransaction":
                        data = jo.GetValue("data")?.ToObject<SolanaSignTransactionRpcResponseData>(serializer);
                        break;
                    case "signAndSendTransaction":
                        data = jo.GetValue("data")?.ToObject<SolanaSignAndSendTransactionRpcResponseData>(serializer);
                        break;
                    default:
                        data = null;
                        break;
                }

                return new SolanaRpcResponseDetails { Method = method, Data = data };
            }
        }

        internal class EthereumRpcResponseDetails : IRpcResponseDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("data")]
            public string Data;
        }

        [JsonConverter(typeof(SolanaRpcResponseDetailsConverter))]
        internal class SolanaRpcResponseDetails : IRpcResponseDetails
        {
            [JsonProperty("method")]
            public string Method;

            /// <summary>
            /// Raw data object; cast to the appropriate type based on <see cref="Method"/>:
            /// <list type="bullet">
            /// <item><see cref="SolanaSignMessageRpcResponseData"/> when method is "signMessage"</item>
            /// <item><see cref="SolanaSignTransactionRpcResponseData"/> when method is "signTransaction"</item>
            /// <item><see cref="SolanaSignAndSendTransactionRpcResponseData"/> when method is "signAndSendTransaction"</item>
            /// </list>
            /// </summary>
            [JsonProperty("data")]
            public ISolanaRpcResponseData Data;
        }

        internal interface ISolanaRpcResponseData { }

        internal class SolanaSignMessageRpcResponseData : ISolanaRpcResponseData
        {
            [JsonProperty("signature")]
            public string Signature;
        }

        internal class SolanaSignTransactionRpcResponseData : ISolanaRpcResponseData
        {
            [JsonProperty("signedTransaction")]
            public string SignedTransaction;
        }

        internal class SolanaSignAndSendTransactionRpcResponseData : ISolanaRpcResponseData
        {
            [JsonProperty("hash")]
            public string Hash;
        }
    }

    internal class UserSignerSignResponseData
    {
        /// <summary>
        /// A base64 encoding of the resulting signature
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum ChainType
    {
        [EnumMember(Value = "ethereum")]
        Ethereum,

        [EnumMember(Value = "solana")]
        Solana
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum EntropyIdVerifierName
    {
        // In contrast with EntropyIdVerifier, this enum is public, for compatibility with the public classes here.
        [EnumMember(Value = "ethereum-address-verifier")]
        EthereumAddress,

        [EnumMember(Value = "solana-address-verifier")]
        SolanaAddress
    }

    internal static class EntropyIdVerifierNameExtensions
    {
        internal static EntropyIdVerifierName ToVerifierName(this EntropyIdVerifier verifier)
        {
            return verifier switch
            {
                EntropyIdVerifier.EthereumAddress => EntropyIdVerifierName.EthereumAddress,
                EntropyIdVerifier.SolanaAddress => EntropyIdVerifierName.SolanaAddress,
                _ => throw new ArgumentOutOfRangeException(nameof(verifier), verifier, "Could not verify the wallets entropy")
            };
        }
    }
}
