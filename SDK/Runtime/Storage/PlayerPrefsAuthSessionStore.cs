using System;
using Privy.Utils;
using UnityEngine;

namespace Privy.Internal.Storage
{
    // Compatibility backend for platforms without iOS Keychain / Android Keystore.
    internal sealed class PlayerPrefsAuthSessionStore : IAuthSessionStore
    {
        private readonly IPlayerPrefsDataManager _preferences;

        internal PlayerPrefsAuthSessionStore(IPlayerPrefsDataManager preferences)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }

        public string Read() => _preferences.LoadData<string>(Constants.INTERNAL_AUTH_SESSION_KEY);

        public void Write(string value) => _preferences.SaveData(Constants.INTERNAL_AUTH_SESSION_KEY, value);

        public void Delete()
        {
            _preferences.DeleteData(Constants.INTERNAL_AUTH_SESSION_KEY);
            PlayerPrefs.Save();
        }
    }
}
