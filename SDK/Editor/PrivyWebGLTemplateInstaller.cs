using System.IO;
using UnityEditor;
using UnityEngine;
public class PrivyWebGLTemplateInstaller
{
    [MenuItem("Tools/Privy/Install WebGL Templates")]
    public static void InstallTemplates()
    {
        // Try multiple candidate locations: local SDK folder or package path
        string[] candidates = new[] {
            Path.Combine(Application.dataPath, "../SDK/WebGLTemplates~"),
            Path.Combine(Application.dataPath, "../Packages/com.privy.unity-sdk/WebGLTemplates~")
        };

        string sourceRoot = null;
        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
            {
                sourceRoot = c;
                break;
            }
        }

        if (sourceRoot == null)
        {
            Debug.LogError("Could not find WebGL templates source directory. Have you installed the SDK package?");
            return;
        }

        var destRoot = Path.Combine(Application.dataPath, "WebGLTemplates");
        if (!Directory.Exists(destRoot))
            Directory.CreateDirectory(destRoot);

        CopyDirectory(sourceRoot, destRoot);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Privy SDK", "WebGL templates installed to Assets/WebGLTemplates.\n\nPlease review the documentation for further instructions.", "OK");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
        }
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var destFile = file.Replace(sourceDir, destDir);
            File.Copy(file, destFile, true);
        }
    }
}
