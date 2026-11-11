#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarbleOdyssey.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetupWizard
    {
        static ProjectSetupWizard()
        {
            if (SessionState.GetBool("MO_SetupChecked", false)) return;
            SessionState.SetBool("MO_SetupChecked", true);
            EditorApplication.delayCall += CheckProjectSetup;
        }

        private static void CheckProjectSetup()
        {
            bool needsSetup = false;
            string issues = "";

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                needsSetup = true;
                issues += "- No URP Render Pipeline Asset assigned in Graphics Settings\n";
                issues += "  Create via: Assets > Create > Rendering > URP Asset (with Universal Renderer)\n";
                issues += "  Then assign in: Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings\n\n";
            }

            if (QualitySettings.renderPipeline == null && GraphicsSettings.defaultRenderPipeline == null)
            {
                issues += "- Quality levels need URP pipeline assets assigned\n";
                issues += "  Set in: Edit > Project Settings > Quality > Rendering (for each quality level)\n\n";
            }

            string[] requiredTags = { "Marble", "Obstacle", "Collectible", "Checkpoint", "FinishZone", "DeathZone", "SpeedBoost", "JumpPad", "MovingPlatform", "Laser" };
            foreach (var tag in requiredTags)
            {
                try
                {
                    GameObject.FindGameObjectsWithTag(tag);
                }
                catch
                {
                    needsSetup = true;
                    issues += $"- Tag '{tag}' not found. Add it in: Edit > Project Settings > Tags and Layers\n";
                }
            }

            if (needsSetup)
            {
                Debug.LogWarning(
                    "[Marble Odyssey] Project setup required:\n\n" + issues +
                    "\nUse menu: Marble Odyssey > Scene Setup to create scene hierarchies.");
            }
            else
            {
                Debug.Log("[Marble Odyssey] Project setup looks good!");
            }
        }

        [MenuItem("Marble Odyssey/Verify Project Setup")]
        public static void ManualCheck()
        {
            SessionState.SetBool("MO_SetupChecked", false);
            CheckProjectSetup();
        }

        [MenuItem("Marble Odyssey/Setup/Apply Android Build Settings")]
        public static void ApplyAndroidBuildSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingBackend.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Application.targetFrameRate = 60;

            Debug.Log("[Marble Odyssey] Android build settings applied. " +
                      "Remember to set up URP pipeline assets for each quality level.");
        }

        [MenuItem("Marble Odyssey/Setup/Apply Quality Settings")]
        public static void ApplyQualitySettings()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 70f;

            Debug.Log("[Marble Odyssey] Quality settings applied for Medium tier. " +
                      "Create Low/Medium/High URP assets and assign them per quality level.");
        }
    }
}
#endif
