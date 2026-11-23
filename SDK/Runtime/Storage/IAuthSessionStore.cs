namespace Privy.Internal.Storage
{
    internal interface IAuthSessionStore
    {
        string Read();
        void Write(string value);
        void Delete();
    }
}
