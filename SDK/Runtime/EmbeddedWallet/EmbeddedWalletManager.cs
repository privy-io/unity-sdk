using System;
using System.Threading.Tasks;
using static Privy.PrivyException;

namespace Privy
{
    internal class EmbeddedWalletManager
    {
        private WebViewManager _webViewManager;
        private AuthDelegator _authDelegator;

        // utility for unwrapping IframeResponse values and throwing appropriate exceptions
        private static TRes UnwrapResponse<TRes>(IframeResponse res, EmbeddedWalletError errorType, string errorPrefix)
        {
            if (res is IframeResponseSuccess<TRes> success)
            {
                return success.Data;
            }

            if (res is IframeResponseError err)
            {
                throw new EmbeddedWalletException($"{errorPrefix}: {err.Error.Message}", errorType);
            }

            throw new EmbeddedWalletException(errorPrefix, errorType);
        }

        private EmbeddedWalletState _embeddedWalletState = new EmbeddedWalletState.Disconnected();

        private Task<string> _connectWalletTask;


        public EmbeddedWalletManager(WebViewManager webViewManager, AuthDelegator authDelegator)
        {
            _webViewManager = webViewManager;
            _authDelegator = authDelegator;

            _authDelegator.OnAuthStateChanged += HandleAuthStateChanged;
        }

        private async void HandleAuthStateChanged(AuthState newState)
        {
            if (newState == AuthState.Authenticated)
            {
                //IMPORTANT, we do not throw errors from here to the developer. They can't catch these errors, as the state change and connect wallet is happening internally
                //Instead, we throw the errors when the user executes an operation that requires a connected wallet
                try
                {
                    await AttemptConnectingToWallet(_authDelegator
                        .GetAuthSession()); //Await so we actually get the error
                }
                catch (Exception ex)
                {
                    PrivyLogger.Error("Could not connect wallet after authenticating", ex);
                }
            }
            else
            {
                PrivyLogger.Debug("Wallet Disconnected");
                _embeddedWalletState = new EmbeddedWalletState.Disconnected();
            }
        }

        // unsubscribe from the event when the manager is disposed of to avoid memory leaks
        public void Dispose()
        {
            _authDelegator.OnAuthStateChanged -= HandleAuthStateChanged;
        }

        internal async Task AttemptConnectingToWallet(InternalAuthSession authSession)
        {
            var primaryWallet = authSession.User.LinkedAccounts.PrimaryEmbeddedWalletAccountOrNull();
            var isOnDeviceWallet = primaryWallet?.IsOnDevice ?? false;
            if (!isOnDeviceWallet)
            {
                // Connection is not necessary for wallets that are not on-device
                return;
            }

            PrivyLogger.Debug("Attempting connect");
            var walletEntropy = authSession.User.LinkedAccounts.WalletEntropyOrNull() ??
                                throw new EmbeddedWalletException(
                                    $"Failed to connect wallet: wallet does not exist",
                                    EmbeddedWalletError
                                        .WalletDoesNotExist); //Let this bubble up to await connected or handle auth state change, as they are caught there

            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                //No need to connect
                return;
            }

            string token = await _authDelegator.GetAccessToken();
            await ConnectWallet(token, walletEntropy, true);
        }

        private async Task AwaitConnected()
        {
            if (_authDelegator.CurrentAuthState != AuthState.Authenticated)
            {
                throw new AuthenticationException("User is not authenticated.", AuthenticationError.NotAuthenticated);
            }

            await AttemptConnectingToWallet(_authDelegator.GetAuthSession());
        }


        internal async Task<string> CreateEthereumWallet(string accessToken, string solanaAddress = null)
        {
            var result = await _webViewManager.CreateEthereumWallet(accessToken, solanaAddress);
            var data = UnwrapResponse<CreateEthereumWalletResponseData>(
                result,
                EmbeddedWalletError.CreateFailed,
                "Failed to create wallet");
            return data.Address;
        }

        internal async Task<string> CreateSolanaWallet(string accessToken, string ethereumAddress = null)
        {
            var result = await _webViewManager.CreateSolanaWallet(accessToken);
            var data = UnwrapResponse<CreateSolanaWalletResponseData>(
                result,
                EmbeddedWalletError.CreateFailed,
                "Failed to create wallet");
            return data.PublicKey;
        }

        internal async Task<string> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex)
        {
            var result =
                await _webViewManager.CreateAdditionalWallet(accessToken, walletEntropy, chainType, hdWalletIndex);
            var data = UnwrapResponse<CreateAdditionalWalletResponseData>(
                result,
                EmbeddedWalletError.CreateAdditionalFailed,
                "Failed to create additional wallet");
            return data.Address;
        }

        internal async Task<string> ConnectWallet(string accessToken, WalletEntropy walletEntropy,
            bool attemptRecovery = false)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                return connectedState.WalletAddress;
            }

            // If a connection task is already in progress, return it
            if (_connectWalletTask != null)
            {
                return await _connectWalletTask;
            }

            // Otherwise, start a new connection task
            _connectWalletTask = ConnectWalletWithoutLock(accessToken, walletEntropy, attemptRecovery);

            try
            {
                // Await the connection task and return the result
                return await _connectWalletTask;
            }
            finally
            {
                // Clear the task when it's done, regardless of success or failure
                _connectWalletTask = null;
            }
        }

        private async Task<string> ConnectWalletWithoutLock(string accessToken, WalletEntropy walletEntropy,
            bool attemptRecovery)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Connected connectedState)
            {
                return connectedState.WalletAddress;
            }

            var result = await _webViewManager.ConnectWallet(accessToken, walletEntropy);

            if (result is IframeResponseError errorResponse)
            {
                PrivyLogger.Debug("Error Type: " + errorResponse.Error.Type);
                PrivyLogger.Debug("Error Message: " + errorResponse.Error.Message);

                // Check if we should attempt recovery and if the error type is "wallet_not_on_device"
                if (attemptRecovery && errorResponse.Error.Type == "wallet_not_on_device")
                {
                    PrivyLogger.Debug("Attempting Recovery due to wallet not being on device.");
                    return
                        await RecoverWalletThenTryConnecting(accessToken,
                            walletEntropy); //This could throw an error, which would be bubbled to connect, and caught
                }
                else
                {
                    // Handle other errors or return null
                    throw new PrivyException.EmbeddedWalletException(
                        $"Failed to connect wallet: {errorResponse.Error.Message}",
                        EmbeddedWalletError
                            .ConnectionFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
                }
            }

            else if (result is IframeResponseSuccess<ConnectWalletResponseData> walletResponse)
            {
                // EntropyId equals the wallet address in this case
                string connectedWalletAddress = walletResponse.Data.EntropyId;
                _embeddedWalletState = new EmbeddedWalletState.Connected(connectedWalletAddress);
                return connectedWalletAddress;
            }
            else
            {
                throw new PrivyException.EmbeddedWalletException($"Failed to connect wallet",
                    EmbeddedWalletError
                        .ConnectionFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
            }
        }


        internal async Task<string> RecoverWalletThenTryConnecting(string accessToken, WalletEntropy walletEntropy)
        {
            var result = await _webViewManager.RecoverWallet(accessToken, walletEntropy);
            var data = UnwrapResponse<RecoverWalletResponseData>(
                result,
                EmbeddedWalletError.RecoverFailed,
                "Failed to recover wallet");
            // Upon success simply retry connection without recovery flag
            return await ConnectWalletWithoutLock(accessToken, walletEntropy, false);
        }

        internal async Task<RpcResponseData.IRpcResponseDetails> Request(WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Disconnected)
            {
                await AwaitConnected();
            }

            string token = await _authDelegator.GetAccessToken();
            var result = await _webViewManager.Request(token, walletEntropy, chainType, hdWalletIndex, request);
            var data = UnwrapResponse<RpcResponseData>(
                result,
                EmbeddedWalletError.RpcRequestFailed,
                "Failed to execute RPC Request");
            return data.Response;
        }

        internal async Task<byte[]> SignWithUserSigner(string accessToken, byte[] message)
        {
            var result = await _webViewManager.SignWithUserSigner(accessToken, message);
            var data = UnwrapResponse<UserSignerSignResponseData>(
                result,
                EmbeddedWalletError.UserSignerRequestFailed,
                "Failed to sign with the user's authorization key");
            return Convert.FromBase64String(data.Signature);
        }
    }
}
