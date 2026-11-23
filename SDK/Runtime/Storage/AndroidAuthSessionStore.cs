#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Privy.Internal.Storage
{
    internal sealed class AndroidAuthSessionStore : IAuthSessionStore
    {
        private readonly string _account;

        internal AndroidAuthSessionStore(string account)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public string Read()
        {
            using var store = CreateStore();
            return store.Call<string>("read");
        }

        public void Write(string value)
        {
            using var store = CreateStore();
            store.Call("write", value);
        }

        public void Delete()
        {
            using var store = CreateStore();
            store.Call("delete");
        }

        private AndroidJavaObject CreateStore()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return new AndroidJavaObject("io.privy.unity.storage.AuthSessionStore", activity, _account);
        }
    }
}
#endif
