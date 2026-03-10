using Privy.Utils;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class VersionedExport
{
    private static readonly Regex SemverRegex = new Regex(@"\d+\.\d+\.\d+");

    [MenuItem("Tools/Export Privy SDK")]
    public static void MoveAndExportVersioned()
    {
        // Paths are relative to the project root
        var sdkPath = "Packages/com.privy.unity-sdk";
        var webGLTemplatePath = "Assets/WebGLTemplates";

        // ensure WebGL callback files aren't included
        File.Delete("Assets/WebGLTemplates/unity-webview-2020/unity_callback.html");
        File.Delete("Assets/WebGLTemplates/unity-webview-2020/unity_callback.meta");

        AssetDatabase.Refresh();

        // Read version from SdkVersion
        var version = ReadVersionNumber();
        if (string.IsNullOrEmpty(version))
        {
            Debug.LogWarning("Export terminated due to invalid version");
            return;
        }
        // bump version in package.json to match
        UpdatePackageJsonVersion(version);

        // Show save file dialog to select where to save the package
        var savePath = PromptSavePath(version);
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogWarning("Export cancelled by user.");
            return;
        }

        string[] assetPaths = { sdkPath, webGLTemplatePath };

        // Export the SDK and WebGL templates as a versioned .unitypackage to the selected path
        AssetDatabase.ExportPackage(assetPaths, savePath, ExportPackageOptions.Recurse);
        Debug.Log($"Package exported successfully to {savePath}.");

        // Refresh the Asset Database to ensure everything is up-to-date
        AssetDatabase.Refresh();
    }

    private static string PromptSavePath(string version)
    {
        var defaultName = $"PrivySDK_v{version}.unitypackage";
        return EditorUtility.SaveFilePanel("Save Unity Package", "", defaultName, "unitypackage");
    }

    private static void UpdatePackageJsonVersion(string version)
    {
        var pkgPath = Path.Combine(Application.dataPath, "../SDK/package.json");
        if (File.Exists(pkgPath))
        {
            var text = File.ReadAllText(pkgPath);
            // using verbatim string literal to avoid escape issues with backslashes
            // update version field using a normal string literal with proper escaping
            var updated = Regex.Replace(text, "\"version\"\\s*:\\s*\"[^\"]+\"", $"\"version\": \"{version}\"");
            File.WriteAllText(pkgPath, updated);
        }
    }

    private static string ReadVersionNumber()
    {
        var version = SdkVersion.VersionNumber;

        if (!SemverRegex.IsMatch(version))
        {
            Debug.LogError("Invalid version format in SdkVersionManager.SDKVersion!");
            return null;
        }

        return version;
    }
}
