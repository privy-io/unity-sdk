using System.Collections.Generic;

namespace Privy.Auth.Models
{
    internal class InternalPrivyUser
    {
        public string Id { get; set; }
        public PrivyLinkedAccount[] LinkedAccounts { get; set; }
        public Dictionary<string, string> CustomMetadata { get; set; }
    }

    internal class InternalAuthSession
    {
        public InternalPrivyUser User { get; set; }
        public string AccessToken { get; set; }
        public string IdentityToken { get; set; }
        public string RefreshToken { get; set; }
        public SessionUpdateAction SessionUpdateAction { get; set; }
    }
}
