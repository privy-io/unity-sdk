using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EnvFileReader
{
    private static Dictionary<string, string> _variables;

    public static string Get(string key)
    {
        if (_variables == null)
        {
            Load();
        }

        if (_variables.TryGetValue(key, out var value))
        {
            return value;
        }

        Debug.LogError($"EnvFileReader: Key '{key}' not found in .env file. " +
                       "Make sure you have a .env file in the project root (copy from .env.example).");
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
