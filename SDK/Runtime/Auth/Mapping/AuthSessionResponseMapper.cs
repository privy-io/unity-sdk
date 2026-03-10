using System.Linq;
using Privy.Auth.Models;
using Privy.Utils;

namespace Privy.Auth.Mapping
{
    internal static class AuthSessionResponseMapper
    {
        public static InternalAuthSession MapToInternalSession(ValidSessionResponse authResponse)
        {
            return new InternalAuthSession
            {
                User = MapToInternalUser(authResponse.User),
                AccessToken = authResponse.Token,
                IdentityToken = authResponse.IdentityToken,
                RefreshToken = authResponse.RefreshToken,
                SessionUpdateAction = authResponse.SessionUpdateAction
            };
        }

        public static InternalPrivyUser MapToInternalUser(UserResponse userResponse)
        {
            return new InternalPrivyUser
            {
                Id = userResponse.Id,
                LinkedAccounts = userResponse.LinkedAccounts
                    .Select(account =>
                    {
                        var mappedAccount = LinkedAccountResponseMapper.MapToPublic(account);
                        PrivyLogger.Debug($"Mapped account type: {mappedAccount?.GetType().Name ?? "null"}");
                        return mappedAccount;
                    })
                    .Where(mappedAccount => mappedAccount != null)
                    .ToArray(),
                CustomMetadata = userResponse.CustomMetadata
            };
        }
    }
}
