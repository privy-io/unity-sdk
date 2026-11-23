using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class PrivySDKPostprocessBuild
#if UNITY_ANDROID
    : IPostGenerateGradleAndroidProject
#endif
{
#if UNITY_ANDROID
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // JNI looks this class up by name; R8 cannot infer its managed callers.
        const string keepRule = "-keep class io.privy.unity.storage.AuthSessionStore { public *; }";
        var rulesPath = Path.Combine(path, "proguard-unity.txt");
        if (!File.Exists(rulesPath) || !File.ReadAllText(rulesPath).Contains(keepRule))
            File.AppendAllText(rulesPath, "\n" + keepRule + "\n");
    }
#endif

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
#if UNITY_IOS
        if (buildTarget == BuildTarget.iOS)
        {
            var projPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));

            // Link the AuthenticationServices framework, used by OAuth flows on iOS.
            var frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTargetGuid, "AuthenticationServices.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "Security.framework", false);
            File.WriteAllText(projPath, project.WriteToString());

            // Add the 'Sign in with Apple' capability to the Player target, necessary for displaying the native flow.
            var mainTargetGuid = project.GetUnityMainTargetGuid();
            var capabilitiesManager = new ProjectCapabilityManager(projPath, "Unity-iPhone/Unity-iPhone.entitlements",
                targetGuid: mainTargetGuid);
            capabilitiesManager.AddSignInWithApple();
            capabilitiesManager.WriteToFile();
        }
#endif
    }
}
