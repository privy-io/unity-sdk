using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Privy.Auth.Models;
using Privy.Auth.Mapping;
using Privy.Auth.Converters;
using Privy.Internal.Networking;
using Privy.Utils;

namespace Privy.Auth
{
    internal class AuthRepository : IAuthRepository
    {
        private IHttpRequestHandler _httpRequestHandler;

        public AuthRepository(IHttpRequestHandler httpRequestHandler)
        {
            _httpRequestHandler = httpRequestHandler;
        }

        public async Task<bool> SendEmailCode(string email)
        {
            var requestData = new SendCodeRequestData
            {
                Email = email
            };


            string serializedRequest = JsonConvert.SerializeObject(requestData);

            string path = "passwordless/init";

            // Execute the request

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);
                var response = JsonConvert.DeserializeObject<SendCodeResponseData>(jsonResponse);
                return response.Success; //this response could be failure, which is why return type is a bool
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to send email code: {ex.Message}",
                    AuthenticationError.SendCodeFailed, ex);
            }
        }

        public async Task<InternalAuthSession> LoginWithEmailCode(string email, string code)
        {
            //Construct Request Data + Path
            var requestData = new LogInRequestData
            {
                Email = email,
                Code = code
            };

            string path = "passwordless/authenticate";

            //Serialize the request data
            string serializedRequest = JsonConvert.SerializeObject(requestData);


            // errors from the HTTP layer are caught and re‑wrapped below
            try
            {
                // Execute the request
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                //Deserialize Response
                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                //Mapping To Internal
                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);

                return _internalAuthSession;
            }
            catch (Exception ex) when (ex.Message.Contains("422"))
            {
                // server returned a 422 Unprocessable Entity, which means the OTP was wrong
                throw new PrivyAuthenticationException("Incorrect OTP code.",
                    AuthenticationError.IncorrectOtpCode);
            }
            catch (Exception ex)
            {
                // this catch now handles any other underlying failure (network, deserialization, etc.)
                throw new PrivyAuthenticationException($"Failed to login with email code: {ex.Message}",
                    AuthenticationError.InternalError, ex);
            }
        }

        public async Task<InitiateOAuthFlowResponse> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge,
            string redirectUri, string stateCode)
        {
            var requestData = new InitiateOAuthFlowRequestData
            {
                ProviderName = provider,
                CodeChallenge = codeChallenge,
                RedirectUri = redirectUri,
                StateCode = stateCode
            };

            string path = "oauth/init";

            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                InitiateOAuthFlowResponse initOauthResponse =
                    JsonConvert.DeserializeObject<InitiateOAuthFlowResponse>(jsonResponse);

                return initOauthResponse;
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to initiate OAuth: {ex.Message}",
                    AuthenticationError.OAuthInitFailed, ex);
            }
        }

        public async Task<InternalAuthSession> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier,
            string stateCode, bool isRawFlow = false)
        {
            var requestData = new AuthenticateOAuthFlowRequestData
            {
                AuthorizationCode = authorizationCode,
                CodeVerifier = codeVerifier,
                StateCode = stateCode,
                CodeType = isRawFlow ? AuthenticateOAuthFlowCodeType.Raw : null
            };

            string path = "oauth/authenticate";

            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);

                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);
                return _internalAuthSession;
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to authenticate with OAuth: {ex.Message}",
                    AuthenticationError.OAuthAuthenticateFailed, ex);
            }
        }

        public async Task<InternalAuthSession> RefreshSession(string accessToken, string refreshToken)
        {
            //This Refresh Session is called by multiple methods
            //It's called on initialize by the Restore, but also called by CreateWallet, to initially get a valid access token + to refresh after wallet creation
            string path = "sessions";


            SendRefreshRequestData requestData = new SendRefreshRequestData
            {
                RefreshToken = refreshToken
            };


            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

            //Serialize the request data
            string serializedRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                // Execute the request
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest, headers);

                // Deserialize Response
                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);

                // Mapping To Internal
                InternalAuthSession _internalAuthSession = AuthSessionResponseMapper.MapToInternalSession(authResponse);

                return _internalAuthSession;
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to refresh session: {ex.Message}",
                    AuthenticationError.RefreshFailed, ex);
            }
        }

        private ValidSessionResponse DeserializeSessionResponse(string jsonResponse)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Converters = new List<JsonConverter> { new LinkedAccountConverter() }
            };

            ValidSessionResponse authResponse =
                JsonConvert.DeserializeObject<ValidSessionResponse>(jsonResponse, settings);
            return authResponse;
        }

        private UserResponse DeserializeUserResponse(string jsonResponse)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Converters = new List<JsonConverter> { new LinkedAccountConverter() }
            };

            return JsonConvert.DeserializeObject<UserResponse>(jsonResponse, settings);
        }

        public async Task<bool> SendSmsCode(string phoneNumber)
        {
            var requestData = new SendSmsCodeRequestData
            {
                PhoneNumber = phoneNumber
            };

            string serializedRequest = JsonConvert.SerializeObject(requestData);
            string path = "passwordless_sms/init";

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);
                var response = JsonConvert.DeserializeObject<SendCodeResponseData>(jsonResponse);
                return response.Success;
            }
            catch (Exception ex) when (ex.Message.Contains("422"))
            {
                // backend returns 422 for invalid phone format or similar
                throw new PrivyAuthenticationException("Invalid phone number format.",
                    AuthenticationError.InvalidPhoneNumber, ex);
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to send SMS code: {ex.Message}",
                    AuthenticationError.SendCodeFailed, ex);
            }
        }

        public async Task<InternalAuthSession> LoginWithSmsCode(string phoneNumber, string code)
        {
            var requestData = new SmsLoginRequestData
            {
                PhoneNumber = phoneNumber,
                Code = code
            };

            string serializedRequest = JsonConvert.SerializeObject(requestData);
            string path = "passwordless_sms/authenticate";

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest);
                ValidSessionResponse authResponse = DeserializeSessionResponse(jsonResponse);
                return AuthSessionResponseMapper.MapToInternalSession(authResponse);
            }
            catch (Exception ex) when (ex.Message.Contains("422"))
            {
                throw new PrivyAuthenticationException("Incorrect OTP code.",
                    AuthenticationError.IncorrectOtpCode);
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to login with SMS code: {ex.Message}",
                    AuthenticationError.WrongOtpCode, ex);
            }
        }

        public async Task<InternalPrivyUser> LinkSms(string phoneNumber, string code, string accessToken)
        {
            var requestData = new SmsLinkRequestData
            {
                PhoneNumber = phoneNumber,
                Code = code
            };

            string serializedRequest = JsonConvert.SerializeObject(requestData);
            string path = "passwordless_sms/link";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest, headers);
                UserResponse userResponse = DeserializeUserResponse(jsonResponse);
                return AuthSessionResponseMapper.MapToInternalUser(userResponse);
            }
            catch (Exception ex) when (ex.Message.Contains("422"))
            {
                throw new PrivyAuthenticationException("Incorrect OTP code.",
                    AuthenticationError.IncorrectOtpCode);
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to link SMS: {ex.Message}",
                    AuthenticationError.LinkFailed, ex);
            }
        }

        public async Task<InternalPrivyUser> UpdateSmsPhoneNumber(string phoneNumber, string code, string accessToken)
        {
            var requestData = new SmsUpdateRequestData
            {
                PhoneNumber = phoneNumber,
                Code = code
            };

            string serializedRequest = JsonConvert.SerializeObject(requestData);
            string path = "passwordless_sms/update";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest, headers);
                UserResponse userResponse = DeserializeUserResponse(jsonResponse);
                return AuthSessionResponseMapper.MapToInternalUser(userResponse);
            }
            catch (Exception ex) when (ex.Message.Contains("422"))
            {
                throw new PrivyAuthenticationException("Incorrect OTP code.",
                    AuthenticationError.IncorrectOtpCode);
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to update SMS phone number: {ex.Message}",
                    AuthenticationError.LinkFailed, ex);
            }
        }

        public async Task<InternalPrivyUser> UnlinkSms(string phoneNumber, string accessToken)
        {
            var requestData = new SmsUnlinkRequestData
            {
                PhoneNumber = phoneNumber
            };

            string serializedRequest = JsonConvert.SerializeObject(requestData);
            string path = "passwordless_sms/unlink";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

            try
            {
                string jsonResponse = await _httpRequestHandler.SendRequestAsync(path, serializedRequest, headers);
                UserResponse userResponse = DeserializeUserResponse(jsonResponse);
                return AuthSessionResponseMapper.MapToInternalUser(userResponse);
            }
            catch (Exception ex)
            {
                throw new PrivyAuthenticationException($"Failed to unlink SMS: {ex.Message}",
                    AuthenticationError.UnlinkFailed, ex);
            }
        }
    }
}
