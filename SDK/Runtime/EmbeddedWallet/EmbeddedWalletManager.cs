using System;
using System.Threading.Tasks;
using Privy.Auth;
using Privy.Auth.Models;
using Privy.Utils;

namespace Privy.Wallets
{
    internal class EmbeddedWalletManager : IDisposable
    {
        private WebViewManager _webViewManager;
        private AuthDelegator _authDelegator;

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
                                throw new PrivyWalletException(
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
                throw new Exception("User is not authenticated."); //TBD change this to another exception
            }

            await AttemptConnectingToWallet(_authDelegator.GetAuthSession());
        }


        // Extracts TData from a successful IframeResponse, or throws PrivyWalletException.
        private static TData UnwrapResult<TData>(IframeResponse result, string errorMessage, EmbeddedWalletError errorCode)
        {
            if (result is IframeResponseSuccess<TData> success)
                return success.Data;

            var detail = (result as IframeResponseError)?.Error?.Message;

            throw new PrivyWalletException(
                detail != null ? $"{errorMessage}: {detail}" : errorMessage,
                errorCode);
        }

        internal async Task<string> CreateEthereumWallet(string accessToken, string solanaAddress = null)
        {
            var result = await _webViewManager.CreateEthereumWallet(accessToken, solanaAddress);
            return UnwrapResult<CreateEthereumWalletResponseData>(result, "Failed to create wallet", EmbeddedWalletError.CreateFailed).Address;
        }

        internal async Task<string> CreateSolanaWallet(string accessToken, string ethereumAddress = null)
        {
            var result = await _webViewManager.CreateSolanaWallet(accessToken);
            return UnwrapResult<CreateSolanaWalletResponseData>(result, "Failed to create wallet", EmbeddedWalletError.CreateFailed).PublicKey;
        }

        internal async Task<string> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex)
        {
            var result = await _webViewManager.CreateAdditionalWallet(accessToken, walletEntropy, chainType, hdWalletIndex);
            return UnwrapResult<CreateAdditionalWalletResponseData>(result, "Failed to create additional wallet", EmbeddedWalletError.CreateAdditionalFailed).Address;
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
                    throw new PrivyWalletException(
                        $"Failed to connect wallet: {errorResponse.Error.Message}",
                        EmbeddedWalletError
                            .ConnectionFailed); //Let this bubble up to HandleAuthStateChanged and AwaitConnected
                }
            }

            var data = UnwrapResult<ConnectWalletResponseData>(result, "Failed to connect wallet", EmbeddedWalletError.ConnectionFailed);
            // EntropyId equals the wallet address in this case
            _embeddedWalletState = new EmbeddedWalletState.Connected(data.EntropyId);
            return data.EntropyId;
        }


        internal async Task<string> RecoverWalletThenTryConnecting(string accessToken, WalletEntropy walletEntropy)
        {
            var result = await _webViewManager.RecoverWallet(accessToken, walletEntropy);
            UnwrapResult<RecoverWalletResponseData>(result, "Failed to recover wallet", EmbeddedWalletError.RecoverFailed);
            // After recovery, call ConnectWalletWithoutLock to avoid re-locking
            return await ConnectWalletWithoutLock(accessToken, walletEntropy, false);
        }

        internal async Task<RpcResponseData.IRpcResponseDetails> Request(WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            if (_embeddedWalletState is EmbeddedWalletState.Disconnected)
            {
                //This could throw an EmbeddedWalletException, due to a failure with connecting the wallet
                //Developer needs to catch this
                await AwaitConnected();
            }

            string token = await _authDelegator.GetAccessToken();
            var result = await _webViewManager.Request(token, walletEntropy, chainType, hdWalletIndex, request);
            return UnwrapResult<RpcResponseData>(result, "Failed to execute RPC Request", EmbeddedWalletError.RpcRequestFailed).Response;
        }

        internal async Task<byte[]> SignWithUserSigner(string accessToken, byte[] message)
        {
            var result = await _webViewManager.SignWithUserSigner(accessToken, message);
            var signature = UnwrapResult<UserSignerSignResponseData>(result, "Failed to sign with the user's authorization key", EmbeddedWalletError.UserSignerRequestFailed).Signature;
            return Convert.FromBase64String(signature);
        }
    }
}
