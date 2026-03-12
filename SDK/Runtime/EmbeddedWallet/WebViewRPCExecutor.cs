using System;
using System.Threading.Tasks;
using Privy.Utils;
using static Privy.Wallets.RpcRequestData;
using static Privy.Wallets.RpcResponseData;


namespace Privy.Wallets
{
    internal class WebViewRPCExecutor : IRpcExecutor
    {
        private readonly WalletEntropy _entropy;
        private readonly int _hdWalletIndex;
        private readonly EmbeddedWalletManager _embeddedWalletManager;
        private readonly SolanaJsonRpcClient _solanaJsonRpcClient;

        internal WebViewRPCExecutor(WalletEntropy walletEntropy, int hdWalletIndex,
            EmbeddedWalletManager embeddedWalletManager)
        {
            _entropy = walletEntropy;
            _hdWalletIndex = hdWalletIndex;
            _embeddedWalletManager = embeddedWalletManager;
            _solanaJsonRpcClient = new SolanaJsonRpcClient();
        }

        async Task<RpcResponseData.IRpcResponseDetails> IRpcExecutor.Evaluate(RpcRequestData.IRpcRequestDetails request)
        {
            // Intercept Solana transaction operations which require client-side decomposition
            // before forwarding to the webview (the iframe only handles signMessage for Solana).
            if (request is SolanaRpcRequestDetails solanaRequest)
            {
                switch (solanaRequest.Method)
                {
                    case "signTransaction":
                        return await SignTransactionClientSide(solanaRequest);
                    case "signAndSendTransaction":
                        return await SignAndSendTransactionClientSide(solanaRequest);
                }
            }

            var chainType = request switch
            {
                EthereumRpcRequestDetails => ChainType.Ethereum,
                SolanaRpcRequestDetails => ChainType.Solana,
                _ => throw new PrivyWalletException("Unsupported wallet request type.",
                    EmbeddedWalletError.RpcRequestFailed)
            };
            return await _embeddedWalletManager.Request(_entropy, chainType, _hdWalletIndex, request);
        }

        /// <summary>
        /// Signs a Solana transaction client-side by:
        /// 1. Decoding the base64 transaction bytes
        /// 2. Parsing out the message portion via <see cref="SolanaTransaction"/>
        /// 3. Signing just the message bytes through the webview's signMessage
        /// 4. Reassembling the signed transaction ([0x01][signature][message])
        /// </summary>
        private async Task<IRpcResponseDetails> SignTransactionClientSide(SolanaRpcRequestDetails request)
        {
            if (request.Params is not SolanaSignTransactionRpcRequestParams txParams ||
                string.IsNullOrEmpty(txParams.Transaction))
            {
                throw new PrivyWalletException(
                    "signTransaction requires a valid SolanaSignTransactionRpcRequestParams",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            byte[] txBytes = Convert.FromBase64String(txParams.Transaction);
            var transaction = new SolanaTransaction(txBytes);

            string signedTxBase64 = await SignMessageAndReassemble(transaction);

            return new SolanaRpcResponseDetails
            {
                Method = "signTransaction",
                Data = new SolanaSignTransactionRpcResponseData { SignedTransaction = signedTxBase64 }
            };
        }

        /// <summary>
        /// Signs a Solana transaction client-side (same as <see cref="SignTransactionClientSide"/>)
        /// and then broadcasts it to the Solana network via <see cref="SolanaJsonRpcClient"/>.
        /// </summary>
        private async Task<IRpcResponseDetails> SignAndSendTransactionClientSide(SolanaRpcRequestDetails request)
        {
            if (request.Params is not SolanaSignAndSendTransactionRpcRequestParams txParams ||
                string.IsNullOrEmpty(txParams.Transaction) ||
                string.IsNullOrEmpty(txParams.Cluster))
            {
                throw new PrivyWalletException(
                    "signAndSendTransaction requires a valid SolanaSignAndSendTransactionRpcRequestParams with a cluster RPC URL",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            byte[] txBytes = Convert.FromBase64String(txParams.Transaction);
            var transaction = new SolanaTransaction(txBytes);

            string signedTxBase64 = await SignMessageAndReassemble(transaction);

            SolanaSendOptions sendOptions = txParams.Options == null ? null : new SolanaSendOptions
            {
                SkipPreflight = txParams.Options.SkipPreflight,
                PreflightCommitment = txParams.Options.PreflightCommitment,
                MaxRetries = txParams.Options.MaxRetries,
                MinContextSlot = txParams.Options.MinContextSlot
            };

            string txHash = await _solanaJsonRpcClient.SendTransaction(
                signedTxBase64, txParams.Cluster, sendOptions);

            return new SolanaRpcResponseDetails
            {
                Method = "signAndSendTransaction",
                Data = new SolanaSignAndSendTransactionRpcResponseData { Hash = txHash }
            };
        }

        /// <summary>
        /// Signs the message portion of a parsed <see cref="SolanaTransaction"/> via the webview's
        /// <c>signMessage</c> endpoint and reassembles the fully signed transaction bytes.
        /// </summary>
        /// <returns>Base64-encoded signed transaction.</returns>
        private async Task<string> SignMessageAndReassemble(SolanaTransaction transaction)
        {
            string messageBase64 = Convert.ToBase64String(transaction.Message);

            var signMessageRequest = new SolanaRpcRequestDetails
            {
                Method = "signMessage",
                Params = new SolanaSignMessageRpcRequestParams { Message = messageBase64 }
            };

            var signResponse = await _embeddedWalletManager.Request(
                _entropy, ChainType.Solana, _hdWalletIndex, signMessageRequest);

            if (signResponse is not SolanaRpcResponseDetails signatureDetails ||
                signatureDetails.Data is not SolanaSignMessageRpcResponseData signatureData ||
                string.IsNullOrEmpty(signatureData.Signature))
            {
                throw new PrivyWalletException(
                    "Failed to obtain signature from webview signMessage",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            byte[] signatureBytes = Convert.FromBase64String(signatureData.Signature);
            byte[] signedTxBytes = transaction.AddSignature(signatureBytes);
            return Convert.ToBase64String(signedTxBytes);
        }
    }
}

