#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Privy.Internal.Storage
{
    internal sealed class IOSAuthSessionStore : IAuthSessionStore
    {
        private readonly string _account;

        internal IOSAuthSessionStore(string account)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public string Read()
        {
            var status = PrivyAuthSessionRead(_account, out var value);
            try
            {
                if (status == -25300) return null; // errSecItemNotFound
                if (status == -26275) // errSecDecode
                {
                    Delete();
                    return null;
                }
                Check(status);
                return Marshal.PtrToStringUTF8(value);
            }
            finally
            {
                if (value != IntPtr.Zero) PrivyAuthSessionFree(value);
            }
        }

        public void Write(string value) => Check(PrivyAuthSessionWrite(_account, value));

        public void Delete()
        {
            var status = PrivyAuthSessionDelete(_account);
            if (status != -25300) Check(status);
        }

        private static void Check(int status)
        {
            if (status != 0) throw new InvalidOperationException("Native session storage is unavailable.");
        }

        [DllImport("__Internal")]
        private static extern int PrivyAuthSessionRead([MarshalAs(UnmanagedType.LPUTF8Str)] string account, out IntPtr value);

        [DllImport("__Internal")]
        private static extern int PrivyAuthSessionWrite([MarshalAs(UnmanagedType.LPUTF8Str)] string account,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport("__Internal")]
        private static extern int PrivyAuthSessionDelete([MarshalAs(UnmanagedType.LPUTF8Str)] string account);

        [DllImport("__Internal")]
        private static extern void PrivyAuthSessionFree(IntPtr value);
    }
}
#endif
