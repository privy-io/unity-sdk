namespace Privy.Analytics
{
    public interface IClientAnalyticsIdRepository
    {
        public string LoadClientId();

        void ResetClientId();
    }
}
