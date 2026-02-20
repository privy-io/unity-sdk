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
        // Paths are relative to SampleApp/
        var sdkPath = "Assets/Plugins/Privy"; // The file /SampleApp/AssetsPlugins/Privy is a symlink to <root>/SDK/
        var webGLTemplatePath = "Assets/WebGLTemplates";

        File.Delete("Assets/WebGLTemplates/unity-webview-2020/unity_callback.html");
        File.Delete("Assets/WebGLTemplates/unity-webview-2020/unity_callback.meta");

        // Refresh the Asset Database to ensure everything is up-to-date
        AssetDatabase.Refresh();

        // Read version from SdkVersionManager
        var version = ReadVersionNumber();
        if (string.IsNullOrEmpty(version))
        {
            Debug.LogWarning("Export terminated due to invalid version");
            return;
        }

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

    private static string ReadVersionNumber()
    {
        var version = Privy.SdkVersion.VersionNumber;

        if (!SemverRegex.IsMatch(version))
        {
            Debug.LogError("Invalid version format in SdkVersionManager.SDKVersion!");
            return null;
        }

        return version;
    }
}