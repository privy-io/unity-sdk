using Privy.Utils;
using Privy.Internal.Storage;
using Privy.Auth.Models;

namespace Privy.Auth
{
    internal class InternalAuthSessionStorage
    {
        private readonly IPlayerPrefsDataManager _playerPrefsDataManager;

        internal InternalAuthSessionStorage(IPlayerPrefsDataManager playerPrefsDataManager)
        {
            _playerPrefsDataManager = playerPrefsDataManager;
        }

        internal InternalAuthSession RetrieveInternalAuthSessionFromStorage()
        {
            // generic load will already deserialize or return default
            return _playerPrefsDataManager.LoadData<InternalAuthSession>(
                Constants.INTERNAL_AUTH_SESSION_KEY);
        }

        internal void SaveInternalAuthSessionInStorage(InternalAuthSession internalAuthSession)
        {
            _playerPrefsDataManager.SaveData(Constants.INTERNAL_AUTH_SESSION_KEY, internalAuthSession);
        }

        internal void ClearInternalAuthSessionInStorage()
        {
            _playerPrefsDataManager.DeleteData(Constants.INTERNAL_AUTH_SESSION_KEY);
        }
    }
}
