using System.Threading.Tasks;
using Privy.Utils;
using static Privy.Wallets.RpcRequestData;
using static Privy.Wallets.RpcResponseData;

namespace Privy.Wallets
{
    internal class EmbeddedSolanaWalletProvider : IEmbeddedSolanaWalletProvider
    {
        private readonly IRpcExecutor _rpcExecutor;

        internal EmbeddedSolanaWalletProvider(IRpcExecutor rpcExecutor)
        {
            _rpcExecutor = rpcExecutor;
        }

        public async Task<string> SignMessage(string message)
        {
            var request = new SolanaRpcRequestDetails
            {
                Method = "signMessage",
                Params = new SolanaSignMessageRpcRequestParams { Message = message }
            };

            var response = await _rpcExecutor.Evaluate(request);

            if (response is SolanaRpcResponseDetails solanaResponse &&
                solanaResponse.Data is SolanaSignMessageRpcResponseData signatureData)
                return signatureData.Signature;

            throw new PrivyWalletException("Failed to execute message signature",
                EmbeddedWalletError.RpcRequestFailed);
        }

        public async Task<string> SignTransaction(string transaction)
        {
            var request = new SolanaRpcRequestDetails
            {
                Method = "signTransaction",
                Params = new SolanaSignTransactionRpcRequestParams { Transaction = transaction }
            };

            var response = await _rpcExecutor.Evaluate(request);

            if (response is SolanaRpcResponseDetails solanaResponse &&
                solanaResponse.Data is SolanaSignTransactionRpcResponseData txData)
                return txData.SignedTransaction;

            throw new PrivyWalletException("Failed to sign transaction",
                EmbeddedWalletError.RpcRequestFailed);
        }

        public async Task<string> SignAndSendTransaction(
            string transaction,
            SolanaCluster cluster,
            SolanaSendOptions options = null)
        {
            var optionsParams = options == null ? null : new SolanaSignAndSendOptionsParams
            {
                SkipPreflight = options.SkipPreflight,
                PreflightCommitment = options.PreflightCommitment,
                MaxRetries = options.MaxRetries,
                MinContextSlot = options.MinContextSlot
            };

            var request = new SolanaRpcRequestDetails
            {
                Method = "signAndSendTransaction",
                Params = new SolanaSignAndSendTransactionRpcRequestParams
                {
                    Transaction = transaction,
                    Cluster = cluster.RpcUrl,
                    Options = optionsParams
                }
            };

            var response = await _rpcExecutor.Evaluate(request);

            if (response is SolanaRpcResponseDetails solanaResponse &&
                solanaResponse.Data is SolanaSignAndSendTransactionRpcResponseData hashData)
                return hashData.Hash;

            throw new PrivyWalletException("Failed to sign and send transaction",
                EmbeddedWalletError.RpcRequestFailed);
        }
    }
}

