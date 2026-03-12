using System;
using System.Threading.Tasks;
using Privy.Auth;
using Privy.Auth.Email;
using Privy.Auth.Sms;
using Privy.Auth.OAuth;
using Privy.Auth.Models;

namespace Privy.Core
{
    /// <summary>
    /// Interface representing the core functionality of the Privy SDK.
    /// Provides access to authentication, user data, and embedded wallet management.
    /// </summary>
    public interface IPrivy
    {
        /// <summary>
        /// Gets a value indicating whether the SDK is ready for use.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Email-based authentication methods.
        /// </summary>
        ILoginWithEmail Email { get; }

        /// <summary>
        /// OAuth-based authentication methods.
        /// </summary>
        ILoginWithOAuth OAuth { get; }

        /// <summary>
        /// SMS (phone number) authentication methods.
        /// </summary>
        ILoginWithSms Sms { get; }

        /// <summary>
        /// The currently authenticated user, if any.
        /// </summary>
        Task<IPrivyUser> GetUser();

        /// <summary>
        /// The current authentication state.  This call will block until SDK initialization completes.
        /// </summary>
        Task<AuthState> GetAuthState();

        /// <summary>
        /// Raised whenever the authentication state changes.
        /// </summary>
        event Action<AuthState> AuthStateChanged;

        /// <summary>
        /// Logs the user out of the application, clearing the session and authentication data.
        /// </summary>
        void Logout();
    }
}
