using Privy.Utils;

namespace Privy.Config
{
    public class PrivyConfig
    {
        public string AppId { get; set; }
        public string ClientId { get; set; }
        public PrivyLogLevel LogLevel { get; set; } = PrivyLogLevel.None;
    }
}
