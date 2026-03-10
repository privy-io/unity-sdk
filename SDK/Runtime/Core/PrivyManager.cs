using System;
using System.Threading.Tasks;
using Privy.Config;

namespace Privy.Core
{
    public class PrivyManager
    {
        private static PrivyImpl _privyInstance;

        // Property to access the Privy instance
        public static IPrivy Instance
        {
            get
            {
                if (_privyInstance == null)
                {
                    throw new InvalidOperationException("Call PrivyManager.Initialize before attempting to get the Privy instance.");
                }

                return _privyInstance;
            }
        }

        // Synchronous initialization.  Returns the SDK instance immediately and
        // begins background setup.  Consumers do **not** need to await this call.
        public static IPrivy Initialize(PrivyConfig config)
        {
            if (_privyInstance == null)
            {
                _privyInstance = new PrivyImpl(config);
                // fire-and-forget initialization; any calls to GetAuthState/GetUser will
                // await internally until initialization completes.
                _ = _privyInstance.InitializeAsync();
            }

            return _privyInstance; // return immediately
        }
    }
}
