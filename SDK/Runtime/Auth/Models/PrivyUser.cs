using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Privy.Auth;
using Privy.Config;
using Privy.Utils;
using Privy.Wallets;

namespace Privy.Auth.Models
{
    internal class PrivyUser : IPrivyUser
    {
        public string Id
        {
            get
            {
                if (_authDelegator.CurrentAuthState != AuthState.Authenticated)
                {
                    return "";
                }
                else
                {
                    return _authDelegator.GetAuthSession().User.Id;
                }
            }
        }


        public PrivyLinkedAccount[] LinkedAccounts
        {
            get
            {
                if (_authDelegator.CurrentAuthState != AuthState.Authenticated)
                {
                    return Array.Empty<PrivyLinkedAccount>();
                }
                else
                {
                    return _authDelegator.GetAuthSession().User.LinkedAccounts;
                }
            }
        }


        public IEmbeddedEthereumWallet[] EmbeddedEthereumWallets
        {
            get
            {
                var linkedAccounts = LinkedAccounts;
                var onDeviceWalletEntropy = linkedAccounts.WalletEntropyOrNull();

                // On-device wallets need the user's HD entropy. TEE wallets are addressed by id, so they are
                // listed whatever their HD index, using their own address as the entropy id.
                return linkedAccounts.EmbeddedWalletAccounts()
                    .Select(account => new
                    {
                        Account = account,
                        WalletEntropy = account.IsOnDevice
                            ? onDeviceWalletEntropy
                            : new WalletEntropy(account.Address, EntropyIdVerifier.EthereumAddress)
                    })
                    .Where(item => item.WalletEntropy != null)
                    .Select(item =>
                        EmbeddedEthereumWallet.Create(item.Account, item.WalletEntropy.Value, _embeddedWalletManager,
                            _walletApiRepository, _authDelegator))
                    .ToArray<IEmbeddedEthereumWallet>();
            }
        }

        // deprecated alias to help existing consumers transition
        [Obsolete("Use EmbeddedEthereumWallets instead.")]
        public IEmbeddedEthereumWallet[] EmbeddedWallets => EmbeddedEthereumWallets;

        public IEmbeddedSolanaWallet[] EmbeddedSolanaWallets
        {
            get
            {
                var linkedAccounts = LinkedAccounts;
                var onDeviceWalletEntropy = linkedAccounts.WalletEntropyOrNull();

                // On-device wallets need the user's HD entropy. TEE wallets are addressed by id, so they are
                // listed whatever their HD index, using their own address as the entropy id.
                return linkedAccounts.EmbeddedSolanaWalletAccounts()
                    .Select(account => new
                    {
                        Account = account,
                        WalletEntropy = account.IsOnDevice
                            ? onDeviceWalletEntropy
                            : new WalletEntropy(account.Address, EntropyIdVerifier.SolanaAddress)
                    })
                    .Where(item => item.WalletEntropy != null)
                    .Select(item =>
                        EmbeddedSolanaWallet.Create(item.Account, item.WalletEntropy.Value, _embeddedWalletManager,
                            _walletApiRepository, _authDelegator))
                    .ToArray<IEmbeddedSolanaWallet>();
            }
        }

        public Dictionary<string, string> CustomMetadata
        {
            get
            {
                if (_authDelegator.CurrentAuthState != AuthState.Authenticated)
                {
                    return new Dictionary<string, string>();
                }
                else
                {
                    return _authDelegator.GetAuthSession().User.CustomMetadata;
                }
            }
        }

        private AuthDelegator _authDelegator;
        private AppConfigRepository _appConfigRepository;
        private WalletApiWalletCreator _walletApiWalletCreator;
        private WalletApiRepository _walletApiRepository;
        private IAuthorizationKey _authorizationKey;

        private EmbeddedWalletManager _embeddedWalletManager;

        //Constructor is internal, but the object is public facing
        //TODO: Create Interface class for this
        internal PrivyUser(AuthDelegator authDelegator, EmbeddedWalletManager embeddedWalletManager,
            AppConfigRepository appConfigRepository, WalletApiWalletCreator walletApiWalletCreator,
            WalletApiRepository walletApiRepository, IAuthorizationKey authorizationKey)
        {
            _authDelegator = authDelegator;
            _embeddedWalletManager = embeddedWalletManager;
            _appConfigRepository = appConfigRepository;
            _walletApiWalletCreator = walletApiWalletCreator;
            _walletApiRepository = walletApiRepository;
            _authorizationKey = authorizationKey;
        }

        public async Task<string> GetAccessToken()
        {
            return await _authDelegator.GetAccessToken();
        }

        public async Task<string> GetIdentityToken()
        {
            return await _authDelegator.GetIdentityToken();
        }

        public async Task<string> GenerateAuthorizationSignature(WalletApiPayload payload)
        {
            byte[] encodedPayload = payload.EncodePayload();
            byte[] signature = await _authorizationKey.Signature(encodedPayload);
            return Convert.ToBase64String(signature);
        }

        public async Task<string> GenerateAuthorizationSignature(byte[] payload)
        {
            byte[] signature = await _authorizationKey.Signature(payload);
            return Convert.ToBase64String(signature);
        }

        // Public Methods
        public async Task<IEmbeddedEthereumWallet> CreateEthereumWallet(bool allowAdditional = false)
        {
            var appConfig = await _appConfigRepository.LoadAppConfig();
            if (appConfig.EmbeddedWalletConfig.Mode == EmbeddedWalletMode.UserControlledServerWalletsOnly)
            {
                var result = await _walletApiWalletCreator.CreateWallet(ChainType.Ethereum, allowAdditional);
                return await PrepareAndConnectWallet(result.Address, walletId: result.Id);
            }

            if (!HasPrimaryWallet())
            {
                return await CreatePrimaryEthereumWallet(throwErrorIfPrimaryWalletExists: true);
            }
            else if (!allowAdditional)
            {
                // User has primary wallet, but didn't explicitly set allowAdditional to true, so throw error
                throw new PrivyWalletException(
                    "Wallet Create Failed: Primary wallet already exists. To create an additional wallet, set allowAdditional to true.",
                    EmbeddedWalletError.CreateFailed);
            }
            else
            {
                PrivyEmbeddedWalletAccount walletWithHighestIndex =
                    LinkedAccounts.EmbeddedWalletWithGreatestWalletIndexOrNull();

                if (walletWithHighestIndex == null)
                {
                    // This should never happen due to the precondition checks above, but adding a check to just in case
                    throw new PrivyWalletException(
                        "No existing wallet found, so can't determine new wallet's HD index.",
                        EmbeddedWalletError.CreateFailed);
                }

                // Next hd wallet index should be the highest current index + 1
                int nextIndex = walletWithHighestIndex.WalletIndex + 1;

                return await CreateHdWallet(hdWalletIndex: nextIndex);
            }
        }

        public async Task<IEmbeddedSolanaWallet> CreateSolanaWallet(bool allowAdditional = false)
        {
            var appConfig = await _appConfigRepository.LoadAppConfig();
            if (appConfig.EmbeddedWalletConfig.Mode == EmbeddedWalletMode.UserControlledServerWalletsOnly)
            {
                var result = await _walletApiWalletCreator.CreateWallet(ChainType.Solana, allowAdditional);
                return await PrepareAndConnectSolanaWallet(result.Address, walletId: result.Id);
            }

            var solanaWallets = LinkedAccounts.EmbeddedSolanaWalletAccounts();

            if (solanaWallets.Length == 0)
            {
                var primaryEthAccount = LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull();
                var accessToken = await _authDelegator.GetAccessToken();
                var createdWalletAddress =
                    await _embeddedWalletManager.CreateSolanaWallet(accessToken,
                        ethereumAddress: primaryEthAccount?.Address);
                return await PrepareAndConnectSolanaWallet(createdWalletAddress);
            }

            if (allowAdditional)
            {
                var highestIndex = solanaWallets.Max(wallet => wallet.WalletIndex);
                return await CreateHdSolanaWallet(hdWalletIndex: highestIndex + 1);
            }

            // User has primary wallet, but didn't explicitly set allowAdditional to true, so throw error
            throw new PrivyWalletException(
                "Wallet Create Failed: Primary wallet already exists. To create an additional wallet, set allowAdditional to true.",
                EmbeddedWalletError.CreateFailed);
        }

        // Creates an Ethereum wallet at the specified HD index, or returns the existing wallet if one already exists at that index.
        // This method is idempotent. Calling it multiple times with the same HD index will have the same effect as calling it once.
        public async Task<IEmbeddedEthereumWallet> CreateEthereumWalletAtHdIndex(int hdWalletIndex)
        {
            if (hdWalletIndex < 0)
            {
                // Negative HD index is invalid
                throw new PrivyWalletException(
                    "A negative HD wallet index is invalid.",
                    EmbeddedWalletError.CreateAdditionalFailed
                );
            }

            var appConfig = await _appConfigRepository.LoadAppConfig();
            if (appConfig.EmbeddedWalletConfig.Mode == EmbeddedWalletMode.UserControlledServerWalletsOnly)
            {
                // Keep index 0 idempotent: return the first existing index-0 wallet, as on-device does.
                var existingIndexZeroWallet = hdWalletIndex == 0
                    ? EmbeddedEthereumWallets.FirstOrDefault(wallet => wallet.HdWalletIndex == 0)
                    : null;
                if (existingIndexZeroWallet != null)
                {
                    return existingIndexZeroWallet;
                }

                // TEE wallets get their HD index from the server, so a caller can't choose one.
                throw new PrivyWalletException(
                    "Creating a wallet at a specific HD index is not supported for user-controlled server wallets. Use CreateEthereumWallet instead.",
                    EmbeddedWalletError.CreateFailed
                );
            }

            if (hdWalletIndex == 0)
            {
                // Dev wants to create primary wallet
                // CreatePrimaryEthereumWallet should not throw if a primary wallet already exists, because CreateEthereumWalletAtHdIndex
                // is idempotent and either creates a wallet or returns an existing one.
                return await CreatePrimaryEthereumWallet(throwErrorIfPrimaryWalletExists: false);
            }
            else
            {
                // HD index > 0, ensure there is a primary wallet before creating HD wallet
                // This could throw an error if the access token needs a refresh, and the refresh fails
                return await CreateHdWallet(hdWalletIndex: hdWalletIndex);
            }
        }

        // Private Methods
        private async Task<IEmbeddedEthereumWallet> PrepareAndConnectWallet(string walletAddress,
            string walletId = null)
        {
            await _authDelegator.RefreshSession(true); //Forces refresh even if token is valid

            //Get the first wallet with matching id or address, else null
            var embeddedWallet =
                EmbeddedEthereumWallets.FirstOrDefault(wallet =>
                    (walletId != null && wallet.Id == walletId) || wallet.Address == walletAddress);

            if (embeddedWallet == null)
            {
                throw new PrivyWalletException(
                    "Wallet Create Failed: Wallet was not added to account.", EmbeddedWalletError.CreateFailed);
            }

            // Fire-and-forget the wallet connection; log any exception via helper.
            _embeddedWalletManager
                .AttemptConnectingToWallet(_authDelegator.GetAuthSession())
                .SafeFireAndForget(ex => PrivyLogger.Error("Could not connect wallet", ex));

            return embeddedWallet;
        }

        private async Task<IEmbeddedSolanaWallet> PrepareAndConnectSolanaWallet(string walletAddress,
            string walletId = null)
        {
            await _authDelegator.RefreshSession(forceRefresh: true);

            var embeddedWallet =
                EmbeddedSolanaWallets.FirstOrDefault(wallet =>
                    (walletId != null && wallet.Id == walletId) || wallet.Address == walletAddress);

            if (embeddedWallet == null)
                throw new PrivyWalletException(
                    "Wallet Create Failed: Wallet was not added to account.", EmbeddedWalletError.CreateFailed);

            // Fire-and-forget the connection; log any exceptions
            _embeddedWalletManager
                .AttemptConnectingToWallet(_authDelegator.GetAuthSession())
                .SafeFireAndForget(ex => PrivyLogger.Error("Could not connect wallet", ex));

            return embeddedWallet;
        }

        private bool HasPrimaryWallet()
        {
            return LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull() != null;
        }

        /**
         * Attempts to create a primary ETH wallet (hdWalletIndex == 0) for the user.
         *
         * Params:
         * throwErrorIfPrimaryWalletExists: Set to true if this method should throw an error if a
         *      primary wallet already exists. Set to false if this method should return the existing primary wallet.
         *
         * Returns: Newly created primary wallet, or the user's existing primary wallet (if throwErrorIfPrimaryWalletExists == false)
         * Throws: An error if user already has a primary wallet (if throwErrorIfPrimaryWalletExists == true) or
         * if there is any issue creating the wallet
         */
        private async Task<IEmbeddedEthereumWallet> CreatePrimaryEthereumWallet(bool throwErrorIfPrimaryWalletExists)
        {
            // Some callers might have checked this already, but safer to double-check
            PrivyEmbeddedWalletAccount existingPrimaryWallet = LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull();

            if (existingPrimaryWallet != null)
            {
                if (throwErrorIfPrimaryWalletExists)
                {
                    throw new PrivyWalletException(
                        "Wallet Create Failed: Primary wallet already exists.",
                        EmbeddedWalletError.CreateFailed);
                }
                else
                {
                    PrivyLogger.Debug($"Wallet with HD index 0 already exists.");
                    return await PrepareAndConnectWallet(existingPrimaryWallet.Address);
                }
            }

            // This could throw an error if the access token needs a refresh, and the refresh fails
            string accessToken = await _authDelegator.GetAccessToken();

            var existingSolanaAccount = LinkedAccounts.EmbeddedSolanaWalletAccounts()
                .FirstOrDefault(account => account.WalletIndex == 0);
            string primaryWalletAddress =
                await _embeddedWalletManager.CreateEthereumWallet(accessToken,
                    solanaAddress: existingSolanaAccount?.Address);

            return await PrepareAndConnectWallet(primaryWalletAddress);
        }

        /**
         * Creates an Ethereum wallet at the specified HD index, or returns the existing wallet if one already exists at that index.
         *
         * Returns: Newly created HD wallet with specified index, or existing wallet if it already exists
         * Throws: An error if there is any issue creating the wallet, or a precondition check fails.
         */
        private async Task<IEmbeddedEthereumWallet> CreateHdWallet(int hdWalletIndex)
        {
            // Ensure user has a primary wallet
            if (LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull() is null)
                throw new PrivyWalletException(
                    "A user must have a wallet at HD index 0 before creating a wallet at greater HD indices.",
                    EmbeddedWalletError.CreateFailed
                );

            // Ensure user has wallet entropy
            var walletEntropy = LinkedAccounts.WalletEntropyOrNull() ??
                                throw new PrivyWalletException(
                                    "A user must have a primary wallet before creating a wallet at HD indices >= 1.",
                                    EmbeddedWalletError.CreateFailed
                                );

            // If there is already an existing wallet with the specified HD index, no need to create it
            PrivyEmbeddedWalletAccount existingWalletWithHdIndex =
                LinkedAccounts.EmbeddedWalletByIndexOrNull(hdWalletIndex);
            if (existingWalletWithHdIndex != null)
            {
                PrivyLogger.Debug($"Wallet with specified HD index ({hdWalletIndex}) already exists.");
                return await PrepareAndConnectWallet(existingWalletWithHdIndex.Address);
            }

            // This could throw an error if the access token needs a refresh, and the refresh fails
            string accessToken = await _authDelegator.GetAccessToken();

            // Create HD wallet at specified index
            string hdWalletAddress = await _embeddedWalletManager.CreateAdditionalWallet(
                accessToken: accessToken,
                walletEntropy: walletEntropy, chainType: ChainType.Ethereum, hdWalletIndex: hdWalletIndex);

            return await PrepareAndConnectWallet(hdWalletAddress);
        }

        /**
         * Creates a Solana wallet at the specified HD index, or returns the existing wallet if one already exists at that index.
         *
         * Returns: Newly created HD Solana wallet with specified index, or existing wallet if it already exists
         * Throws: An error if there is any issue creating the wallet, or a precondition check fails.
         */
        private async Task<IEmbeddedSolanaWallet> CreateHdSolanaWallet(int hdWalletIndex)
        {
            var solWalletAccounts = LinkedAccounts.EmbeddedSolanaWalletAccounts();

            // Ensure user has a primary SOL wallet
            if (solWalletAccounts.All(account => account.WalletIndex != 0))
                throw new PrivyWalletException(
                    "A user must have a wallet at HD index 0 before creating a wallet at greater HD indices.",
                    EmbeddedWalletError.CreateFailed
                );

            // Ensure user has wallet entropy
            var walletEntropy = LinkedAccounts.WalletEntropyOrNull() ??
                                throw new PrivyWalletException(
                                    "A user must have a primary wallet before creating a wallet at HD indices >= 1.",
                                    EmbeddedWalletError.CreateFailed
                                );

            // If there is already an existing wallet with the specified HD index, no need to create it
            var existingWalletWithHdIndex =
                solWalletAccounts.FirstOrDefault(account => account.WalletIndex == hdWalletIndex);
            if (existingWalletWithHdIndex != null)
            {
                PrivyLogger.Debug($"Wallet with specified HD index ({hdWalletIndex}) already exists.");
                return await PrepareAndConnectSolanaWallet(existingWalletWithHdIndex.Address);
            }

            // Create HD wallet at specified index
            string accessToken = await _authDelegator.GetAccessToken();
            string hdWalletAddress = await _embeddedWalletManager.CreateAdditionalWallet(accessToken: accessToken,
                walletEntropy: walletEntropy, chainType: ChainType.Solana, hdWalletIndex: hdWalletIndex);

            return await PrepareAndConnectSolanaWallet(hdWalletAddress);
        }
    }
}
