// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// ProductionBuildValidator.cs — Pre-build validation for production releases
// ============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MarbleOdyssey.Advertising.Editor
{
    /// <summary>
    /// Pre-build processor that validates ad configuration before production builds.
    /// Prevents accidental release with test IDs or missing configuration.
    /// </summary>
    public class ProductionBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Only validate for Android release builds
            if (report.summary.platform != BuildTarget.Android)
                return;

            Debug.Log("[BuildValidator] Running production ad configuration checks...");

            // Find the AdConfiguration asset
            var guids = AssetDatabase.FindAssets("t:AdConfiguration");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[BuildValidator] No AdConfiguration asset found. " +
                                 "Create one via Assets > Create > Marble Odyssey > Ad Configuration.");
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<AdConfiguration>(path);

                if (config == null) continue;

                ValidateConfig(config, report);
            }
        }

        private void ValidateConfig(AdConfiguration config, BuildReport report)
        {
            bool isReleaseBuild = !EditorUserBuildSettings.development;

            if (isReleaseBuild && config.testMode)
            {
                // WARNING: Building a release with test mode on
                Debug.LogWarning(
                    "[BuildValidator] ⚠ RELEASE BUILD with Test Mode ON!\n" +
                    "The build will use test ad IDs and will not generate revenue.\n" +
                    "Set testMode to false in AdConfiguration before release.");

                // Show dialog to developer
                if (!EditorUtility.DisplayDialog(
                    "AdMob Test Mode Warning",
                    "You are building a RELEASE build with Test Mode enabled!\n\n" +
                    "Test ads will be shown and no revenue will be generated.\n\n" +
                    "Do you want to continue anyway?",
                    "Continue (Test)", "Cancel Build"))
                {
                    throw new BuildFailedException(
                        "Build cancelled: Test mode is enabled for release build.");
                }
            }

            if (!config.testMode && !config.ValidateProductionIds())
            {
                string msg = "Production build has placeholder Ad Unit IDs!\n" +
                             "Replace all placeholder IDs in AdConfiguration before release.";

                Debug.LogError($"[BuildValidator] {msg}");

                if (!EditorUtility.DisplayDialog(
                    "AdMob Configuration Error",
                    msg + "\n\nDo you want to continue anyway?",
                    "Continue", "Cancel Build"))
                {
                    throw new BuildFailedException(
                        "Build cancelled: Placeholder ad unit IDs detected.");
                }
            }

            // Check Android manifest App ID
            ValidateAndroidManifest(config);

            Debug.Log("[BuildValidator] Ad configuration validation complete.");
        }

        private void ValidateAndroidManifest(AdConfiguration config)
        {
            // Check if the GoogleMobileAds plugin has set up the manifest correctly
            // The plugin normally handles this via its settings window
            string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

            if (!System.IO.File.Exists(manifestPath))
            {
                Debug.Log("[BuildValidator] No custom AndroidManifest.xml found. " +
                          "The Google Mobile Ads plugin should configure the App ID automatically " +
                          "via Assets > Google Mobile Ads > Settings.");
            }
        }

        /// <summary>
        /// Menu item for manual validation.
        /// </summary>
        [MenuItem("Marble Odyssey/Validate Ad Configuration")]
        private static void ValidateFromMenu()
        {
            Debug.Log("═══ Manual Ad Configuration Validation ═══");

            var guids = AssetDatabase.FindAssets("t:AdConfiguration");
            if (guids.Length == 0)
            {
                Debug.LogError("No AdConfiguration asset found!");
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<AdConfiguration>(path);
                if (config != null)
                {
                    Debug.Log($"Validating: {path}");
                    bool valid = config.ValidateProductionIds();
                    Debug.Log($"Test Mode: {config.testMode}");
                    Debug.Log($"Valid: {valid}");
                }
            }

            Debug.Log("═══════════════════════════════════════════");
        }
    }
}
#endif
