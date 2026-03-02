namespace Privy
{
    public class PrivyConfig
    {
        public string AppId;
        public string ClientId;
        public PrivyLogLevel LogLevel = PrivyLogLevel.NONE;

        /// <summary>
        /// When true the SDK will always use the server‑side wallet creation path
        /// (i.e. the wallet API) instead of attempting to open an iframe/webview.
        /// Useful for WebGL builds or any environment where an embedded iframe
        /// cannot be displayed.
        /// </summary>
        public bool ForceServerWallets = false;
    }
}
