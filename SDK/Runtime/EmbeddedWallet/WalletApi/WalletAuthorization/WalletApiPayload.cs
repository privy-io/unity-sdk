using System.Collections.Generic;
using Newtonsoft.Json;
using Org.Webpki.JsonCanonicalizer;

namespace Privy.Wallets
{
    /// <summary>
    /// Represents an API request payload to be signed for authorization.
    /// The signature can be included as the <c>privy-authorization-signature</c> header.
    /// </summary>
    public struct WalletApiPayload
    {
        [JsonProperty("version")]
        public int Version;

        [JsonProperty("url")]
        public string Url;

        [JsonProperty("method")]
        public string Method;

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers;

        [JsonProperty("body")]
        public object Body;

        internal byte[] EncodePayload()
        {
            string json = JsonConvert.SerializeObject(this);
            var jsoncanicalizer = new JsonCanonicalizer(json);
            return jsoncanicalizer.GetEncodedUTF8();
        }
    }
}
