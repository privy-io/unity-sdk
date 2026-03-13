namespace Privy.Analytics
{
    internal interface IClientAnalyticsIdRepository
    {
        public string LoadClientId();

        void ResetClientId();
    }
}
