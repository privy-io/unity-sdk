using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Privy
{
    internal class WebViewManager : IDisposable
    {
        private readonly IWebViewHandler _webViewHandler;
        private TaskCompletionSource<bool> _readyTcs = new TaskCompletionSource<bool>();

        // map from request ID to the completion source; may be accessed on different threads
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _requestResponseMap =
            new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public WebViewManager(PrivyConfig privyConfig)
        {
            _webViewHandler = IWebViewHandler.GetPlatformWebViewHandler(this, privyConfig);

            _webViewHandler.LoadUrl(
                $"{PrivyEnvironment.BASE_URL}/apps/{privyConfig.AppId}/embedded-wallets?client_id={privyConfig.ClientId}");
        }

        internal void OnWebViewReady()
        {
            //Receives message from the source
            PrivyLogger.Debug("WebView is ready.");
            if (!_readyTcs.Task.IsCompleted)
            {
                _readyTcs.TrySetResult(true); //No effect if this is called multiple times, due to the "try"
            }
        }


        // Handle messages received from the WebView
        internal void OnMessageReceived(string message)
        {
            //Here we take a message, deserialize it into an IFrame Response and then parse the relevant fields

            PrivyLogger.Debug("Message from WebView: " + message);

            // attempt to parse as JSON; if that fails and the string contains '%', we
            // assume it was URL-encoded (observed on Android) and decode. avoids
            // false positives when '%' appears legitimately in JSON.
            bool parsed = true;
            try
            {
                JsonConvert.DeserializeObject<object>(message);
            }
            catch
            {
                parsed = false;
            }

            if (!parsed && message.Contains("%"))
            {
                message = Uri.UnescapeDataString(message);
                PrivyLogger.Debug("Decoded message from WebView: " + message);
            }

            //Generic Response Parsing, for the event and ID
            var messageResponse = JsonConvert.DeserializeObject<IframeResponse>(message);

            string @event = messageResponse.Event;
            string id = messageResponse.Id;

            if (_requestResponseMap.TryRemove(id, out var tcs))
            {
                tcs.TrySetResult(message);
            }
            else
            {
                // This happens if the request already timed out and was removed
                PrivyLogger.Error($"No matching task found for ID: {id}");
            }
        }


        // generic helper: sends a request and attempts to deserialize the response as either
        // success (TRes) or error. callers simply inspect the returned IframeResponse.
        private async Task<IframeResponse> SendRequest<TReq, TRes>(IframeRequest<TReq> request, double seconds = 30, CancellationToken cancellationToken = default)
        {
            if (request.Event != PrivyEventType.Ready)
            {
                // for all requests except for "ping ready", we need to await until web view is ready
                await AwaitReady();
            }

            var tcs = new TaskCompletionSource<string>();
            _requestResponseMap[request.Id] = tcs;

            //POTENTIAL NEW LOGIC
            //Could use a CancellationTokenSource here
            //We could link the task completion source to the cancellation source
            //the cts allows us to acess a "cancelafter" property, which mimics a timeout
            //On cancellation we can throw the error
            //cancellation would only happen if timeout finishes, AND the task isn't currently complete        

            string serializedRequest = JsonConvert.SerializeObject(request);

            // log only non-sensitive fields
            PrivyLogger.Debug($"Message to WebView: {{event:{request.Event}, id:{request.Id}}}");

            _webViewHandler.SendMessage(serializedRequest); //Sends message based on if it's the iframe or the webview

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(seconds));
            var cancellationTask = cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.Delay(Timeout.Infinite, cancellationToken);

            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask, cancellationTask);

            if (completedTask == cancellationTask)
            {
                _requestResponseMap.TryRemove(request.Id, out _);
                throw new OperationCanceledException(cancellationToken);
            }

            if (completedTask == timeoutTask)
            {
                PrivyLogger.Error("Request timed out.");
                _requestResponseMap.TryRemove(request.Id, out _);
                return new IframeResponseError
                {
                    Id = request.Id,
                    Event = request.Event,
                    Error = new ErrorDetails
                    {
                        Type = "timeout_error",
                        Message = "The request timed out."
                    },
                } as IframeResponse;
            }

            string responseString = await tcs.Task;
            // first try interpreting as an explicit error response
            var maybeError = JsonConvert.DeserializeObject<IframeResponseError>(responseString);
            if (maybeError?.Error != null)
            {
                return maybeError;
            }

            // after performing a structural error check above, we already know this is not an
            // error value. try to deserialize into the expected response type.
            try
            {
                return JsonConvert.DeserializeObject<IframeResponseSuccess<TRes>>(responseString);
            }
            catch (Exception ex)
            {
                // if the response didn't match the expected shape, return a generic error
                return new IframeResponseError
                {
                    Id = request.Id,
                    Event = request.Event,
                    Error = new ErrorDetails
                    {
                        Type = "deserialization_error",
                        Message = ex.Message
                    },
                } as IframeResponse;
            }
        }

        internal async Task PingReadyUntilSuccessful(CancellationToken cancellationToken = default)
        {
            //NOTE: Semaphore has been removed here, as this is only called once, and we want to prevent WebGL issues, as WebGL does not support multi-threading
            //Essentially, we're using a Task Completion Source to know when the Webview is ready
            //So our await ready is just waiting for the value of the Task Completion source to be set, it doesn't PingReady again
            //However, there is a chance in the future, that we need to call PingReadyUntilSuccessful from multiple places, and in that scenario we may need to re-introduce locking        
            if (_readyTcs.Task.IsCompleted)
            {
                PrivyLogger.Debug("Already ready");
                return;
            }

            PrivyLogger.Debug("Ping Ready until success invoked");

            while (!_readyTcs.Task.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    PrivyLogger.Debug("PingReadyUntilSuccessful canceled");
                    cancellationToken.ThrowIfCancellationRequested();
                }

                PrivyLogger.Debug("NOT COMPLETED");
                // Send a message to the WebView to check if it's ready

                try
                {
                    //We've added functionality to have PingReady return Error Responses, which we don't need to handle, since the loop will re-trigger
                    //However Ping Ready could potentially throw an error if there is an error in serialization, so we can rap it in a try catch, because this wouldn't be caught
                    //So we wrap in a try catch for that example, although unlikely
                    var result = await PingReady(cancellationToken);

                    if (result is IframeResponseSuccess<ReadyResponseData>)
                    {
                        //If this never executes, this loop runs indefinitely
                        // Handle the success response
                        PrivyLogger.Debug("Success Response Received: WebView is ready.");
                        _readyTcs.TrySetResult(true); //No effect if this is called multiple times, due to the "try"
                    }
                }
                catch (Exception)
                {
                    //Fail silently, to have loop continue
                    PrivyLogger.Debug("Success response not received, pinging again");
                }
                // back off slightly to avoid busy-spin
                await Task.Delay(100);
            }

            PrivyLogger.Debug("Task completed");
        }

        private async Task<IframeResponse> PingReady(CancellationToken cancellationToken = default)
        {
            var readyRequest = new IframeRequest<ReadyRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Ready,
                Data = new ReadyRequestData { }
            };

            var res = await SendRequest<ReadyRequestData, ReadyResponseData>(readyRequest, 0.15, cancellationToken);

            // Ensure the response is of the correct type
            if (res is IframeResponseSuccess<ReadyResponseData> successResponse)
            {
                return successResponse;
            }
            else if (res is IframeResponseError errorResponse)
            {
                //This will get triggered if there's any timeouts, or webview returns an error response
                return errorResponse;
            }
            else
            {
                //Fail silently if we receive an error from webview we don't know how to handle
                //This will trigger another ping
                //Throwing an exception here won't be caught, and there's nothing to send back to the developer, so makes sense to do this
                return null;
            }
        }

        private async Task AwaitReady()
        {
            // after this function executes, it indicates that the webview has sent a message back to unity with the "privy:iframe:ready"
            //usage
            // await WebViewManager.AwaitReady()
            //now ready to send additional messages        


            //Enhancement: Add timeout here, in case the ready state never updates - to prevent indefinite requests
            // You can perform additional async operations here if needed
            await _readyTcs.Task;
        }
        /// <summary>
        /// Cancel any outstanding requests and release resources held by the underlying handler.
        /// </summary>
        public void Dispose()
        {
            // cancel pending requests
            foreach (var kv in _requestResponseMap)
            {
                kv.Value.TrySetException(new ObjectDisposedException("WebViewManager"));
            }
            _requestResponseMap.Clear();

            // the ready task may never complete; cancel it if possible by recreating
            if (!_readyTcs.Task.IsCompleted)
            {
                _readyTcs.TrySetCanceled();
            }

            if (_webViewHandler is IDisposable disp)
            {
                disp.Dispose();
            }
        }

        //Wallet Methods
        //TBD
        internal async Task<IframeResponse> CreateEthereumWallet(string accessToken, string solanaAddress = null, CancellationToken cancellationToken = default)
        {
            var data = new CreateEthereumWalletRequestData
            {
                AccessToken = accessToken,
                SolanaAddress = solanaAddress
            };

            var createWalletRequest = new IframeRequest<CreateEthereumWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateEthereumWallet,
                Data = data
            };

            return await SendRequest<CreateEthereumWalletRequestData, CreateEthereumWalletResponseData>(createWalletRequest, 60, cancellationToken);
        }

        internal async Task<IframeResponse> CreateSolanaWallet(string accessToken, string ethereumAddress = null, CancellationToken cancellationToken = default)
        {
            var data = new CreateSolanaWalletRequestData
            {
                AccessToken = accessToken,
                EthereumAddress = ethereumAddress
            };

            var createWalletRequest = new IframeRequest<CreateSolanaWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateSolanaWallet,
                Data = data
            };

            return await SendRequest<CreateSolanaWalletRequestData, CreateSolanaWalletResponseData>(createWalletRequest, 60, cancellationToken);
        }

        internal async Task<IframeResponse> CreateAdditionalWallet(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, CancellationToken cancellationToken = default)
        {
            var data = new CreateAdditionalWalletRequestData
            {
                AccessToken = accessToken,
                ChainType = chainType,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                WalletIndex = hdWalletIndex
            };

            var createWalletRequest = new IframeRequest<CreateAdditionalWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.CreateAdditional,
                Data = data
            };

            return await SendRequest<CreateAdditionalWalletRequestData, CreateAdditionalWalletResponseData>(createWalletRequest, 60, cancellationToken);
        }


        internal async Task<IframeResponse> ConnectWallet(string accessToken, WalletEntropy walletEntropy, CancellationToken cancellationToken = default)
        {
            var data = new ConnectWalletRequestData
            {
                AccessToken = accessToken,
                ChainType = ChainType.Ethereum,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
            };

            var connectWalletRequest = new IframeRequest<ConnectWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Connect,
                Data = data
            };

            return await SendRequest<ConnectWalletRequestData, ConnectWalletResponseData>(connectWalletRequest);
        }

        internal async Task<IframeResponse> RecoverWallet(string accessToken, WalletEntropy walletEntropy)
        {
            var data = new RecoverWalletRequestData
            {
                AccessToken = accessToken,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName()
            };

            var recoverWalletRequest = new IframeRequest<RecoverWalletRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Recover,
                Data = data
            };

            return await SendRequest<RecoverWalletRequestData, RecoverWalletResponseData>(recoverWalletRequest);
        }

        internal async Task<IframeResponse> Request(string accessToken, WalletEntropy walletEntropy,
            ChainType chainType, int hdWalletIndex, RpcRequestData.IRpcRequestDetails request)
        {
            var data = new RpcRequestData
            {
                AccessToken = accessToken,
                ChainType = chainType,
                EntropyId = walletEntropy.Id,
                EntropyIdVerifier = walletEntropy.Verifier.ToVerifierName(),
                WalletIndex = hdWalletIndex,
                Request = request
            };

            var rpcRequest = new IframeRequest<RpcRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.Rpc,
                Data = data
            };

            return await SendRequest<RpcRequestData, RpcResponseData>(rpcRequest);
        }

        internal async Task<IframeResponse> SignWithUserSigner(string accessToken, byte[] message)
        {
            var data = new UserSignerSignRequestData
            {
                AccessToken = accessToken,
                Message = Convert.ToBase64String(message)
            };

            var userSignerSignRequest = new IframeRequest<UserSignerSignRequestData>
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Event = PrivyEventType.SignWithUserSigner,
                Data = data
            };

            return await SendRequest<UserSignerSignRequestData, UserSignerSignResponseData>(userSignerSignRequest);
        }
    }
}
