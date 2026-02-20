using Newtonsoft.Json;
using System.Collections.Generic;

//Models for Sign Typed Data
internal class Param
{
    [JsonProperty("types")]
    public ParamTypes Types { get; set; }

    [JsonProperty("primaryType")]
    public string PrimaryType { get; set; }

    [JsonProperty("domain")]
    public Domain Domain { get; set; }

    [JsonProperty("message")]
    public Message Message { get; set; }
}

internal class ParamTypes
{
    [JsonProperty("EIP712Domain")]
    public List<DomainType> EIP712Domain { get; set; }

    [JsonProperty("Person")]
    public List<DomainType> Person { get; set; }

    [JsonProperty("Mail")]
    public List<DomainType> Mail { get; set; }
}

internal class DomainType
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

internal class Domain
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("chainId")]
    public int ChainId { get; set; }

    [JsonProperty("verifyingContract")]
    public string VerifyingContract { get; set; }
}

internal class Message
{
    [JsonProperty("from")]
    public W From { get; set; }

    [JsonProperty("to")]
    public W To { get; set; }

    [JsonProperty("contents")]
    public string Contents { get; set; }

    internal class W
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("wallet")]
        public string Wallet { get; set; }
    }
}