using System;
using System.Security.Cryptography;
using System.Text;

namespace Privy.Internal.Storage
{
    internal static class AuthSessionStoreNamespace
    {
        internal static string Create(string appId, string clientId)
        {
            if (string.IsNullOrEmpty(appId)) throw new ArgumentException("App ID is required.", nameof(appId));
            // Length prefixes keep different app/client pairs distinct.
            var value = $"v1:{appId.Length}:{appId}:{clientId?.Length ?? 0}:{clientId}";
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(value)))
                .Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
