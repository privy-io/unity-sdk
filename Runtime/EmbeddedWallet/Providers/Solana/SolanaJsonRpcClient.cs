using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Privy
{
    /// <summary>
    /// Lightweight Solana JSON-RPC client used to broadcast signed transactions
    /// to the Solana network from on-device (WebView) wallets.
    /// </summary>
    internal class SolanaJsonRpcClient
    {
        /// <summary>
        /// Broadcasts a signed transaction to the Solana network via the JSON-RPC
        /// <c>sendTransaction</c> method and returns the transaction signature (hash).
        /// </summary>
        /// <param name="signedTransactionBase64">Base64-encoded signed transaction bytes.</param>
        /// <param name="rpcUrl">The Solana cluster RPC URL (e.g. from <see cref="SolanaCluster.RpcUrl"/>).</param>
        /// <param name="options">Optional send options; pass <c>null</c> to use cluster defaults.</param>
        /// <returns>The transaction signature string (base58-encoded hash).</returns>
        internal async Task<string> SendTransaction(
            string signedTransactionBase64,
            string rpcUrl,
            SolanaSendOptions options = null)
        {
            var configObj = new JObject { ["encoding"] = "base64" };
            if (options != null)
            {
                if (options.SkipPreflight.HasValue)
                    configObj["skipPreflight"] = options.SkipPreflight.Value;
                if (!string.IsNullOrEmpty(options.PreflightCommitment))
                    configObj["preflightCommitment"] = options.PreflightCommitment;
                if (options.MaxRetries.HasValue)
                    configObj["maxRetries"] = options.MaxRetries.Value;
                if (options.MinContextSlot.HasValue)
                    configObj["minContextSlot"] = options.MinContextSlot.Value;
            }

            var requestBody = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 1,
                ["method"] = "sendTransaction",
                ["params"] = new JArray { signedTransactionBase64, configObj }
            };

            string jsonBody = requestBody.ToString(Formatting.None);
            string responseText = await PostAsync(rpcUrl, jsonBody);

            var responseObj = JObject.Parse(responseText);

            var errorToken = responseObj["error"];
            if (errorToken != null && errorToken.Type != JTokenType.Null)
            {
                string errorMessage = errorToken["message"]?.Value<string>() ?? "Unknown Solana RPC error";
                throw new PrivyException.EmbeddedWalletException(
                    $"Solana RPC error: {errorMessage}",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            string txHash = responseObj["result"]?.Value<string>();
            if (string.IsNullOrEmpty(txHash))
            {
                throw new PrivyException.EmbeddedWalletException(
                    "Solana sendTransaction returned an empty result",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            return txHash;
        }

        private static async Task<string> PostAsync(string url, string jsonBody)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new PrivyException.EmbeddedWalletException(
                    $"HTTP error sending transaction to {url}: {request.error}",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            return request.downloadHandler.text;
        }
    }
}
