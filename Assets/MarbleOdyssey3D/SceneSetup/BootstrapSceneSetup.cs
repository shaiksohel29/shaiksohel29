// ============================================================================
// Marble Odyssey 3D — BootstrapSceneSetup.cs
// Editor utility to create the initial scene hierarchy for each scene type
// ============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarbleOdyssey.SceneSetup
{
    /// <summary>
    /// Editor menu items that stamp out correctly-configured scene hierarchies.
    /// Each scene type (Boot, MainMenu, Gameplay, WorldSelect) gets its own builder
    /// so artists and designers always start from the right structure.
    /// </summary>
    public static class BootstrapSceneSetup
    {
        // =================================================================
        // Boot / Splash Scene
        // =================================================================

        [MenuItem("Marble Odyssey/Scene Setup/Create Boot Scene Hierarchy")]
        public static void CreateBootScene()
        {
            ClearScene();

            // Camera
            var camGo = CreateCamera("Boot Camera");
            camGo.transform.position = new Vector3(0, 2, -10);
            camGo.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camGo.GetComponent<Camera>().backgroundColor = new Color(0.02f, 0.02f, 0.06f);

            // Light
            CreateDirectionalLight();

            // Canvas for splash / loading UI
            var canvas = CreateUICanvas("BootCanvas");
            CreateFullScreenPanel(canvas.transform, "SplashPanel");

            // Event system
            CreateEventSystem();

            // Managers root
            var mgrs = new GameObject("[Managers]");
            new GameObject("GameManager").transform.SetParent(mgrs.transform);
            new GameObject("AudioManager").transform.SetParent(mgrs.transform);

            Debug.Log("[SceneSetup] Boot scene hierarchy created.");
        }

        // =================================================================
        // Main Menu Scene
        // =================================================================

        [MenuItem("Marble Odyssey/Scene Setup/Create Main Menu Scene Hierarchy")]
        public static void CreateMainMenuScene()
        {
            ClearScene();

            // Camera
            var camGo = CreateCamera("MenuCamera");
            camGo.transform.position = new Vector3(0, 5, -12);
            camGo.transform.rotation = Quaternion.Euler(15, 0, 0);

            // Light
            CreateDirectionalLight();

            // Skybox placeholder
            CreateSkyboxNote();

            // Environment root for decorative marble scene
            var env = new GameObject("[MenuEnvironment]");
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "DisplayPlatform";
            platform.transform.SetParent(env.transform);
            platform.transform.localScale = new Vector3(6, 0.2f, 6);
            platform.transform.position = Vector3.zero;

            var marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marble.name = "DisplayMarble";
            marble.transform.SetParent(env.transform);
            marble.transform.position = new Vector3(0, 1.5f, 0);

            // Canvas
            var canvas = CreateUICanvas("MenuCanvas");
            CreateFullScreenPanel(canvas.transform, "TitlePanel");
            CreateFullScreenPanel(canvas.transform, "SettingsPanel").SetActive(false);

            // Event system
            CreateEventSystem();

            // Particle system placeholder
            var particles = new GameObject("[Particles]");
            particles.AddComponent<ParticleSystem>();
            var main = particles.GetComponent<ParticleSystem>().main;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;
            main.startLifetime = 4f;
            main.maxParticles = 100;

            Debug.Log("[SceneSetup] Main menu scene hierarchy created.");
        }

        // =================================================================
        // Gameplay Scene
        // =================================================================

        [MenuItem("Marble Odyssey/Scene Setup/Create Gameplay Scene Hierarchy")]
        public static void CreateGameplayScene()
        {
            ClearScene();

            // Camera (Cinemachine will drive this)
            var camGo = CreateCamera("GameplayCamera");
            camGo.transform.position = new Vector3(0, 8, -10);
            camGo.transform.rotation = Quaternion.Euler(30, 0, 0);

            // Lights
            var sun = CreateDirectionalLight();
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Environment root
            var env = new GameObject("[Environment]");

            // Level root — LevelManager populates children dynamically
            var levelRoot = new GameObject("[LevelRoot]");
            levelRoot.transform.SetParent(env.transform);

            // Marble spawn
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 2, 0);

            // Player marble
            var marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marble.name = "PlayerMarble";
            marble.tag  = "Marble";
            marble.layer = 6; // Marble layer
            marble.transform.position = new Vector3(0, 2, 0);
            var rb = marble.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // A simple starter platform
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StartPlatform";
            floor.tag  = "Untagged";
            floor.layer = 7; // Platform layer
            floor.transform.SetParent(levelRoot.transform);
            floor.transform.localScale = new Vector3(8, 0.5f, 30);
            floor.transform.position   = new Vector3(0, -0.25f, 10);

            // Finish zone (trigger)
            var finish = new GameObject("FinishZone");
            finish.tag   = "FinishZone";
            finish.layer = 10; // Trigger
            finish.transform.SetParent(levelRoot.transform);
            finish.transform.position = new Vector3(0, 1, 24);
            var finBox = finish.AddComponent<BoxCollider>();
            finBox.isTrigger = true;
            finBox.size = new Vector3(4, 3, 2);

            // Death zone
            var death = new GameObject("DeathZone");
            death.tag = "DeathZone";
            death.transform.SetParent(env.transform);
            death.transform.position = new Vector3(0, -20, 0);
            var dBox = death.AddComponent<BoxCollider>();
            dBox.isTrigger = true;
            dBox.size = new Vector3(500, 1, 500);

            // HUD canvas
            var canvas = CreateUICanvas("HUDCanvas");
            CreateFullScreenPanel(canvas.transform, "HUDPanel");
            CreateFullScreenPanel(canvas.transform, "PausePanel").SetActive(false);
            CreateFullScreenPanel(canvas.transform, "LevelCompletePanel").SetActive(false);
            CreateFullScreenPanel(canvas.transform, "GameOverPanel").SetActive(false);

            // Event system
            CreateEventSystem();

            // Post-processing volume placeholder
            var ppVolume = new GameObject("[PostProcessVolume]");
            // UnityEngine.Rendering.Volume would be added via URP — commented to avoid
            // compile errors when URP isn't imported yet:
            // ppVolume.AddComponent<UnityEngine.Rendering.Volume>();

            Debug.Log("[SceneSetup] Gameplay scene hierarchy created. " +
                      "Attach MarbleController, CameraController, LevelManager scripts.");
        }

        // =================================================================
        // World Selection Scene
        // =================================================================

        [MenuItem("Marble Odyssey/Scene Setup/Create World Select Scene Hierarchy")]
        public static void CreateWorldSelectScene()
        {
            ClearScene();

            var camGo = CreateCamera("WorldSelectCamera");
            camGo.transform.position = new Vector3(0, 10, -15);
            camGo.transform.rotation = Quaternion.Euler(25, 0, 0);

            CreateDirectionalLight();
            CreateSkyboxNote();

            var worldRoot = new GameObject("[WorldNodes]");
            string[] worldNames = {
                "Crystal Caverns", "Neon City", "Ancient Ruins",
                "Sky Fortress", "Lava Depths", "Cosmic Void"
            };
            for (int i = 0; i < worldNames.Length; i++)
            {
                var node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                node.name = $"WorldNode_{i}_{worldNames[i].Replace(" ", "")}";
                node.transform.SetParent(worldRoot.transform);
                float angle = i * Mathf.PI * 2f / worldNames.Length;
                node.transform.position = new Vector3(
                    Mathf.Cos(angle) * 8f, 0, Mathf.Sin(angle) * 8f);
                node.transform.localScale = Vector3.one * 2f;
            }

            var canvas = CreateUICanvas("WorldSelectCanvas");
            CreateFullScreenPanel(canvas.transform, "WorldInfoPanel");

            CreateEventSystem();

            Debug.Log("[SceneSetup] World select scene hierarchy created.");
        }

        // =================================================================
        // Helpers
        // =================================================================

        private static void ClearScene()
        {
            // Note: does not clear the scene — that would be destructive.
            // Just logs a reminder.
            Debug.Log("[SceneSetup] Create a new empty scene before stamping hierarchy.");
        }

        private static GameObject CreateCamera(string name)
        {
            var go = new GameObject(name);
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 300f;
            cam.fieldOfView   = 60f;
            go.AddComponent<AudioListener>();
            return go;
        }

        private static GameObject CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
            return go;
        }

        private static GameObject CreateUICanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // transparent background
            return go;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private static void CreateSkyboxNote()
        {
            var note = new GameObject("-- Assign SkyboxGradient Material to Lighting > Skybox --");
            note.transform.position = Vector3.zero;
        }
    }
}
#endif
