using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using Privy.Core;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Privy.Auth;
using Privy.Auth.Models;
using Privy.Internal.Storage;
using Privy.Utils;

namespace Privy.Tests
{
    public class AuthSessionStorageTests
    {
        private const string AppId = "test-app";
        private readonly List<string> _events = new List<string>();
        private RecordingStore _store;
        private LegacyStore _legacy;

        [SetUp]
        public void SetUp()
        {
            _events.Clear();
            _store = new RecordingStore(_events);
            _legacy = new LegacyStore(_events);
        }

        [Test]
        public void MigrationVerifiesSecureWriteBeforeDeletingLegacySession()
        {
            _legacy.Value = JsonConvert.SerializeObject(Session());
            var restored = Storage().RetrieveInternalAuthSessionFromStorage();
            Assert.That(restored.User.Id, Is.EqualTo("test-user"));
            Assert.That(_store.Value, Is.Not.Null);
            Assert.That(_legacy.Value, Is.Null);
            Assert.That(_events, Is.EqualTo(new[] { "read", "legacy-read", "write", "read", "legacy-delete" }));
        }

        [Test]
        public void FailedMigrationPurgesPlaintextWithoutReturningLegacySession()
        {
            _legacy.Value = JsonConvert.SerializeObject(Session());
            _store.FailWrite = true;
            var exception = Assert.Throws<PrivyAuthenticationException>(
                () => Storage().RetrieveInternalAuthSessionFromStorage());
            Assert.That(exception.Error, Is.EqualTo(AuthenticationError.StorageUnavailable));
            Assert.That(_legacy.Value, Is.Null);
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public void FailedReadbackRemovesUnverifiedSecureWriteAndPlaintext()
        {
            _legacy.Value = JsonConvert.SerializeObject(Session());
            _store.FailReadback = true;
            Assert.Throws<PrivyAuthenticationException>(() => Storage().RetrieveInternalAuthSessionFromStorage());
            Assert.That(_store.Value, Is.Null);
            Assert.That(_legacy.Value, Is.Null);
        }

        [Test]
        public void ExistingSecureSessionAlsoPurgesLegacyCopy()
        {
            _store.Value = JsonConvert.SerializeObject(Session());
            _legacy.Value = "stale plaintext session";
            Assert.That(Storage().RetrieveInternalAuthSessionFromStorage(), Is.Not.Null);
            Assert.That(_legacy.Value, Is.Null);
            Assert.That(_events, Does.Not.Contain("legacy-read"));
        }

        [TestCase("not json")]
        [TestCase("null")]
        public void InvalidSecureRecordIsRemoved(string value)
        {
            _store.Value = value;
            Assert.That(Storage().RetrieveInternalAuthSessionFromStorage(), Is.Null);
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public void LegacySessionFromAnotherAppIsRemovedWithoutMigration()
        {
            _legacy.Value = JsonConvert.SerializeObject(Session(appId: "other-app"));
            Assert.That(Storage().RetrieveInternalAuthSessionFromStorage(), Is.Null);
            Assert.That(_store.Value, Is.Null);
            Assert.That(_legacy.Value, Is.Null);
        }

        [Test]
        public void CompatibilityBackendPreservesSessionAcrossStorageInstances()
        {
            _legacy.AllowWrites = true;
            var backend = new PlayerPrefsAuthSessionStore(_legacy);
            new InternalAuthSessionStorage(backend, null, AppId).SaveInternalAuthSessionInStorage(Session());
            Assert.That(_legacy.Value, Is.Not.Null);
            var restored = new InternalAuthSessionStorage(new PlayerPrefsAuthSessionStore(_legacy), null, AppId)
                .RetrieveInternalAuthSessionFromStorage();
            Assert.That(restored.User.Id, Is.EqualTo("test-user"));
            Assert.That(_legacy.Value, Is.Not.Null);
        }

        [Test]
        public void CompatibilityBackendLogoutDeletesPersistedSession()
        {
            _legacy.AllowWrites = true;
            var storage = new InternalAuthSessionStorage(new PlayerPrefsAuthSessionStore(_legacy), null, AppId);
            storage.SaveInternalAuthSessionInStorage(Session());
            storage.ClearInternalAuthSessionInStorage();
            Assert.That(_legacy.Value, Is.Null);
            Assert.That(storage.RetrieveInternalAuthSessionFromStorage(), Is.Null);
        }

        [Test]
        public async Task LogoutClearsMemoryEvenWhenNativeDeletionFails()
        {
            var auth = new AuthDelegator(new Repository(), Storage());
            await auth.LoginWithEmailCode("test@example.com", "123456");
            _store.FailDelete = true;
            Assert.Throws<PrivyAuthenticationException>(() => auth.Logout());
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(auth.GetAuthSession(), Is.Null);
        }

        [Test]
        public async Task FailedStorageWriteCannotAuthenticate()
        {
            _store.FailWrite = true;
            var auth = new AuthDelegator(new Repository(), Storage());
            var exception = Assert.ThrowsAsync<PrivyAuthenticationException>(
                () => auth.LoginWithEmailCode("test@example.com", "123456"));
            Assert.That(exception.Error, Is.EqualTo(AuthenticationError.StorageUnavailable));
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(auth.GetAuthSession(), Is.Null);
            await Task.CompletedTask;
        }

        [Test]
        public async Task RefreshCompletingAfterLogoutCannotPersistOrRestoreAuthentication()
        {
            var repository = new Repository();
            var auth = new AuthDelegator(repository, Storage());
            await auth.LoginWithEmailCode("test@example.com", "123456");
            var refresh = auth.RefreshSession(true);
            auth.Logout();
            repository.Refresh.SetResult(Session());
            await refresh;
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(auth.GetAuthSession(), Is.Null);
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public async Task RestoreCompletingAfterLogoutCannotPersistOrRestoreAuthentication()
        {
            _store.Value = JsonConvert.SerializeObject(Session(expired: true));
            var repository = new Repository();
            var auth = new AuthDelegator(repository, Storage());
            var restore = auth.RestoreSession();
            auth.Logout();
            repository.Refresh.SetResult(Session());
            await restore;
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public async Task FailedUserUpdatePersistenceClearsAuthentication()
        {
            var repository = new Repository();
            var auth = new AuthDelegator(repository, Storage());
            await auth.LoginWithEmailCode("test@example.com", "123456");
            _store.FailWrite = true;
            repository.UpdatedUser.SetResult(Session().User);
            Assert.ThrowsAsync<PrivyAuthenticationException>(() => auth.LinkSms("+15551234567", "123456"));
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(auth.GetAuthSession(), Is.Null);
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public async Task UserUpdateCompletingAfterLogoutCannotRecreateSession()
        {
            var repository = new Repository();
            var auth = new AuthDelegator(repository, Storage());
            await auth.LoginWithEmailCode("test@example.com", "123456");
            var update = auth.LinkSms("+15551234567", "123456");
            auth.Logout();
            repository.UpdatedUser.SetResult(Session().User);
            Assert.ThrowsAsync<PrivyAuthenticationException>(async () => await update);
            Assert.That(auth.CurrentAuthState, Is.EqualTo(AuthState.Unauthenticated));
            Assert.That(_store.Value, Is.Null);
        }

        [Test]
        public async Task InitializationFailureCompletesReadinessTaskWithSameError()
        {
            _store.FailRead = true;
            var auth = new AuthDelegator(new Repository(), Storage());
            // Bypass unrelated WebView/network construction to exercise production InitializeAsync.
            var privy = (PrivyImpl)FormatterServices.GetUninitializedObject(typeof(PrivyImpl));
            typeof(PrivyImpl).GetField("_authDelegator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(privy, auth);
            typeof(PrivyImpl).GetField("_initializationCompletionSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(privy, new TaskCompletionSource<bool>());
            var waitingForState = privy.GetAuthState();
            var failure = Assert.ThrowsAsync<PrivyAuthenticationException>(() => privy.InitializeAsync());
            Assert.That(waitingForState.IsCompleted, Is.True);
            var readinessFailure = Assert.ThrowsAsync<PrivyAuthenticationException>(async () => await waitingForState);
            Assert.That(readinessFailure, Is.SameAs(failure));
            Assert.That(failure.Error, Is.EqualTo(AuthenticationError.StorageUnavailable));
            await Task.CompletedTask;
        }

        [Test]
        public void InvalidLegacyTokenIsDiscardedWithoutMigration()
        {
            var session = Session();
            session.AccessToken = "invalid";
            _legacy.Value = JsonConvert.SerializeObject(session);
            Assert.That(Storage().RetrieveInternalAuthSessionFromStorage(), Is.Null);
            Assert.That(_store.Value, Is.Null);
            Assert.That(_legacy.Value, Is.Null);
        }

        [Test]
        public void FailedLegacyCleanupRemovesNewSecureWriteAndDisablesRestore()
        {
            _legacy.FailDelete = true;
            var storage = Storage();
            var failure = Assert.Throws<PrivyAuthenticationException>(
                () => storage.SaveInternalAuthSessionInStorage(Session()));
            Assert.That(failure.Error, Is.EqualTo(AuthenticationError.StorageUnavailable));
            Assert.That(_store.Value, Is.Null);
            _legacy.FailDelete = false;
            Assert.That(storage.RetrieveInternalAuthSessionFromStorage(), Is.Null);
        }

        [Test]
        public void InvalidNewSessionRemovesPreviousSecureSession()
        {
            _store.Value = JsonConvert.SerializeObject(Session());
            var invalid = Session();
            invalid.RefreshToken = null;
            Assert.Throws<PrivyAuthenticationException>(() => Storage().SaveInternalAuthSessionInStorage(invalid));
            Assert.That(_store.Value, Is.Null);
        }

#if UNITY_ANDROID
        [Test]
        public void GeneratedAndroidProjectKeepsJniStorageAdapterOnce()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "privy-storage-test-" + Guid.NewGuid());
            Directory.CreateDirectory(projectPath);
            try
            {
                var processor = new PrivySDKPostprocessBuild();
                processor.OnPostGenerateGradleAndroidProject(projectPath);
                processor.OnPostGenerateGradleAndroidProject(projectPath);
                var rules = File.ReadAllText(Path.Combine(projectPath, "proguard-unity.txt"));
                const string keepRule = "-keep class io.privy.unity.storage.AuthSessionStore { public *; }";
                Assert.That(rules.Split(new[] { keepRule }, StringSplitOptions.None).Length, Is.EqualTo(2));
            }
            finally
            {
                Directory.Delete(projectPath, true);
            }
        }
#endif

        [Test]
        public void LegacySessionWithoutUserIdentityIsDiscarded()
        {
            var session = Session();
            session.User.Id = null;
            _legacy.Value = JsonConvert.SerializeObject(session);
            Assert.That(Storage().RetrieveInternalAuthSessionFromStorage(), Is.Null);
            Assert.That(_store.Value, Is.Null);
            Assert.That(_legacy.Value, Is.Null);
        }

        [Test]
        public void StorageNamespacesSeparateAmbiguousAppClientPairs()
        {
            Assert.That(AuthSessionStoreNamespace.Create("a", "bc"),
                Is.Not.EqualTo(AuthSessionStoreNamespace.Create("ab", "c")));
        }

        private InternalAuthSessionStorage Storage() => new InternalAuthSessionStorage(_store, _legacy, AppId);

        private static InternalAuthSession Session(string appId = AppId, bool expired = false)
        {
            var payload = JsonConvert.SerializeObject(new
            {
                aud = appId,
                sub = "test-user",
                exp = expired ? 1 : DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=')
                .Replace('+', '-').Replace('/', '_');
            return new InternalAuthSession
            {
                AccessToken = $"header.{encoded}.signature",
                RefreshToken = "test-refresh-token",
                User = new InternalPrivyUser { Id = "test-user" },
                SessionUpdateAction = SessionUpdateAction.Set
            };
        }

        private sealed class RecordingStore : IAuthSessionStore
        {
            private readonly List<string> _events;
            internal string Value;
            internal bool FailRead;
            internal bool FailWrite;
            internal bool FailDelete;
            internal bool FailReadback;
            private bool _written;
            internal RecordingStore(List<string> events) => _events = events;
            public string Read()
            {
                _events.Add("read");
                if (FailRead) throw new InvalidOperationException("native detail must not escape");
                return FailReadback && _written ? "different value" : Value;
            }
            public void Write(string value)
            {
                _events.Add("write");
                if (FailWrite) throw new InvalidOperationException("storage failure");
                Value = value;
                _written = true;
            }
            public void Delete()
            {
                _events.Add("delete");
                if (FailDelete) throw new InvalidOperationException("storage failure");
                Value = null;
            }
        }

        private sealed class LegacyStore : IPlayerPrefsDataManager
        {
            private readonly List<string> _events;
            internal string Value;
            internal bool FailDelete;
            internal LegacyStore(List<string> events) => _events = events;
            internal bool AllowWrites;
            public void SaveData<T>(string key, T value)
            {
                if (!AllowWrites) throw new InvalidOperationException("No legacy writes allowed.");
                Value = (string)(object)value;
            }
            public T LoadData<T>(string key)
            {
                _events.Add("legacy-read");
                return (T)(object)Value;
            }
            public void DeleteData(string key)
            {
                _events.Add("legacy-delete");
                if (FailDelete) throw new InvalidOperationException("legacy cleanup failed");
                Value = null;
            }
        }

        private sealed class Repository : IAuthRepository
        {
            internal readonly TaskCompletionSource<InternalAuthSession> Refresh = new TaskCompletionSource<InternalAuthSession>();
            public Task<InternalAuthSession> LoginWithEmailCode(string email, string code) => Task.FromResult(Session());
            public Task<InternalAuthSession> RefreshSession(string accessToken, string refreshToken) => Refresh.Task;
            public Task<bool> SendEmailCode(string email) => throw new NotImplementedException();
            public Task<InitiateOAuthFlowResponse> InitiateOAuthFlow(OAuthProvider provider, string codeChallenge,
                string redirectUri, string stateCode) => throw new NotImplementedException();
            public Task<InternalAuthSession> AuthenticateOAuthFlow(string authorizationCode, string codeVerifier,
                string stateCode, bool isRawFlow = false) => throw new NotImplementedException();
            public Task<bool> SendSmsCode(string phoneNumber) => throw new NotImplementedException();
            public Task<InternalAuthSession> LoginWithSmsCode(string phoneNumber, string code) => throw new NotImplementedException();
            internal readonly TaskCompletionSource<InternalPrivyUser> UpdatedUser = new TaskCompletionSource<InternalPrivyUser>();
            public Task<InternalPrivyUser> LinkSms(string phoneNumber, string code, string accessToken) => UpdatedUser.Task;
            public Task<InternalPrivyUser> UnlinkSms(string phoneNumber, string accessToken) => throw new NotImplementedException();
            public Task<InternalPrivyUser> UpdateSmsPhoneNumber(string phoneNumber, string code, string accessToken) => throw new NotImplementedException();
        }
    }
}
