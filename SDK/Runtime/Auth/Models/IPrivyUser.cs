using System.Threading.Tasks;
using System.Collections.Generic;
using Privy.Wallets;

namespace Privy.Auth.Models
{
    /// <summary>
    /// Represents a Privy user with properties and methods for managing the user's identity and embedded wallets.
    /// </summary>
    public interface IPrivyUser
    {
        /// <summary>
        /// Gets the user's unique identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the list of the user's linked accounts.
        /// </summary>
        PrivyLinkedAccount[] LinkedAccounts { get; }

        /// <summary>
        /// Gets the list of the user's embedded Ethereum wallets.
        /// </summary>
        IEmbeddedEthereumWallet[] EmbeddedEthereumWallets { get; }

        /// <summary>
        /// **DEPRECATED**: use <see cref="EmbeddedEthereumWallets"/> instead.
        /// The old name is retained for compatibility and simply forwards to the new property.
        /// </summary>
        [System.Obsolete("Use EmbeddedEthereumWallets instead.")]
        IEmbeddedEthereumWallet[] EmbeddedWallets { get; }

        /// <summary>
        /// Gets the list of the user's embedded Solana wallets.
        /// </summary>
        IEmbeddedSolanaWallet[] EmbeddedSolanaWallets { get; }

        /// <summary>
        /// Gets the user's custom metadata key-value mapping.
        /// </summary>
        Dictionary<string, string> CustomMetadata { get; }

        /// <summary>
        /// Gets the user's access token, refreshing the session if necessary.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's access token.</returns>
        Task<string> GetAccessToken();

        /// <summary>
        /// Gets the user's identity token, refreshing the session if necessary.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's identity token.</returns>
        Task<string> GetIdentityToken();

        /// <summary>
        /// Creates a new embedded Ethereum wallet for the user.
        /// </summary>
        /// <param name="allowAdditional">Whether to allow the creation of additional wallets derived from the primary HD wallet</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created embedded wallet.</returns>
        /// <exception cref="PrivyAuthenticationException">
        /// Thrown if there is an issue with authentication, such as a failure to refresh the access token.
        /// </exception>
        /// <exception cref="PrivyWalletException">
        /// Thrown if the wallet creation fails or the wallet cannot be added to the user's account.
        /// </exception>
        Task<IEmbeddedEthereumWallet> CreateEthereumWallet(bool allowAdditional = false);


        /// <summary>
        /// Creates an Ethereum wallet at the specified HD index, or returns the existing wallet if one already exists at that index.
        /// A wallet with HD index 0 must be created before creating a wallet at greater HD indices.
        /// This method is idempotent. Calling it multiple times with the same HD index will have the same effect as calling it once.
        /// 
        /// </summary>
        /// <param name="hdWalletIndex">The specified HD wallet index of the wallet.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the newly created embedded wallet.</returns>
        /// <exception cref="PrivyAuthenticationException">
        /// Thrown if there is an issue with authentication, such as a failure to refresh the access token.
        /// </exception>
        /// <exception cref="PrivyWalletException">
        /// Thrown if the wallet creation fails or the wallet cannot be added to the user's account.
        /// Can also be thrown if an invalid HD wallet index is supplied, i.e. hdWalletIndex is less than 0,
        /// or if HD wallet index is greater than 0 while user has no wallet with HD index 0.
        /// </exception>
        Task<IEmbeddedEthereumWallet> CreateEthereumWalletAtHdIndex(int hdWalletIndex);

        /// <summary>
        /// Creates a new Solana embedded wallet for the user.
        /// </summary>
        Task<IEmbeddedSolanaWallet> CreateSolanaWallet(bool allowAdditional = false);

        /// <summary>
        /// Generates a cryptographic signature for an API request payload using the
        /// authenticated user's signing key.
        ///
        /// This method canonicalizes the payload to JSON (RFC 8785) and signs the
        /// canonical bytes. The returned base64-encoded signature can be included as
        /// the <c>privy-authorization-signature</c> header to authorize Privy API requests.
        /// </summary>
        /// <param name="payload">The <see cref="WalletApiPayload"/> containing the API request details to be signed.</param>
        /// <returns>A task whose result is the base64-encoded signature string.</returns>
        Task<string> GenerateAuthorizationSignature(WalletApiPayload payload);

        /// <summary>
        /// Generates a cryptographic signature for a pre-serialized binary payload using the
        /// authenticated user's signing key.
        ///
        /// Unlike the typed payload variant, this method skips JSON canonicalization and signs the
        /// raw bytes directly. Use this when you have already serialized and canonicalized your
        /// payload outside the SDK.
        /// </summary>
        /// <param name="payload">The pre-serialized payload bytes to sign.</param>
        /// <returns>A task whose result is the base64-encoded signature string.</returns>
        Task<string> GenerateAuthorizationSignature(byte[] payload);
    }
}
