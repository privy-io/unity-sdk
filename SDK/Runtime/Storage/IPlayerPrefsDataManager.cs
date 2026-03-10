namespace Privy.Internal.Storage
{
    internal interface IPlayerPrefsDataManager
    {
        //Generic Types here
        void SaveData<T>(string key, T data);
        T LoadData<T>(string key);
        void DeleteData(string key);
    }
}
