using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Seon.Editor {
    /// <summary>
    /// ScriptableObject configuration for SEON SDK build settings.
    /// Auto-created on first use. Accessible via Window → SEON → Build Config.
    /// </summary>
    public class SeonBuildConfig : ScriptableObject {
        private const string DefaultAssetPath = "Assets/SeonBuildConfig.asset";

        [Header("Post-Build Processor")]
        [Tooltip("Enable geolocation-related Xcode modifications (CoreLocation, Info.plist). Framework embedding is always applied.")]
        public bool EnablePostBuildProcessor = true;

        [Header("iOS — Geolocation")]
        [Tooltip("Add CoreLocation.framework to the Xcode project. Required for geolocation features.")]
        public bool AddCoreLocationFramework = false;

        [Tooltip("Add NSLocationWhenInUseUsageDescription to Info.plist. Required for geolocation permission prompt.")]
        public bool AddLocationUsageDescription = false;

        [Tooltip("The location usage description shown in the iOS permission dialog.")]
        public string LocationUsageDescription = "This app uses your location for fraud prevention.";

        /// <summary>
        /// Load the SeonBuildConfig asset, auto-creating one if it doesn't exist.
        /// </summary>
        public static SeonBuildConfig Load() {
            // Look for config in Resources first
            var config = Resources.Load<SeonBuildConfig>("SeonBuildConfig");
            if (config != null) {
                return config;
            }

#if UNITY_EDITOR
            // Search AssetDatabase
            var guids = AssetDatabase.FindAssets("t:SeonBuildConfig");
            if (guids.Length > 1) {
                Debug.LogWarning(
                    $"[SeonSDK] Found {guids.Length} SeonBuildConfig assets. " +
                    $"Using: {AssetDatabase.GUIDToAssetPath(guids[0])}. " +
                    "Delete duplicates to avoid confusion.");
            }
            if (guids.Length > 0) {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<SeonBuildConfig>(path);
            }

            // Auto-create a default config
            config = CreateInstance<SeonBuildConfig>();
            AssetDatabase.CreateAsset(config, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SeonSDK] Created default build config at {DefaultAssetPath}");
            return config;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        // Menu Items 
        [MenuItem("Window/SEON/Build Config", priority = 1)]
        private static void SelectBuildConfig() {
            var config = Load();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Window/SEON/Documentation", priority = 100)]
        private static void OpenDocs() {
            Application.OpenURL("https://github.com/seontechnologies/seon-unity-sdk-plugin#readme");
        }
#endif
    }
}
