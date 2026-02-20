#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Seon.Editor
{
    /// <summary>
    /// iOS post-build processor for SEON SDK.
    /// Always embeds the SeonSDK framework (required for runtime linking).
    /// Optionally adds CoreLocation and Info.plist entries via SeonBuildConfig.
    /// </summary>
    public static class SeonPostBuildProcessor
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target != BuildTarget.iOS) return;

            var pbxPath = PBXProject.GetPBXProjectPath(pathToProject);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(pbxPath));

            var mainTargetGuid = project.GetUnityMainTargetGuid();
            var frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            // ── ALWAYS: Embed SeonSDK framework ──────────────────────
            // Without this, the app crashes at launch with a dyld error:
            // "Library not loaded: SeonSDK.framework/SeonSDK"
            EmbedSeonFramework(project, mainTargetGuid, pathToProject);

            // ── OPTIONAL: Config-driven Xcode modifications ──────────
            var config = SeonBuildConfig.Load();
            if (config != null && config.EnablePostBuildProcessor)
            {
                // CoreLocation.framework (for geolocation)
                if (config.AddCoreLocationFramework)
                {
                    project.AddFrameworkToProject(frameworkTargetGuid, "CoreLocation.framework", false);
                    Debug.Log("[SeonSDK] Added CoreLocation.framework");
                }

                // Info.plist modifications
                if (config.AddLocationUsageDescription)
                {
                    AddPlistEntry(pathToProject, config.LocationUsageDescription);
                }
            }

            project.WriteToFile(pbxPath);
        }

        /// <summary>
        /// Find the SeonSDK.framework in the Pods directory and ensure it's
        /// embedded (copied into the app bundle) with code signing.
        /// </summary>
        private static void EmbedSeonFramework(PBXProject project, string mainTargetGuid, string pathToProject)
        {
            // Search for the SeonSDK framework in the built Xcode project
            string frameworkPath = FindSeonFramework(pathToProject);
            if (string.IsNullOrEmpty(frameworkPath))
            {
                Debug.LogWarning(
                    "[SeonSDK] Could not find SeonSDK.framework in the Xcode project. " +
                    "Ensure CocoaPods ran successfully. You may need to manually embed " +
                    "the framework: Target → General → Frameworks → Embed & Sign.");
                return;
            }

            // Make path relative to the Xcode project
            string relativePath = frameworkPath;
            if (frameworkPath.StartsWith(pathToProject))
            {
                relativePath = frameworkPath.Substring(pathToProject.Length).TrimStart('/');
            }

            // Add the framework file to the project if not already present
            string fileGuid = project.FindFileGuidByProjectPath(relativePath);
            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = project.AddFile(relativePath, relativePath);
            }

            // Add to the Embed Frameworks build phase (this copies it into .app/Frameworks/)
            // PBXProjectExtensions is in UnityEditor.iOS.Xcode.Extensions
            UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions.AddFileToEmbedFrameworks(
                project, mainTargetGuid, fileGuid);

            Debug.Log($"[SeonSDK] Embedded SeonSDK framework: {relativePath}");
        }

        /// <summary>
        /// Recursively search for SeonSDK.framework in the Pods directory.
        /// </summary>
        private static string FindSeonFramework(string projectPath)
        {
            // Common locations for CocoaPods frameworks
            string[] searchPaths = new[]
            {
                Path.Combine(projectPath, "Pods", "SeonSDK", "Frameworks", "SeonSDK.framework"),
                Path.Combine(projectPath, "Pods", "SeonSDK", "SeonSDK.framework"),
                Path.Combine(projectPath, "Frameworks", "SeonSDK.framework"),
            };

            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            // Fallback: recursive search under Pods
            string podsDir = Path.Combine(projectPath, "Pods");
            if (Directory.Exists(podsDir))
            {
                foreach (var dir in Directory.GetDirectories(podsDir, "SeonSDK.framework", SearchOption.AllDirectories))
                {
                    return dir;
                }
            }

            return null;
        }

        private static void AddPlistEntry(string pathToProject, string locationDescription)
        {
            var plistPath = Path.Combine(pathToProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            if (!plist.root.values.ContainsKey("NSLocationWhenInUseUsageDescription"))
            {
                plist.root.SetString("NSLocationWhenInUseUsageDescription", locationDescription);
                Debug.Log("[SeonSDK] Added NSLocationWhenInUseUsageDescription to Info.plist");
            }

            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif
