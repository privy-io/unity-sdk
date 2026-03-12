using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace Privy.Auth.Models
{
    //Request Models

    //ideally would use record structs here but c# version limitation, still potentially room for improvement
    //immutable req/resp body data types, ex usage: new SendCodeRequestData('moiz@privy.io')
    internal class SendCodeRequestData
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }

    internal class LogInRequestData
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    internal class SendSmsCodeRequestData
    {
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    internal class SmsLoginRequestData
    {
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    internal class SmsLinkRequestData
    {
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    internal class SmsUnlinkRequestData
    {
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    internal class SmsUpdateRequestData
    {
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    internal class SendRefreshRequestData
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    internal class InitiateOAuthFlowRequestData
    {
        [JsonProperty("provider")]
        public OAuthProvider ProviderName { get; set; }

        [JsonProperty("redirect_to")]
        public string RedirectUri { get; set; }

        [JsonProperty("code_challenge")]
        public string CodeChallenge { get; set; }

        [JsonProperty("state_code")]
        public string StateCode { get; set; }
    }

    internal class AuthenticateOAuthFlowRequestData
    {
        [JsonProperty("authorization_code")]
        public string AuthorizationCode { get; set; }

        [JsonProperty("code_verifier")]
        public string CodeVerifier { get; set; }

        [JsonProperty("state_code")]
        public string StateCode { get; set; }

        [JsonProperty("code_type", NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticateOAuthFlowCodeType? CodeType;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum AuthenticateOAuthFlowCodeType
    {
        [EnumMember(Value = "raw")]
        Raw
    }

    //Response Models
    internal class SendCodeResponseData
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    internal class UserResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        // TODO: convert these to dates
        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }

        [JsonProperty("has_accepted_terms")]
        public bool HasAcceptedTerms { get; set; }

        [JsonProperty("linked_accounts")]
        public LinkedAccountResponse[] LinkedAccounts;

        [JsonProperty("custom_metadata")]
        public Dictionary<string, string> CustomMetadata = new Dictionary<string, string>();

        // TODO: implement mfa_methods
    }

    /// <summary>
    /// Instructs the SDK how to process the returned session.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum SessionUpdateAction
    {
        [EnumMember(Value = "set")]
        Set,
        [EnumMember(Value = "clear")]
        Clear,
        [EnumMember(Value = "ignore")]
        Ignore
    }

    internal class ValidSessionResponse
    {
        [JsonProperty("user")]
        public UserResponse User { get; set; }

        [JsonProperty("is_new_user")]
        public bool IsNewUser { get; set; }

        public string Token { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("identity_token")]
        public string IdentityToken { get; set; }

        [JsonProperty("session_update_action")]
        public SessionUpdateAction SessionUpdateAction { get; set; }
    }

    internal class InitiateOAuthFlowResponse
    {
        [JsonProperty("url")]
        public string OAuthUrl { get; set; }
    }
}
