using Privy.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Privy.Wallets
{
    internal class EmbeddedEthereumWalletProvider : IEmbeddedEthereumWalletProvider
    {
        private readonly IRpcExecutor _rpcExecutor;

        private static readonly HashSet<string> _allowedMethods = new HashSet<string>
        {
            "eth_sign",
            "secp256k1_sign",
            "personal_sign",
            "eth_populateTransactionRequest",
            "eth_signTypedData_v4",
            "eth_signTransaction",
            "eth_sendTransaction"
        };

        public EmbeddedEthereumWalletProvider(IRpcExecutor rpcExecutor)
        {
            _rpcExecutor = rpcExecutor;
        }

        public async Task<RpcResponse> Request(RpcRequest request)
        {
            if (_allowedMethods.Contains(request.Method))
            {
                var requestDetails = new RpcRequestData.EthereumRpcRequestDetails
                {
                    Method = request.Method,
                    Params = request.Params
                };
                var responseDetails = await _rpcExecutor.Evaluate(requestDetails);

                if (responseDetails is RpcResponseData.EthereumRpcResponseDetails response)
                {
                    return new RpcResponse
                    {
                        Method = response.Method,
                        Data = response.Data
                    };
                }

                throw new PrivyWalletException($"Failed to execute RPC Request",
                    EmbeddedWalletError.RpcRequestFailed);
            }
            else
            {
                return await HandleJsonRpc(request);
            }
        }

        private Task<RpcResponse> HandleJsonRpc(RpcRequest request)
        {
            PrivyLogger.Debug("Unsupported rpc request type");
            // returning null would lead to NRE in callers; throw a descriptive exception instead
            // this should never happen because the UI should prevent unsupported methods from being requested in the first place, but we want to fail gracefully just in case
            throw new PrivyWalletException("Unsupported RPC method", EmbeddedWalletError.RpcRequestFailed);
        }
    }
}
