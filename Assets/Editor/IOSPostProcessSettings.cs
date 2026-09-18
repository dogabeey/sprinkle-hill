// IMPORTANT NOTE FOR DEVELOPER (ME): Do not actually use this script when going live with your app. 
// This script is only for testing purposes and will remove the APNs entitlement from your app, 
// which will prevent push notifications from working. You should remove this script before 
// submitting your app to the App Store.
// Instead, modify the certificate and provisioning profile used for your app to not include the APNs entitlement.

#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class IOSPostProcess
{
    [PostProcessBuild(9999)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS)
            return;

        string projectPath = PBXProject.GetPBXProjectPath(path);

        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        string mainTarget = project.GetUnityMainTargetGuid();

        // Find the entitlements file used by the main target.
        string entitlementsRelativePath =
            project.GetEntitlementFilePathForTarget(mainTarget);

        if (!string.IsNullOrEmpty(entitlementsRelativePath))
        {
            string entitlementsPath =
                Path.Combine(path, entitlementsRelativePath);

            if (File.Exists(entitlementsPath))
            {
                var entitlements = new PlistDocument();
                entitlements.ReadFromFile(entitlementsPath);

                // Remove APNs entitlement.
                entitlements.root.values.Remove("aps-environment");

                entitlements.WriteToFile(entitlementsPath);
            }
        }

        project.WriteToFile(projectPath);
    }
}
#endif