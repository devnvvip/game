using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using StreamerGame.Core;
using StreamerGame.Player;
using StreamerGame.Interactables;
using StreamerGame.Livestream;
using StreamerGame.UI;
using StreamerGame.Audio;

namespace StreamerGame.Editor
{
    [InitializeOnLoad]
    public static class SceneAutoBuilder
    {
        private const string SceneVersion = "StreamerIdol_v1.0.4";
        private const string ScenePrefKey = "StreamerIdol_BuiltVersion";
        private const string ScenePath = "Assets/Scenes/MainStreamerScene.unity";

        static SceneAutoBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            string builtVer = EditorPrefs.GetString(ScenePrefKey, "");
            if (builtVer != SceneVersion || !File.Exists(ScenePath))
            {
                Debug.Log($"[SceneAutoBuilder] Scene not found or version mismatch ({builtVer} != {SceneVersion}). Building scene now...");
                RebuildScene();
            }
        }

        [MenuItem("Game/Rebuild Scene")]
        public static void RebuildScene()
        {
            try
            {
                Debug.Log("[SceneAutoBuilder] Starting procedural construction of Idol Streamer Apartment Scene...");

                // Create new scene
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Setup Render Settings & Environment
                SetupEnvironment();

                // Build 3D Apartment
                GameObject apartmentRoot = new GameObject("Apartment");
                BuildApartmentGeometry(apartmentRoot);

                // Build Props & Interactables
                GameObject propsRoot = new GameObject("InteractableProps");
                Transform sitPoint;
                BuildPropsAndInteractables(propsRoot, out sitPoint);

                // Setup Lighting
                Light sunLight, ceilingLight, ringLight, bedLamp;
                SetupLighting(apartmentRoot, out sunLight, out ceilingLight, out ringLight, out bedLamp);

                // Build Player
                GameObject playerObj = BuildPlayer(sitPoint);

                // Build UI Canvas
                GameObject canvasObj = BuildUICanvas();

                // Build Managers
                BuildManagers(sunLight, ceilingLight, ringLight, bedLamp, canvasObj);

                // Ensure Scenes folder exists
                if (!Directory.Exists("Assets/Scenes"))
                {
                    Directory.CreateDirectory("Assets/Scenes");
                }

                // Save Scene
                bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"[SceneAutoBuilder] Saved scene to {ScenePath}: {saved}");

                // Register Scene in Build Settings at index 0
                AddSceneToBuildSettings(ScenePath);

                // Mark version as built
                EditorPrefs.SetString(ScenePrefKey, SceneVersion);
                Debug.Log($"[SceneAutoBuilder] Scene build completed successfully! Version: {SceneVersion}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneAutoBuilder] Error during scene build: {ex}");
            }
        }

        private static void SetupEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);
        }

        private static Shader GetLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness = 0.5f, float metallic = 0.1f)
        {
            Material mat = new Material(GetLitShader())
            {
                name = name,
                color = color
            };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        private static Material CreateEmissiveMaterial(string name, Color color, Color emissionColor)
        {
            Material mat = CreateMaterial(name, color, 0.8f, 0.2f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            return mat;
        }

        private static void SetupLighting(GameObject parent, out Light sun, out Light ceiling, out Light ring, out Light bed)
        {
            GameObject lightsObj = new GameObject("Lighting");
            lightsObj.transform.SetParent(parent.transform);

            // Sun Directional Light
            GameObject sunObj = new GameObject("SunLight_Directional");
            sunObj.transform.SetParent(lightsObj.transform);
            sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.92f, 0.82f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(28f, 50f, 0f);

            // Ceiling Recessed Room Light
            GameObject ceilObj = new GameObject("Ceiling_RoomLight");
            ceilObj.transform.SetParent(lightsObj.transform);
            ceilObj.transform.position = new Vector3(0f, 2.9f, 0f);
            ceiling = ceilObj.AddComponent<Light>();
            ceiling.type = LightType.Point;
            ceiling.color = new Color(0.95f, 0.95f, 1f);
            ceiling.intensity = 0.8f;
            ceiling.range = 14f;
            ceiling.shadows = LightShadows.Soft;

            // Streamer Studio Ring Light (Spotlight)
            GameObject ringObj = new GameObject("Studio_RingSpotLight");
            ringObj.transform.SetParent(lightsObj.transform);
            ringObj.transform.position = new Vector3(3.2f, 1.45f, 2.3f);
            ringObj.transform.rotation = Quaternion.Euler(15f, 180f, 0f);
            ring = ringObj.AddComponent<Light>();
            ring.type = LightType.Spot;
            ring.spotAngle = 75f;
            ring.color = new Color(1f, 0.95f, 0.9f);
            ring.intensity = 1.6f;
            ring.range = 6f;
            ring.shadows = LightShadows.Soft;
            ring.enabled = false;

            // Bedside warm lamp
            GameObject bedObj = new GameObject("Bedside_LampLight");
            bedObj.transform.SetParent(lightsObj.transform);
            bedObj.transform.position = new Vector3(-3.8f, 0.9f, -1.8f);
            bed = bedObj.AddComponent<Light>();
            bed.type = LightType.Point;
            bed.color = new Color(1f, 0.8f, 0.55f);
            bed.intensity = 0.6f;
            bed.range = 5f;
            bed.shadows = LightShadows.Soft;
        }

        private static void BuildApartmentGeometry(GameObject root)
        {
            Material floorMat = CreateMaterial("Mat_WoodFloor", new Color(0.35f, 0.25f, 0.18f), 0.6f, 0.05f);
            Material wallMat = CreateMaterial("Mat_ModernWall", new Color(0.92f, 0.92f, 0.93f), 0.2f, 0f);
            Material ceilingMat = CreateMaterial("Mat_Ceiling", new Color(0.96f, 0.96f, 0.96f), 0.1f, 0f);
            Material studioAccentMat = CreateMaterial("Mat_StudioAccent", new Color(0.12f, 0.14f, 0.18f), 0.3f, 0.1f);
            Material glassMat = CreateMaterial("Mat_WindowGlass", new Color(0.6f, 0.8f, 0.95f, 0.3f), 0.9f, 0.2f);

            // Floor (14m x 10m)
            CreateBox("Floor", root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(14f, 0.1f, 10f), floorMat);

            // Ceiling
            CreateBox("Ceiling", root.transform, new Vector3(0f, 3.05f, 0f), new Vector3(14f, 0.1f, 10f), ceilingMat);

            // Outer Walls
            CreateBox("Wall_North", root.transform, new Vector3(0f, 1.5f, 5f), new Vector3(14f, 3f, 0.2f), wallMat);
            CreateBox("Wall_South", root.transform, new Vector3(0f, 1.5f, -5f), new Vector3(14f, 3f, 0.2f), wallMat);
            CreateBox("Wall_West", root.transform, new Vector3(-7f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallMat);
            CreateBox("Wall_East", root.transform, new Vector3(7f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallMat);

            // Interior Partition: Separates Bedroom (West) from Living/Studio/Kitchen (East)
            CreateBox("Partition_BedroomWall", root.transform, new Vector3(-2.2f, 1.5f, -1.5f), new Vector3(0.2f, 3f, 7f), wallMat);

            // Studio Back Wall Accent (behind streamer desk)
            CreateBox("Studio_AcousticWall", root.transform, new Vector3(3.2f, 1.5f, 4.88f), new Vector3(5.5f, 2.8f, 0.08f), studioAccentMat);

            // Bedroom Window
            CreateBox("Window_Bedroom", root.transform, new Vector3(-7f, 1.6f, 2f), new Vector3(0.05f, 1.8f, 2.5f), glassMat);

            // Living Room Panoramic Window
            CreateBox("Window_Studio", root.transform, new Vector3(7f, 1.6f, 2f), new Vector3(0.05f, 2.0f, 4f), glassMat);
        }

        private static void BuildPropsAndInteractables(GameObject root, out Transform sitPoint)
        {
            Material deskMat = CreateMaterial("Mat_DeskBlack", new Color(0.1f, 0.1f, 0.12f), 0.7f, 0.1f);
            Material metalMat = CreateMaterial("Mat_ChromeMetal", new Color(0.25f, 0.25f, 0.28f), 0.8f, 0.8f);
            Material screenOffMat = CreateEmissiveMaterial("Mat_MonitorScreens", new Color(0.08f, 0.1f, 0.12f), new Color(0.1f, 0.2f, 0.4f) * 1.5f);
            Material bedWoodMat = CreateMaterial("Mat_BedWood", new Color(0.4f, 0.26f, 0.16f), 0.4f, 0.05f);
            Material bedClothMat = CreateMaterial("Mat_BedSheet", new Color(0.85f, 0.88f, 0.92f), 0.2f, 0f);
            Material pillowMat = CreateMaterial("Mat_Pillow", new Color(0.95f, 0.95f, 0.95f), 0.2f, 0f);
            Material chairLeatherMat = CreateMaterial("Mat_GamingChair", new Color(0.12f, 0.12f, 0.15f), 0.5f, 0.1f);
            Material chairAccentMat = CreateMaterial("Mat_ChairNeon", new Color(0f, 0.8f, 1f), 0.7f, 0.2f);
            Material fridgeMat = CreateMaterial("Mat_FridgeSilver", new Color(0.7f, 0.72f, 0.75f), 0.75f, 0.6f);
            Material marbleMat = CreateMaterial("Mat_KitchenMarble", new Color(0.92f, 0.92f, 0.9f), 0.85f, 0.05f);

            // ==========================================
            // 1. STREAMER STUDIO SETUP
            // ==========================================
            GameObject studioDeskObj = new GameObject("Streamer_Desk_Setup");
            studioDeskObj.transform.SetParent(root.transform);
            studioDeskObj.transform.position = new Vector3(3.2f, 0f, 3.8f);

            // Desk tabletop (2.4m wide x 0.9m deep x 0.75m high)
            CreateBox("DeskTop", studioDeskObj.transform, new Vector3(0f, 0.73f, 0f), new Vector3(2.4f, 0.05f, 0.9f), deskMat);
            CreateBox("DeskLeg_L", studioDeskObj.transform, new Vector3(-1.1f, 0.36f, 0f), new Vector3(0.06f, 0.72f, 0.8f), metalMat);
            CreateBox("DeskLeg_R", studioDeskObj.transform, new Vector3(1.1f, 0.36f, 0f), new Vector3(0.06f, 0.72f, 0.8f), metalMat);

            // Triple Monitor Setup
            // Center Monitor 32" Curved
            CreateBox("Monitor_Center", studioDeskObj.transform, new Vector3(0f, 1.15f, 0.32f), new Vector3(0.9f, 0.52f, 0.04f), screenOffMat);
            CreateBox("Monitor_Stand_Center", studioDeskObj.transform, new Vector3(0f, 0.85f, 0.35f), new Vector3(0.18f, 0.25f, 0.05f), metalMat);

            // Left Chat Monitor (vertical portrait)
            GameObject monLeft = CreateBox("Monitor_Left_Chat", studioDeskObj.transform, new Vector3(-0.65f, 1.18f, 0.22f), new Vector3(0.35f, 0.60f, 0.04f), screenOffMat);
            monLeft.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

            // Right Monitor (angled)
            GameObject monRight = CreateBox("Monitor_Right_Aux", studioDeskObj.transform, new Vector3(0.65f, 1.15f, 0.22f), new Vector3(0.7f, 0.48f, 0.04f), screenOffMat);
            monRight.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);

            // Streamer Webcam (on top of center monitor)
            CreateBox("Webcam_4K", studioDeskObj.transform, new Vector3(0f, 1.44f, 0.32f), new Vector3(0.12f, 0.04f, 0.04f), metalMat);

            // High-end PC Tower
            GameObject pcTower = CreateBox("PC_Tower_RGB", studioDeskObj.transform, new Vector3(0.95f, 1.05f, 0.15f), new Vector3(0.24f, 0.52f, 0.50f), deskMat);
            CreateBox("PC_GlassWindow", pcTower.transform, new Vector3(-0.13f, 0f, 0f), new Vector3(0.02f, 0.46f, 0.46f), CreateEmissiveMaterial("Mat_PCGlow", Color.cyan, Color.cyan * 1.5f));

            // Microphone on Boom Arm
            GameObject micBase = CreateBox("Mic_BoomArm", studioDeskObj.transform, new Vector3(-0.7f, 0.76f, -0.1f), new Vector3(0.08f, 0.35f, 0.08f), metalMat);
            CreateBox("Mic_Capsule", micBase.transform, new Vector3(0.2f, 0.35f, 0.2f), new Vector3(0.06f, 0.14f, 0.06f), CreateMaterial("Mat_MicBlack", Color.black, 0.8f, 0.5f));

            // Gaming Chair
            GameObject chairObj = new GameObject("GamingChair");
            chairObj.transform.SetParent(studioDeskObj.transform);
            chairObj.transform.localPosition = new Vector3(0f, 0f, -0.7f);
            chairObj.transform.localRotation = Quaternion.identity;
            CreateBox("Chair_Seat", chairObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.1f, 0.55f), chairLeatherMat);
            CreateBox("Chair_Back", chairObj.transform, new Vector3(0f, 0.95f, -0.25f), new Vector3(0.5f, 0.9f, 0.1f), chairLeatherMat);
            CreateBox("Chair_NeonStripe", chairObj.transform, new Vector3(0f, 0.95f, -0.2f), new Vector3(0.4f, 0.85f, 0.02f), chairAccentMat);
            CreateBox("Chair_Base", chairObj.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.05f, 0.5f), metalMat);

            // Chair Sit Point
            GameObject sitAnchor = new GameObject("SitAnchor");
            sitAnchor.transform.SetParent(chairObj.transform);
            sitAnchor.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            sitAnchor.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            sitPoint = sitAnchor.transform;

            // Streaming Desk Interactable Component
            StreamingDesk deskInteractable = studioDeskObj.AddComponent<StreamingDesk>();
            deskInteractable.chairSitPoint = sitPoint;

            // Studio Ring Light on Tripod
            GameObject ringLightObj = new GameObject("Studio_RingLight_Prop");
            ringLightObj.transform.SetParent(root.transform);
            ringLightObj.transform.position = new Vector3(2.3f, 0f, 2.5f);
            CreateBox("Tripod_Stand", ringLightObj.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.05f, 1.4f, 0.05f), metalMat);
            GameObject ringMesh = CreateBox("Ring_Head", ringLightObj.transform, new Vector3(0f, 1.45f, 0f), new Vector3(0.45f, 0.45f, 0.05f), CreateEmissiveMaterial("Mat_RingGlow", Color.white, Color.white * 2.0f));
            StudioRingLight ringScript = ringLightObj.AddComponent<StudioRingLight>();
            ringScript.ringMeshRenderer = ringMesh.GetComponent<Renderer>();

            // Trophy Shelf with Gold Play Button
            GameObject shelfObj = new GameObject("Trophy_Shelf");
            shelfObj.transform.SetParent(root.transform);
            shelfObj.transform.position = new Vector3(5.8f, 1.4f, 4.85f);
            CreateBox("Shelf_Plank1", shelfObj.transform, new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0.04f, 0.3f), deskMat);
            CreateBox("Shelf_Plank2", shelfObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.04f, 0.3f), deskMat);
            // YouTube Golden Creator Award
            CreateBox("Gold_PlayButton", shelfObj.transform, new Vector3(-0.4f, 0.22f, 0.05f), new Vector3(0.32f, 0.25f, 0.04f), CreateMaterial("Mat_GoldAward", new Color(1f, 0.84f, 0f), 0.9f, 0.9f));
            // Anime Idol Figure
            CreateBox("Idol_Figure", shelfObj.transform, new Vector3(0.3f, 0.15f, 0.05f), new Vector3(0.1f, 0.25f, 0.1f), chairAccentMat);

            // ==========================================
            // 2. MASTER BEDROOM SETUP
            // ==========================================
            GameObject bedObj = new GameObject("Master_Bed_Setup");
            bedObj.transform.SetParent(root.transform);
            bedObj.transform.position = new Vector3(-4.8f, 0f, -2.5f);

            // Bed Frame & Mattress (2m wide x 2.2m long)
            CreateBox("Bed_Frame", bedObj.transform, new Vector3(0f, 0.2f, 0f), new Vector3(2.0f, 0.3f, 2.2f), bedWoodMat);
            CreateBox("Bed_Headboard", bedObj.transform, new Vector3(0f, 0.8f, -1.05f), new Vector3(2.1f, 1.1f, 0.12f), bedWoodMat);
            CreateBox("Bed_Mattress", bedObj.transform, new Vector3(0f, 0.45f, 0.05f), new Vector3(1.85f, 0.3f, 2.0f), bedClothMat);
            CreateBox("Bed_Pillow_L", bedObj.transform, new Vector3(-0.5f, 0.65f, -0.7f), new Vector3(0.6f, 0.12f, 0.4f), pillowMat);
            CreateBox("Bed_Pillow_R", bedObj.transform, new Vector3(0.5f, 0.65f, -0.7f), new Vector3(0.6f, 0.12f, 0.4f), pillowMat);
            CreateBox("Bed_Duvet", bedObj.transform, new Vector3(0f, 0.62f, 0.35f), new Vector3(1.88f, 0.06f, 1.35f), CreateMaterial("Mat_Duvet", new Color(0.2f, 0.35f, 0.5f), 0.3f, 0f));
            bedObj.AddComponent<Bed>();

            // Nightstand & Alarm Clock
            GameObject nightstand = new GameObject("Nightstand_Alarm");
            nightstand.transform.SetParent(root.transform);
            nightstand.transform.position = new Vector3(-3.5f, 0f, -3.4f);
            CreateBox("Nightstand_Table", nightstand.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.55f, 0.6f, 0.55f), bedWoodMat);

            GameObject clockObj = CreateBox("Digital_AlarmClock", nightstand.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.25f, 0.1f, 0.12f), CreateMaterial("Mat_ClockBody", Color.black, 0.7f, 0.2f));
            CreateBox("Clock_LED", clockObj.transform, new Vector3(0f, 0f, 0.065f), new Vector3(0.2f, 0.06f, 0.01f), CreateEmissiveMaterial("Mat_ClockRedLED", Color.red, Color.red * 2.5f));
            clockObj.AddComponent<AlarmClock>();

            // Wardrobe
            GameObject wardrobeObj = new GameObject("Wardrobe_Idol");
            wardrobeObj.transform.SetParent(root.transform);
            wardrobeObj.transform.position = new Vector3(-6.2f, 0f, 0.5f);
            CreateBox("Wardrobe_Cabinet", wardrobeObj.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.8f, 2.5f, 2.2f), deskMat);
            CreateBox("Wardrobe_MirrorDoor", wardrobeObj.transform, new Vector3(0.42f, 1.25f, 0f), new Vector3(0.04f, 2.3f, 1.0f), CreateMaterial("Mat_Mirror", new Color(0.9f, 0.95f, 1f), 0.95f, 0.8f));
            wardrobeObj.AddComponent<Wardrobe>();

            // ==========================================
            // 3. KITCHEN & DINING SETUP
            // ==========================================
            GameObject kitchenObj = new GameObject("Kitchen_Setup");
            kitchenObj.transform.SetParent(root.transform);
            kitchenObj.transform.position = new Vector3(0f, 0f, -3.8f);

            // Kitchen Counter / Bar (3m long x 0.8m deep)
            CreateBox("Kitchen_Counter", kitchenObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(3.2f, 0.9f, 0.8f), marbleMat);

            // Coffee Machine
            GameObject coffeeMachineObj = CreateBox("Espresso_CoffeeMachine", kitchenObj.transform, new Vector3(-0.8f, 1.08f, 0f), new Vector3(0.35f, 0.35f, 0.35f), metalMat);
            CreateBox("Coffee_Cup", coffeeMachineObj.transform, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.1f, 0.12f, 0.1f), pillowMat);
            coffeeMachineObj.AddComponent<CoffeeMachine>();

            // Refrigerator
            GameObject fridgeObj = CreateBox("Refrigerator_Smart", kitchenObj.transform, new Vector3(1.2f, 1.0f, 0f), new Vector3(0.85f, 2.0f, 0.75f), fridgeMat);
            CreateBox("Fridge_Handle", fridgeObj.transform, new Vector3(-0.35f, 0f, 0.4f), new Vector3(0.05f, 0.6f, 0.05f), metalMat);
            fridgeObj.AddComponent<Refrigerator>();

            // ==========================================
            // 4. BATHROOM SETUP
            // ==========================================
            GameObject bathroomObj = new GameObject("Bathroom_Setup");
            bathroomObj.transform.SetParent(root.transform);
            bathroomObj.transform.position = new Vector3(-5.5f, 0f, 3.8f);

            // Sink vanity
            GameObject sinkObj = CreateBox("Vanity_Sink", bathroomObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.88f, 0.65f), marbleMat);
            CreateBox("Chrome_Faucet", sinkObj.transform, new Vector3(0f, 0.55f, -0.15f), new Vector3(0.06f, 0.22f, 0.15f), metalMat);
            CreateBox("Vanity_Mirror", sinkObj.transform, new Vector3(0f, 1.15f, 0.3f), new Vector3(1.0f, 0.8f, 0.04f), CreateMaterial("Mat_BathMirror", new Color(0.9f, 0.95f, 1f), 0.95f, 0.8f));
            sinkObj.AddComponent<BathroomSink>();
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = size;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            return obj;
        }

        private static GameObject BuildPlayer(Transform sitPoint)
        {
            GameObject player = new GameObject("Player_IdolStreamer");
            player.transform.position = new Vector3(-3.2f, 1.0f, -2.0f); // Start near bed
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            PlayerController pc = player.AddComponent<PlayerController>();
            PlayerInteraction pi = player.AddComponent<PlayerInteraction>();
            MobileInputController mic = player.AddComponent<MobileInputController>();

            // Camera Holder
            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camHolder.transform.localRotation = Quaternion.identity;

            // Main Camera
            GameObject camObj = new GameObject("MainCamera");
            camObj.transform.SetParent(camHolder.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 150f;

            camObj.AddComponent<AudioListener>();

            // Wire references
            pc.cameraHolder = camHolder.transform;
            pi.raycastOrigin = cam.transform;

            return player;
        }

        private static GameObject BuildUICanvas()
        {
            GameObject canvasObj = new GameObject("UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Event System
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // ==========================================
            // TOP STATUS HUD
            // ==========================================
            GameObject topBar = CreateUIEmpty("TopStatusBar", canvasObj.transform);
            SetRectTransform(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(0f, 70f));
            AddImage(topBar, new Color(0.08f, 0.1f, 0.14f, 0.85f));

            Text phaseText = AddText(topBar, "PhaseText", "Buổi Sáng (07:00 - 11:30)", defaultFont, 20, Color.cyan, TextAnchor.MiddleLeft);
            SetRectTransform(phaseText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(350f, 40f));

            // Energy & Mood
            Slider energySlider = CreateSimpleSlider("EnergySlider", topBar.transform, new Vector2(400f, 0f), "Năng lượng: ", defaultFont, Color.yellow);
            Slider moodSlider = CreateSimpleSlider("MoodSlider", topBar.transform, new Vector2(650f, 0f), "Tâm trạng: ", defaultFont, new Color(1f, 0.4f, 0.8f));

            Text followersText = AddText(topBar, "FollowersText", "♥ 128,400", defaultFont, 20, Color.white, TextAnchor.MiddleRight);
            SetRectTransform(followersText.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, 0f), new Vector2(180f, 40f));

            Text revenueText = AddText(topBar, "RevenueText", "15,500,000 đ", defaultFont, 20, Color.green, TextAnchor.MiddleRight);
            SetRectTransform(revenueText.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(180f, 40f));

            // ==========================================
            // QUEST CARD
            // ==========================================
            GameObject questBox = CreateUIEmpty("QuestCard", canvasObj.transform);
            SetRectTransform(questBox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -120f), new Vector2(380f, 100f));
            AddImage(questBox, new Color(0.05f, 0.08f, 0.12f, 0.8f));

            Text questTitle = AddText(questBox, "QuestTitle", "[NHIỆM VỤ] Tắt chuông báo thức", defaultFont, 18, Color.yellow, TextAnchor.UpperLeft);
            SetRectTransform(questTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15f, -15f), new Vector2(-30f, 30f));

            Text questDesc = AddText(questBox, "QuestDesc", "Thức dậy và nhấn nút tắt chuông báo thức bên cạnh giường.", defaultFont, 15, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(questDesc.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(15f, 10f), new Vector2(-30f, -45f));

            // ==========================================
            // RETICLE & INTERACTION PROMPT
            // ==========================================
            GameObject reticle = CreateUIEmpty("ReticleDot", canvasObj.transform);
            SetRectTransform(reticle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            AddImage(reticle, Color.white);

            GameObject interactPromptBox = CreateUIEmpty("InteractionPromptBox", canvasObj.transform);
            SetRectTransform(interactPromptBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(400f, 45f));
            AddImage(interactPromptBox, new Color(0f, 0f, 0f, 0.75f));
            Text interactPromptText = AddText(interactPromptBox, "PromptText", "[E] Tương tác", defaultFont, 18, Color.yellow, TextAnchor.MiddleCenter);
            SetRectTransform(interactPromptText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // ==========================================
            // NOTIFICATION TOAST
            // ==========================================
            GameObject notifBox = CreateUIEmpty("NotificationToast", canvasObj.transform);
            SetRectTransform(notifBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(600f, 50f));
            AddImage(notifBox, new Color(0.1f, 0.35f, 0.2f, 0.9f));
            Text notifText = AddText(notifBox, "NotifText", "Thông báo", defaultFont, 17, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(notifText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // ==========================================
            // LIVESTREAM OVERLAY UI
            // ==========================================
            GameObject streamOverlay = CreateUIEmpty("LivestreamOverlay", canvasObj.transform);
            SetRectTransform(streamOverlay, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            StreamOverlayUI overlayUI = streamOverlay.AddComponent<StreamOverlayUI>();
            overlayUI.overlayRoot = streamOverlay;

            // Live Header Info
            GameObject liveHeader = CreateUIEmpty("LiveHeader", streamOverlay.transform);
            SetRectTransform(liveHeader, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(0f, 60f));
            AddImage(liveHeader, new Color(0.05f, 0.05f, 0.1f, 0.9f));

            Text liveBadge = AddText(liveHeader, "LiveBadge", "● TRỰC TIẾP", defaultFont, 20, Color.red, TextAnchor.MiddleLeft);
            SetRectTransform(liveBadge.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(160f, 40f));

            Text viewersTxt = AddText(liveHeader, "LiveViewers", "👁 3,500", defaultFont, 20, Color.white, TextAnchor.MiddleLeft);
            SetRectTransform(viewersTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(160f, 40f));
            overlayUI.viewersText = viewersTxt;

            Text likesTxt = AddText(liveHeader, "LiveLikes", "❤️ 1,200", defaultFont, 20, new Color(1f, 0.4f, 0.5f), TextAnchor.MiddleLeft);
            SetRectTransform(likesTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(160f, 40f));
            overlayUI.likesText = likesTxt;

            Text durationTxt = AddText(liveHeader, "LiveDuration", "⏱ 00:00", defaultFont, 20, Color.yellow, TextAnchor.MiddleLeft);
            SetRectTransform(durationTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(560f, 0f), new Vector2(160f, 40f));
            overlayUI.durationText = durationTxt;

            Text earningsTxt = AddText(liveHeader, "LiveEarnings", "⭐ +0 đ", defaultFont, 20, Color.green, TextAnchor.MiddleRight);
            SetRectTransform(earningsTxt.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, 0f), new Vector2(200f, 40f));
            overlayUI.liveEarningsText = earningsTxt;

            Button endStreamBtn = CreateSimpleButton("EndStreamButton", liveHeader.transform, new Vector2(-30f, 0f), new Vector2(180f, 40f), "KẾT THÚC LIVE", defaultFont, Color.red);
            SetRectTransform(endStreamBtn.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(180f, 40f));
            overlayUI.endStreamButton = endStreamBtn;

            // Live Chat Panel (Right Side)
            GameObject chatPanel = CreateUIEmpty("LiveChatBox", streamOverlay.transform);
            SetRectTransform(chatPanel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-20f, -40f), new Vector2(420f, -160f));
            AddImage(chatPanel, new Color(0.04f, 0.05f, 0.08f, 0.85f));

            Text[] chatLines = new Text[8];
            for (int i = 0; i < 8; i++)
            {
                Text line = AddText(chatPanel, $"ChatLine_{i}", $"Chat message {i}", defaultFont, 16, Color.white, TextAnchor.MiddleLeft);
                SetRectTransform(line.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(15f, 20f + i * 40f), new Vector2(-30f, 35f));
                chatLines[i] = line;
            }
            overlayUI.chatLineTexts = chatLines;

            // Question Modal
            GameObject qModal = CreateUIEmpty("InteractiveQuestionModal", streamOverlay.transform);
            SetRectTransform(qModal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 380f));
            AddImage(qModal, new Color(0.08f, 0.12f, 0.18f, 0.95f));
            overlayUI.questionModal = qModal;

            Text qTopic = AddText(qModal, "QuestionTopic", "Fan hỏi ý kiến của bạn", defaultFont, 20, Color.yellow, TextAnchor.MiddleCenter);
            SetRectTransform(qTopic.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(-40f, 70f));
            overlayUI.questionTopicText = qTopic;

            Button cBtn1 = CreateSimpleButton("Choice1", qModal.transform, new Vector2(0f, 30f), new Vector2(580f, 55f), "Câu trả lời A", defaultFont, new Color(0.2f, 0.4f, 0.7f));
            Button cBtn2 = CreateSimpleButton("Choice2", qModal.transform, new Vector2(0f, -35f), new Vector2(580f, 55f), "Câu trả lời B", defaultFont, new Color(0.2f, 0.4f, 0.7f));
            Button cBtn3 = CreateSimpleButton("Choice3", qModal.transform, new Vector2(0f, -100f), new Vector2(580f, 55f), "Câu trả lời C", defaultFont, new Color(0.2f, 0.4f, 0.7f));
            overlayUI.choiceBtn1 = cBtn1;
            overlayUI.choiceText1 = cBtn1.GetComponentInChildren<Text>();
            overlayUI.choiceBtn2 = cBtn2;
            overlayUI.choiceText2 = cBtn2.GetComponentInChildren<Text>();
            overlayUI.choiceBtn3 = cBtn3;
            overlayUI.choiceText3 = cBtn3.GetComponentInChildren<Text>();

            // Incident Alert Banner
            GameObject incBanner = CreateUIEmpty("IncidentAlertBanner", streamOverlay.transform);
            SetRectTransform(incBanner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(700f, 120f));
            AddImage(incBanner, new Color(0.5f, 0.1f, 0.1f, 0.95f));
            overlayUI.incidentBanner = incBanner;

            Text incTitle = AddText(incBanner, "IncTitle", "SỰ CỐ LIVESTREAM!", defaultFont, 20, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(incTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 35f));
            overlayUI.incidentTitleText = incTitle;

            Text incDesc = AddText(incBanner, "IncDesc", "Mô tả sự cố", defaultFont, 16, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(incDesc.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-40f, 35f));
            overlayUI.incidentDescText = incDesc;

            Button incActionBtn = CreateSimpleButton("IncActionBtn", incBanner.transform, new Vector2(0f, -35f), new Vector2(300f, 40f), "Khắc phục ngay", defaultFont, Color.green);
            overlayUI.incidentActionButton = incActionBtn;
            overlayUI.incidentActionBtnText = incActionBtn.GetComponentInChildren<Text>();

            // ==========================================
            // SMARTPHONE MODAL UI
            // ==========================================
            GameObject phoneRoot = CreateUIEmpty("SmartphoneModal", canvasObj.transform);
            SetRectTransform(phoneRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 720f));
            AddImage(phoneRoot, new Color(0.08f, 0.08f, 0.1f, 0.98f));
            PhoneUIController phoneCtrl = phoneRoot.AddComponent<PhoneUIController>();
            phoneCtrl.phoneRoot = phoneRoot;

            // iPhone Notch / Island
            GameObject notch = CreateUIEmpty("DynamicIsland", phoneRoot.transform);
            SetRectTransform(notch, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(130f, 32f));
            AddImage(notch, Color.black);

            Text phoneClock = AddText(phoneRoot, "PhoneClock", "19:30", defaultFont, 16, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(phoneClock.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -18f), new Vector2(100f, 30f));
            phoneCtrl.phoneClockText = phoneClock;

            // Phone App Content Panels
            // 1. Messages Panel
            GameObject msgPanel = CreateUIEmpty("MessagesAppPanel", phoneRoot.transform);
            SetRectTransform(msgPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(-30f, -120f));
            phoneCtrl.messagesPanel = msgPanel;
            Text msgContent = AddText(msgPanel, "MsgText", "<b>[QUẢN LÝ IDOL]:</b>\n'Tối nay nhớ lên sóng đúng giờ nhé em. Tinh thần thật tốt vào!'\n\n<b>[TRƯỞNG FANCLUB]:</b>\n'Tụi em đã đẩy top trending cho anh rồi ạ! Chúc anh buổi stream rực rỡ!'\n\n<b>[NHÃN HÀNG]:</b>\n'Nhãn hàng gửi tặng set quà tai nghe mới, chúc mừng idol!'", defaultFont, 16, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(msgContent.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 2. Social Panel
            GameObject socPanel = CreateUIEmpty("SocialAppPanel", phoneRoot.transform);
            SetRectTransform(socPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(-30f, -120f));
            phoneCtrl.socialPanel = socPanel;
            Text socFollowers = AddText(socPanel, "SocFollowers", "Followers: 128,400", defaultFont, 18, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(socFollowers.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, 40f));
            phoneCtrl.socialFollowersText = socFollowers;

            Button postBtn = CreateSimpleButton("PostSocialBtn", socPanel.transform, new Vector2(0f, -100f), new Vector2(260f, 50f), "Đăng Ảnh Selfie Mới", defaultFont, new Color(0.9f, 0.2f, 0.5f));
            phoneCtrl.postButton = postBtn;

            Text socStatus = AddText(socPanel, "SocStatus", "Đăng bài để tăng thêm hàng ngàn followers!", defaultFont, 15, Color.gray, TextAnchor.MiddleCenter);
            SetRectTransform(socStatus.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(0f, 60f));
            phoneCtrl.socialStatusText = socStatus;

            // 3. Camera Panel
            GameObject camPanel = CreateUIEmpty("CameraAppPanel", phoneRoot.transform);
            SetRectTransform(camPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(-30f, -120f));
            phoneCtrl.cameraPanel = camPanel;
            AddText(camPanel, "CamViewfinder", "[Ống Kính Idol 4K Selfie]", defaultFont, 18, Color.cyan, TextAnchor.MiddleCenter);
            Button shutterBtn = CreateSimpleButton("ShutterBtn", camPanel.transform, new Vector2(0f, -200f), new Vector2(80f, 80f), "CHỤP", defaultFont, Color.white);
            phoneCtrl.shutterButton = shutterBtn;

            GameObject camFlash = CreateUIEmpty("CameraFlash", phoneRoot.transform);
            SetRectTransform(camFlash, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(camFlash, Color.white);
            camFlash.SetActive(false);
            phoneCtrl.cameraFlashImage = camFlash;

            // 4. Schedule Panel
            GameObject schedPanel = CreateUIEmpty("ScheduleAppPanel", phoneRoot.transform);
            SetRectTransform(schedPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(-30f, -120f));
            phoneCtrl.schedulePanel = schedPanel;
            Text schedText = AddText(schedPanel, "SchedSummary", "Lịch trình hôm nay", defaultFont, 15, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(schedText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            phoneCtrl.scheduleSummaryText = schedText;

            // Phone App Switcher Bar (Bottom)
            GameObject bottomBar = CreateUIEmpty("PhoneNavBar", phoneRoot.transform);
            SetRectTransform(bottomBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(-30f, 50f));
            Button b1 = CreateSimpleButton("Nav_Msg", bottomBar.transform, new Vector2(-120f, 0f), new Vector2(70f, 40f), "Chat", defaultFont, Color.gray);
            b1.onClick.AddListener(phoneCtrl.OpenMessagesApp);
            Button b2 = CreateSimpleButton("Nav_Soc", bottomBar.transform, new Vector2(-40f, 0f), new Vector2(70f, 40f), "MXH", defaultFont, Color.gray);
            b2.onClick.AddListener(phoneCtrl.OpenSocialApp);
            Button b3 = CreateSimpleButton("Nav_Cam", bottomBar.transform, new Vector2(40f, 0f), new Vector2(70f, 40f), "Cam", defaultFont, Color.gray);
            b3.onClick.AddListener(phoneCtrl.OpenCameraApp);
            Button b4 = CreateSimpleButton("Nav_Sch", bottomBar.transform, new Vector2(120f, 0f), new Vector2(70f, 40f), "Lịch", defaultFont, Color.gray);
            b4.onClick.AddListener(phoneCtrl.OpenScheduleApp);

            // Close Phone Button
            Button closePhoneBtn = CreateSimpleButton("ClosePhoneBtn", phoneRoot.transform, new Vector2(0f, -345f), new Vector2(120f, 8f), "", defaultFont, Color.white);
            closePhoneBtn.onClick.AddListener(phoneCtrl.TogglePhone);

            // ==========================================
            // PAUSE MENU UI
            // ==========================================
            GameObject pauseRoot = CreateUIEmpty("PauseMenu", canvasObj.transform);
            SetRectTransform(pauseRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(pauseRoot, new Color(0f, 0f, 0f, 0.85f));
            PauseMenuUI pauseUI = pauseRoot.AddComponent<PauseMenuUI>();
            pauseUI.pauseMenuRoot = pauseRoot;

            AddText(pauseRoot, "PauseTitle", "TẠM DỪNG TRÒ CHƠI", defaultFont, 32, Color.white, TextAnchor.MiddleCenter);
            Button resumeBtn = CreateSimpleButton("ResumeBtn", pauseRoot.transform, new Vector2(0f, 40f), new Vector2(280f, 55f), "Tiếp Tục", defaultFont, new Color(0.2f, 0.6f, 0.3f));
            Button restartBtn = CreateSimpleButton("RestartBtn", pauseRoot.transform, new Vector2(0f, -35f), new Vector2(280f, 55f), "Chơi Lại Ngày Này", defaultFont, new Color(0.7f, 0.2f, 0.2f));
            pauseUI.resumeButton = resumeBtn;
            pauseUI.restartButton = restartBtn;

            // ==========================================
            // END OF DAY SUMMARY UI
            // ==========================================
            GameObject summaryRoot = CreateUIEmpty("DaySummaryModal", canvasObj.transform);
            SetRectTransform(summaryRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(summaryRoot, new Color(0.04f, 0.06f, 0.1f, 0.95f));
            DaySummaryUI summaryUI = summaryRoot.AddComponent<DaySummaryUI>();
            summaryUI.summaryRoot = summaryRoot;

            Text sumTitle = AddText(summaryRoot, "SumTitle", "TỔNG KẾT MỘT NGÀY CỦA IDOL STREAMER", defaultFont, 28, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(sumTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(0f, 50f));

            Text rankTxt = AddText(summaryRoot, "RankText", "HẠNG S (XUẤT SẮC)", defaultFont, 36, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(rankTxt.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(0f, 60f));
            summaryUI.rankText = rankTxt;

            Text peakTxt = AddText(summaryRoot, "PeakTxt", "Lượng xem đỉnh cao: 15,200 người xem", defaultFont, 22, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(peakTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(0f, 40f));
            summaryUI.peakViewersText = peakTxt;

            Text followGainedTxt = AddText(summaryRoot, "FollowGainedTxt", "Lượng fan mới theo dõi: +3,800 followers", defaultFont, 22, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(followGainedTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 40f));
            summaryUI.followersGainedText = followGainedTxt;

            Text earnTxt = AddText(summaryRoot, "EarnTxt", "Thu nhập quà tặng buổi live: +3,500,000 VNĐ", defaultFont, 22, Color.green, TextAnchor.MiddleCenter);
            SetRectTransform(earnTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(0f, 40f));
            summaryUI.sessionEarningsText = earnTxt;

            Text commentTxt = AddText(summaryRoot, "CommentTxt", "Quản lý nhận xét: 'Một ngày làm việc xuất sắc!'", defaultFont, 18, Color.cyan, TextAnchor.MiddleCenter);
            SetRectTransform(commentTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(-100f, 60f));
            summaryUI.managerCommentText = commentTxt;

            Button playAgainBtn = CreateSimpleButton("PlayAgainBtn", summaryRoot.transform, new Vector2(0f, -220f), new Vector2(300f, 60f), "BẮT ĐẦU NGÀY MỚI", defaultFont, Color.yellow);
            summaryUI.playAgainButton = playAgainBtn;

            // ==========================================
            // MOBILE TOUCH CONTROLS UI
            // ==========================================
            GameObject mobControls = CreateUIEmpty("MobileTouchControls", canvasObj.transform);
            SetRectTransform(mobControls, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            MobileTouchControlsUI mobUI = mobControls.AddComponent<MobileTouchControlsUI>();
            mobUI.controlsRoot = mobControls;

            // Joystick (Bottom Left)
            GameObject joyBg = CreateUIEmpty("JoystickBG", mobControls.transform);
            SetRectTransform(joyBg, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(140f, 140f), new Vector2(160f, 160f));
            AddImage(joyBg, new Color(1f, 1f, 1f, 0.25f));
            mobUI.joystickBackground = joyBg.GetComponent<RectTransform>();

            GameObject joyHandle = CreateUIEmpty("JoystickHandle", joyBg.transform);
            SetRectTransform(joyHandle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            AddImage(joyHandle, new Color(1f, 1f, 1f, 0.6f));
            mobUI.joystickHandle = joyHandle.GetComponent<RectTransform>();

            // Large Tactile Interact Button (Bottom Right)
            Button interactBtn = CreateSimpleButton("MobileInteractBtn", mobControls.transform, new Vector2(-140f, 140f), new Vector2(130f, 130f), "TƯƠNG\nTÁC", defaultFont, new Color(0.15f, 0.55f, 0.95f, 0.85f));
            SetRectTransform(interactBtn.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-140f, 140f), new Vector2(130f, 130f));
            mobUI.interactButton = interactBtn;

            // Phone Icon Button (Top Right)
            Button phoneBtn = CreateSimpleButton("MobilePhoneBtn", mobControls.transform, new Vector2(-80f, -80f), new Vector2(90f, 50f), "PHONE", defaultFont, new Color(0.2f, 0.7f, 0.4f, 0.85f));
            SetRectTransform(phoneBtn.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -70f), new Vector2(90f, 50f));
            mobUI.phoneButton = phoneBtn;

            // Pause Button (Top Left)
            Button pauseBtn = CreateSimpleButton("MobilePauseBtn", mobControls.transform, new Vector2(60f, -60f), new Vector2(60f, 40f), "||", defaultFont, new Color(0.4f, 0.4f, 0.4f, 0.8f));
            SetRectTransform(pauseBtn.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(60f, -60f), new Vector2(60f, 40f));
            mobUI.pauseButton = pauseBtn;

            // ==========================================
            // WIRE HUD MANAGER
            // ==========================================
            HUDManager hud = canvasObj.AddComponent<HUDManager>();
            hud.phaseText = phaseText;
            hud.energySlider = energySlider;
            hud.moodSlider = moodSlider;
            hud.followersText = followersText;
            hud.revenueText = revenueText;
            hud.questContainer = questBox;
            hud.questTitleText = questTitle;
            hud.questDescText = questDesc;
            hud.reticleDot = reticle;
            hud.interactionPromptContainer = interactPromptBox;
            hud.interactionPromptText = interactPromptText;
            hud.notificationBanner = notifBox;
            hud.notificationText = notifText;
            hud.phoneController = phoneCtrl;
            hud.pauseMenuController = pauseUI;

            return canvasObj;
        }

        private static void BuildManagers(Light sun, Light ceiling, Light ring, Light bed, GameObject canvas)
        {
            GameObject managersObj = new GameObject("GameManagers");

            GameManager gm = managersObj.AddComponent<GameManager>();
            StatsManager sm = managersObj.AddComponent<StatsManager>();
            TimeOfDayManager tm = managersObj.AddComponent<TimeOfDayManager>();
            tm.sunLight = sun;
            tm.roomCeilingLight = ceiling;
            tm.streamerRingLight = ring;
            tm.bedsideLamp = bed;

            QuestManager qm = managersObj.AddComponent<QuestManager>();
            SoundManager sound = managersObj.AddComponent<SoundManager>();
            LivestreamManager lm = managersObj.AddComponent<LivestreamManager>();
            LiveChatManager lcm = managersObj.AddComponent<LiveChatManager>();
            StreamIncidentManager sim = managersObj.AddComponent<StreamIncidentManager>();
        }

        private static GameObject CreateUIEmpty(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static Image AddImage(GameObject obj, Color color)
        {
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text AddText(GameObject obj, string name, string text, Font font, int size, Color color, TextAnchor alignment)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform));
            txtObj.transform.SetParent(obj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            return t;
        }

        private static Button CreateSimpleButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Font font, Color bgColor)
        {
            GameObject btnObj = CreateUIEmpty(name, parent);
            SetRectTransform(btnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            AddImage(btnObj, bgColor);
            Button btn = btnObj.AddComponent<Button>();

            if (!string.IsNullOrEmpty(label))
            {
                Text txt = AddText(btnObj, "Text", label, font, 18, Color.white, TextAnchor.MiddleCenter);
                SetRectTransform(txt.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            return btn;
        }

        private static Slider CreateSimpleSlider(string name, Transform parent, Vector2 pos, string label, Font font, Color fillColor)
        {
            GameObject sliderObj = CreateUIEmpty(name, parent);
            SetRectTransform(sliderObj, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, new Vector2(220f, 25f));

            // Background
            GameObject bg = CreateUIEmpty("Background", sliderObj.transform);
            SetRectTransform(bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(bg, new Color(0.2f, 0.2f, 0.25f, 0.8f));

            // Fill Area & Fill
            GameObject fillArea = CreateUIEmpty("Fill Area", sliderObj.transform);
            SetRectTransform(fillArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            GameObject fill = CreateUIEmpty("Fill", fillArea.transform);
            SetRectTransform(fill, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image fillImg = AddImage(fill, fillColor);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.value = 1f;

            // Label
            Text lbl = AddText(sliderObj, "Label", label, font, 14, Color.white, TextAnchor.MiddleLeft);
            SetRectTransform(lbl.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(0f, 20f));

            return slider;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
            bool found = false;
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].path == scenePath)
                {
                    found = true;
                    original[i].enabled = true;
                    break;
                }
            }

            if (!found)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[original.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
                for (int i = 0; i < original.Length; i++)
                {
                    newScenes[i + 1] = original[i];
                }
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[SceneAutoBuilder] Added {scenePath} to Build Settings at index 0.");
            }
        }
    }
}
