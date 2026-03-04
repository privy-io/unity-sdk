using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple ScriptableObject that contains environment configuration key/value pairs.
/// Useful for platforms (like WebGL) where the .env file cannot be read at runtime.
/// Create an asset via <c>Assets &gt; Create &gt; Privy &gt; EnvConfig</c>.
/// </summary>
[CreateAssetMenu(fileName = "EnvConfig", menuName = "Privy/EnvConfig", order = 100)]
public class EnvConfig : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// List of key/value pairs stored in this config asset.
    /// </summary>
    public List<Entry> Entries = new List<Entry>();

    /// <summary>
    /// Return the value for the given key, or null if not found.
    /// </summary>
    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        for (int i = 0; i < Entries.Count; i++)
        {
            if (string.Equals(Entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
                return Entries[i].Value;
        }
        return null;
    }
}
