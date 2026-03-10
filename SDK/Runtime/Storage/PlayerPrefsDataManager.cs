using UnityEngine;
using Newtonsoft.Json;

namespace Privy.Internal.Storage
{
    internal class PlayerPrefsDataManager : IPlayerPrefsDataManager
    {
        // Save data of any type
        public void SaveData<T>(string key, T data)
        {
            if (data is string stringValue)
            {
                PlayerPrefs.SetString(key, stringValue);
            }
            else
            {
                string json = JsonConvert.SerializeObject(data);
                PlayerPrefs.SetString(key, json);
            }

            PlayerPrefs.Save();
        }

        // Load data of any type
        public T LoadData<T>(string key)
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            // special-case string because DeserializeObject<string> will strip quotes
            if (typeof(T) == typeof(string))
            {
                // cast via object to satisfy generic return type
                return (T)(object)json;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public void DeleteData(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}
