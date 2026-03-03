using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EnvFileReader
{
    private static Dictionary<string, string> _variables;


    /// <summary>
    /// Optional ScriptableObject that provides configuration values.  The
    /// reader will consult this object before falling back to the .env file.
    /// Assign this reference either in a bootstrap MonoBehaviour or via an
    /// editor script (see README comments).
    /// </summary>
    public static EnvConfig Config { get; set; }

    /// <summary>
    /// Return a configuration value by key.  Checks the following sources in order:
    /// 1. Assigned <see cref="Config"/> ScriptableObject (useful for WebGL builds)
    /// 2. Loaded .env file on disk (desktop/mobile builds)
    /// </summary>
    public static string Get(string key)
    {
        // consult the ScriptableObject first

        // next, consult the ScriptableObject if one has been assigned
        if (Config != null)
        {
            var cfgVal = Config.Get(key);
            //logging here is helpful to ensure values are being read correctly from the ScriptableObject, which is required for WebGL builds
            if (cfgVal == null)
            {
                Debug.LogWarning($"EnvFileReader: Key '{key}' not found in EnvConfig ScriptableObject.");
            }
            else
            {
                Debug.Log($"EnvFileReader: Key '{key}' read from EnvConfig ScriptableObject with value '{cfgVal}'.");
            }
            if (cfgVal != null)
                return cfgVal;
        }

        if (_variables == null)
        {
            Load();
        }

        if (_variables != null && _variables.TryGetValue(key, out var value))
        {
            return value;
        }

#if UNITY_WEBGL
        // WebGL builds cannot read from the local filesystem; the value should come
        // from the assigned EnvConfig asset.  Log an error so the developer notices.
        Debug.LogError($"EnvFileReader: Key '{key}' not found. WebGL builds cannot access the .env file, " +
                       "or no EnvConfig has been assigned.");
#else
        Debug.LogError($"EnvFileReader: Key '{key}' not found in .env file. " +
                       "Make sure you have a .env file in the project root (copy from .env.example).");
#endif
        return null;
    }

    private static void Load()
    {
        _variables = new Dictionary<string, string>();
        var envPath = Path.Combine(Application.dataPath, "..", "..", ".env");

        if (!File.Exists(envPath))
        {
            Debug.LogError(
                "EnvFileReader: .env file not found at " + envPath + ". " +
                "Copy .env.example to .env and fill in your credentials.");
            return;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var key = trimmed.Substring(0, separatorIndex).Trim();
            var val = trimmed.Substring(separatorIndex + 1).Trim();
            _variables[key] = val;
        }
    }
}
