using System.Threading.Tasks;

namespace Privy.Wallets
{
    internal interface IRpcExecutor
    {
        internal Task<RpcResponseData.IRpcResponseDetails> Evaluate(RpcRequestData.IRpcRequestDetails request);
    }
}
