// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdConfigurationEditor.cs — Custom Inspector for AdConfiguration
// ============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MarbleOdyssey.Advertising.Editor
{
    [CustomEditor(typeof(AdConfiguration))]
    public class AdConfigurationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (AdConfiguration)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Marble Odyssey 3D — Ad Configuration",
                EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Warnings
            if (!config.testMode)
            {
                if (!config.ValidateProductionIds())
                {
                    EditorGUILayout.HelpBox(
                        "PRODUCTION MODE: Some Ad Unit IDs are still placeholders! " +
                        "Replace them with real IDs from your AdMob dashboard before building.",
                        MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "PRODUCTION MODE active. Ensure all IDs are correct before release.",
                        MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "TEST MODE: Using official Google test ad unit IDs. " +
                    "Switch to production mode and set real IDs before release.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            // Validation button
            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfig(config);
            }

            // Quick toggle
            EditorGUILayout.Space();
            if (config.testMode)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("⚠ Switch to PRODUCTION Mode"))
                {
                    Undo.RecordObject(config, "Switch to Production");
                    config.testMode = false;
                    EditorUtility.SetDirty(config);
                }
            }
            else
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Switch to TEST Mode"))
                {
                    Undo.RecordObject(config, "Switch to Test Mode");
                    config.testMode = true;
                    EditorUtility.SetDirty(config);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void ValidateConfig(AdConfiguration config)
        {
            bool allValid = true;

            Debug.Log("═══ Ad Configuration Validation ═══");

            // App ID
            if (string.IsNullOrEmpty(config.androidAppId) ||
                config.androidAppId.Contains("XXXXX"))
            {
                if (!config.testMode)
                {
                    Debug.LogError("Android App ID is a placeholder!");
                    allValid = false;
                }
                else
                {
                    Debug.Log("Android App ID: placeholder (OK in test mode)");
                }
            }
            else
            {
                Debug.Log($"Android App ID: {config.androidAppId}");
            }

            // Rewarded
            if (config.enableRewardedAds)
            {
                ValidateAdUnit("Rewarded", config.rewardedAdUnitId, config.testMode,
                    ref allValid);
            }

            // Interstitial
            if (config.enableInterstitials)
            {
                ValidateAdUnit("Interstitial", config.interstitialAdUnitId,
                    config.testMode, ref allValid);
            }

            // Banner
            if (config.enableBannerAds)
            {
                ValidateAdUnit("Banner", config.bannerAdUnitId, config.testMode,
                    ref allValid);
            }

            // Frequency caps
            Debug.Log($"Interstitial grace levels: {config.interstitialGraceLevels}");
            Debug.Log($"Min interval: {config.interstitialMinIntervalSeconds}s");
            Debug.Log($"Level interval: {config.interstitialLevelInterval}");
            Debug.Log($"Max per 30min: {config.interstitialMaxPer30Min}");

            if (allValid)
            {
                Debug.Log("✓ All validations passed!");
            }
            else
            {
                Debug.LogError("✗ Configuration has errors. Fix them before release.");
            }

            Debug.Log("═══════════════════════════════════");
        }

        private void ValidateAdUnit(string name, string id, bool testMode,
            ref bool allValid)
        {
            if (string.IsNullOrEmpty(id) || id.Contains("XXXXX"))
            {
                if (!testMode)
                {
                    Debug.LogError($"{name} ad unit ID is a placeholder!");
                    allValid = false;
                }
                else
                {
                    Debug.Log($"{name}: placeholder (OK in test mode)");
                }
            }
            else
            {
                Debug.Log($"{name}: {id}");
            }
        }
    }
}
#endif
