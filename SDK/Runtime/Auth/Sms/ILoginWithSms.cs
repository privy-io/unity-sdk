using System.Threading.Tasks;

namespace Privy
{
    /// <summary>
    /// Interface for SMS-based (phone number) authentication methods.
    /// Provides functionality to send a one-time password (OTP) code via SMS
    /// and to authenticate, link, or unlink using the sent OTP code.
    /// </summary>
    public interface ILoginWithSms
    {
        /// <summary>
        /// Sends a one-time password (OTP) code to the specified phone number via SMS.
        /// </summary>
        /// <param name="phoneNumber">The phone number to which the OTP code will be sent. Must be in E.164 format (e.g. "+15551234567").</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the code was successfully sent.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if there is an error while sending the OTP code.</exception>
        Task<bool> SendCode(string phoneNumber);

        /// <summary>
        /// Authenticates a user using the provided phone number and OTP code.
        /// </summary>
        /// <param name="phoneNumber">The phone number associated with the OTP code. Must be the same phone number used to request the code.</param>
        /// <param name="code">The OTP code received via SMS. Must be a valid, non-empty code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the authentication state indicating whether the login was successful.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if the authentication fails due to an invalid OTP code.</exception>
        Task<AuthState> LoginWithCode(string phoneNumber, string code);

        /// <summary>
        /// Links a phone number to the currently authenticated user.
        /// The user must already be logged in before calling this method.
        /// After completion, the updated linked accounts are reflected in <c>privy.User.LinkedAccounts</c>.
        /// </summary>
        /// <param name="phoneNumber">The phone number to link. Must be in E.164 format.</param>
        /// <param name="code">The OTP code received via SMS after calling <see cref="SendCode"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if the user is not authenticated or if the link operation fails.</exception>
        Task Link(string phoneNumber, string code);

        /// <summary>
        /// Unlinks a phone number from the currently authenticated user.
        /// The user must already be logged in before calling this method.
        /// After completion, the updated linked accounts are reflected in <c>privy.User.LinkedAccounts</c>.
        /// </summary>
        /// <param name="phoneNumber">The phone number to unlink. Must be in E.164 format.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="PrivyException.AuthenticationException">Thrown if the user is not authenticated or if the unlink operation fails.</exception>
        Task Unlink(string phoneNumber);
    }
}
