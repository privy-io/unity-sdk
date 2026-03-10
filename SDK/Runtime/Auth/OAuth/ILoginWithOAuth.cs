using System.Threading.Tasks;
using Privy.Auth.Models;

namespace Privy.Auth.OAuth
{
    /// <summary>
    ///     Interface for oauth-based authentication methods.
    ///     Provides functionality to perform an OAuth 2.0 flow and to authenticate against Privy with it.
    /// </summary>
    public interface ILoginWithOAuth
    {
        /// <summary>
        ///     Authenticates a user using OAuth 2.0 against the given OAuth Provider.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task result is the authentication state (Authenticated,
        ///     Unauthenticated), indicating whether the login was successful or failed.
        /// </returns>
        /// <exception cref="PrivyAuthenticationException">Thrown if the authentication fails.</exception>
        Task<AuthState> LoginWithProvider(OAuthProvider provider, string redirectUri);
    }
}
