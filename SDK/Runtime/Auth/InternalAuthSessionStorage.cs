using System;
using Newtonsoft.Json;
using Privy.Utils;
using Privy.Internal.Session;
using Privy.Internal.Storage;
using Privy.Auth.Models;
using UnityEngine;

namespace Privy.Auth
{
    internal class InternalAuthSessionStorage
    {
        private readonly IAuthSessionStore _sessionStore;
        private readonly IPlayerPrefsDataManager _legacyStore;
        private readonly string _appId;
        private bool _restoreDisabled;
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        };

        internal InternalAuthSessionStorage(IAuthSessionStore sessionStore,
            IPlayerPrefsDataManager legacyStore, string appId)
        {
            _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
            _legacyStore = legacyStore;
            _appId = appId ?? throw new ArgumentNullException(nameof(appId));
        }

        internal InternalAuthSession RetrieveInternalAuthSessionFromStorage()
        {
            try
            {
                if (_restoreDisabled) return null;
                var storedValue = _sessionStore.Read();
                if (!string.IsNullOrEmpty(storedValue))
                {
                    var session = DeserializeSession(storedValue);
                    if (session == null) _sessionStore.Delete();
                    return session;
                }

                // Only native stores migrate out of the legacy preferences backend.
                if (_legacyStore == null) return null;
                var legacyValue = _legacyStore.LoadData<string>(Constants.INTERNAL_AUTH_SESSION_KEY);
                var legacySession = DeserializeSession(legacyValue);
                if (legacySession == null) return null;

                // Remove legacy data only after a verified secure write. The finally block also
                // removes it on failure, requiring a new login instead of a plaintext fallback.
                WriteAndVerify(legacySession);
                return legacySession;
            }
            catch (Exception)
            {
                _restoreDisabled = true;
                throw StorageUnavailable();
            }
            finally
            {
                PurgeLegacySession();
            }
        }

        internal void SaveInternalAuthSessionInStorage(InternalAuthSession internalAuthSession)
        {
            try
            {
                WriteAndVerify(internalAuthSession);
                _restoreDisabled = false;
            }
            catch (Exception)
            {
                _restoreDisabled = true;
                throw StorageUnavailable();
            }
            finally
            {
                PurgeLegacySession();
            }
        }

        internal void ClearInternalAuthSessionInStorage()
        {
            _restoreDisabled = true;
            try
            {
                _sessionStore.Delete();
            }
            catch (Exception)
            {
                throw StorageUnavailable();
            }
            finally
            {
                PurgeLegacySession();
            }
        }

        private void WriteAndVerify(InternalAuthSession session)
        {
            var verified = false;
            try
            {
                var serialized = JsonConvert.SerializeObject(session, _jsonSettings);
                if (DeserializeSession(serialized) == null) throw StorageUnavailable();
                _sessionStore.Write(serialized);
                if (!string.Equals(_sessionStore.Read(), serialized, StringComparison.Ordinal))
                {
                    throw StorageUnavailable();
                }
                verified = true;
            }
            finally
            {
                if (!verified) _sessionStore.Delete();
            }
        }

        private InternalAuthSession DeserializeSession(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            try
            {
                var session = JsonConvert.DeserializeObject<InternalAuthSession>(value, _jsonSettings);
                var token = Token.Parse(session?.AccessToken);
                // The old PlayerPrefs key was global. Never migrate another Privy app's session.
                return session?.User != null && !string.IsNullOrEmpty(session.User.Id) &&
                       !string.IsNullOrEmpty(session.RefreshToken) &&
                       token?.Audience == _appId && token.Subject == session.User.Id
                    ? session
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void PurgeLegacySession()
        {
            if (_legacyStore == null) return;
            try
            {
                _legacyStore.DeleteData(Constants.INTERNAL_AUTH_SESSION_KEY);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
                _restoreDisabled = true;
                // A failed plaintext cleanup must not leave a new login eligible for restoration.
                try
                {
                    _sessionStore.Delete();
                }
                finally
                {
                    throw StorageUnavailable();
                }
            }
        }

        private static PrivyAuthenticationException StorageUnavailable() =>
            new PrivyAuthenticationException("Authentication session storage is unavailable.",
                AuthenticationError.StorageUnavailable);
    }
}
