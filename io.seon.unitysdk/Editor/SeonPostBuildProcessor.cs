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
    /// Always embeds the SeonSDK xcframework (required for runtime linking).
    /// Optionally adds CoreLocation and Info.plist entries via SeonBuildConfig.
    /// </summary>
    public static class SeonPostBuildProcessor
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target != BuildTarget.iOS){ 
                return; 
            }

            var pbxPath = PBXProject.GetPBXProjectPath(pathToProject);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(pbxPath));

            var mainTargetGuid = project.GetUnityMainTargetGuid();
            var frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            // ── ALWAYS: Embed SeonSDK xcframework 
            // Without this, the app crashes at launch with a dyld error:
            // "Library not loaded: SeonSDK.framework/SeonSDK"
            EmbedSeonFramework(project, mainTargetGuid, pathToProject);

            // ── OPTIONAL: Config-driven Xcode modifications 
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
        /// Find the SeonSDK.xcframework in the Pods directory and ensure it's
        /// embedded (copied into the app bundle) with code signing.
        /// </summary>
        private static void EmbedSeonFramework(PBXProject project, string mainTargetGuid, string pathToProject)
        {
            // Search for the SeonSDK framework in the built Xcode project
            string frameworkPath = FindSeonFramework(pathToProject);
            if (string.IsNullOrEmpty(frameworkPath))
            {
                throw new UnityEditor.Build.BuildFailedException(
                    "[SeonSDK] Could not find SeonSDK.xcframework in the Xcode project. " +
                    "The app will crash at launch without it.\n\n" +
                    "Remediation steps:\n" +
                    "1. Ensure CocoaPods ran successfully (check for Pods/ directory in the Xcode project).\n" +
                    "2. If using EDM4U, try Assets → External Dependency Manager → iOS Resolver → Install Cocoapods.\n" +
                    "3. As a last resort, manually embed the framework: Target → General → Frameworks → Embed & Sign.");
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

            Debug.Log("[SeonSDK] Embedded SeonSDK xcframework: " + relativePath);
        }

        /// <summary>
        /// Recursively search for SeonSDK.xcframework in the Pods directory.
        /// </summary>
        private static string FindSeonFramework(string projectPath)
        {
            // Common locations for CocoaPods frameworks
            string[] searchPaths = new[]
            {
                Path.Combine(projectPath, "Pods", "SeonSDK", "Frameworks", "SeonSDK.xcframework"),
                Path.Combine(projectPath, "Pods", "SeonSDK", "SeonSDK.xcframework"),
                Path.Combine(projectPath, "Frameworks", "SeonSDK.xcframework"),
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
                foreach (var dir in Directory.GetDirectories(podsDir, "SeonSDK.xcframework", SearchOption.AllDirectories))
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
