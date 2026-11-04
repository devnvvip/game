using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        private const string SceneVersion = "StreamerIdol_v3.0.0_UltimateRealisticApartment";
        private const string ScenePrefKey = "StreamerIdol_BuiltVersion";
        private const string ScenePath = "Assets/Scenes/MainStreamerScene.unity";
        private const string MaterialsDir = "Assets/Materials";

        static SceneAutoBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            string builtVer = EditorPrefs.GetString(ScenePrefKey, "");
            if (builtVer != SceneVersion || !File.Exists(ScenePath))
            {
                Debug.Log($"[SceneAutoBuilder] Scene not found or version mismatch ({builtVer} != {SceneVersion}). Rebuilding realistic scene...");
                RebuildScene();
            }
        }

        [MenuItem("Game/Rebuild Scene")]
        public static void RebuildScene()
        {
            try
            {
                Debug.Log("[SceneAutoBuilder] Building Ultimate Realistic Idol Streamer Apartment Scene...");

                if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
                if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

                // 1. Generate All PBR Textures, Posters, and UI Sprites
                PBRTextureGenerator.GenerateAllTextures();

                // 2. Create clean single scene
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // 3. Setup Lighting, Environment & Mobile-Safe Post Processing
                SetupEnvironmentAndPostProcessing();

                // 4. Build Apartment Architecture (Floors, Walls, Baseboards, Crown Moldings, Windows, Curtains, Doors)
                GameObject apartmentRoot = new GameObject("Apartment_Architecture");
                BuildApartmentArchitecture(apartmentRoot);

                // 5. Build Detailed Realistic Furniture & Props
                GameObject propsRoot = new GameObject("Apartment_InteriorFurniture");
                Transform sitPoint;
                BuildFurnitureAndProps(propsRoot, out sitPoint);

                // 6. Setup Lighting Rig, Studio Ring Light, Probes
                Light sunLight, ceilingLight, ringLight, bedLamp;
                SetupLightingAndProbes(apartmentRoot, out sunLight, out ceilingLight, out ringLight, out bedLamp);

                // 7. Atmospheric Sunbeam Dust Motes (Fixed URP Particles Material)
                CreateAtmosphericDustMotes(apartmentRoot);

                // 8. Build Player with Enhanced Touch Look & Character Controller
                GameObject playerObj = BuildPlayer(sitPoint);

                // 9. Build Realistic iPhone UI, HUD, and Pause Menu with Audio Controls
                GameObject canvasObj = BuildUICanvas();

                // 10. Build Core Game Managers
                BuildManagers(sunLight, ceilingLight, ringLight, bedLamp, canvasObj);

                // 11. Save all persistent asset files
                AssetDatabase.SaveAssets();

                // 12. Save Scene
                bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"[SceneAutoBuilder] Saved realistic scene to {ScenePath}: {saved}");

                // 13. Ensure Scene is registered in Build Settings
                AddSceneToBuildSettings(ScenePath);

                EditorPrefs.SetString(ScenePrefKey, SceneVersion);
                Debug.Log($"[SceneAutoBuilder] Realistic Idol Apartment build complete! Version: {SceneVersion}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneAutoBuilder] Error during scene build: {ex}");
            }
        }

        private static void SetupEnvironmentAndPostProcessing()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.74f);

            // Post-Processing Volume with mobile-safe parameters (ACES, subtle Bloom & Vignette)
            GameObject volumeObj = new GameObject("Global_PostProcessingVolume");
            Volume vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;

            VolumeProfile runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "Ultimate_Mobile_PostProcessProfile";

            // ACES Tonemapping for cinema-grade lighting
            Tonemapping tonemap = runtimeProfile.Add<Tonemapping>(true);
            tonemap.mode.value = TonemappingMode.ACES;

            // Subtle Bloom for glowing screens and neon lights
            Bloom bloom = runtimeProfile.Add<Bloom>(true);
            bloom.threshold.value = 0.95f;
            bloom.intensity.value = 0.35f;

            // Very gentle Vignette
            Vignette vig = runtimeProfile.Add<Vignette>(true);
            vig.intensity.value = 0.16f;
            vig.smoothness.value = 0.30f;

            // Warm natural color adjustments
            ColorAdjustments colorAdj = runtimeProfile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.value = 0.15f;
            colorAdj.contrast.value = 8f;
            colorAdj.saturation.value = 6f;

            vol.sharedProfile = runtimeProfile;
            volumeObj.SetActive(true);
        }

        private static Shader GetLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        private static Material CreatePBRMaterial(string name, Color color, string albedoPath = "", string normalPath = "", float smoothness = 0.5f, float metallic = 0.05f, Vector2? tiling = null)
        {
            string matPath = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(GetLitShader());
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = GetLitShader();
            }

            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (!string.IsNullOrEmpty(albedoPath))
            {
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    mat.mainTexture = albedo;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
                    mat.SetColor("_BaseColor", color);

                    if (tiling.HasValue)
                    {
                        mat.mainTextureScale = tiling.Value;
                        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling.Value);
                    }
                }
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetFloat("_BumpScale", 1.0f);

                    if (tiling.HasValue && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTextureScale("_BumpMap", tiling.Value);
                    }
                }
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissivePBRMaterial(string name, Color baseColor, Color emissionColor, string texturePath = "", Vector2? tiling = null)
        {
            Material mat = CreatePBRMaterial(name, baseColor, texturePath, "", 0.85f, 0.1f, tiling);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (tex != null && mat.HasProperty("_EmissionMap"))
                {
                    mat.SetTexture("_EmissionMap", tex);
                    if (tiling.HasValue) mat.SetTextureScale("_EmissionMap", tiling.Value);
                }
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateParticleMaterial(string name, Color color, string texturePath)
        {
            string matPath = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                }
            }

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha blend
            mat.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetupLightingAndProbes(GameObject parent, out Light sun, out Light ceiling, out Light ring, out Light bed)
        {
            GameObject lightsObj = new GameObject("Lighting_And_Probes");
            lightsObj.transform.SetParent(parent.transform);

            // Sun Directional Light (Warm morning/midday sunlight)
            GameObject sunObj = new GameObject("SunLight_Directional");
            sunObj.transform.SetParent(lightsObj.transform);
            sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.86f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowNormalBias = 0.4f;
            sunObj.transform.rotation = Quaternion.Euler(34f, 52f, 0f);

            // Ceiling Recessed Room Light (Warm natural ambient)
            GameObject ceilObj = new GameObject("Ceiling_RoomLight");
            ceilObj.transform.SetParent(lightsObj.transform);
            ceilObj.transform.position = new Vector3(0f, 2.95f, 0f);
            ceiling = ceilObj.AddComponent<Light>();
            ceiling.type = LightType.Point;
            ceiling.color = new Color(0.98f, 0.96f, 0.92f);
            ceiling.intensity = 0.85f;
            ceiling.range = 16f;
            ceiling.shadows = LightShadows.Soft;

            // Streamer Studio Ring Light (Spotlight + Emissive Ring)
            GameObject ringObj = new GameObject("Studio_RingSpotLight");
            ringObj.transform.SetParent(lightsObj.transform);
            ringObj.transform.position = new Vector3(3.2f, 1.45f, 2.3f);
            ringObj.transform.rotation = Quaternion.Euler(15f, 180f, 0f);
            ring = ringObj.AddComponent<Light>();
            ring.type = LightType.Spot;
            ring.spotAngle = 76f;
            ring.color = new Color(1f, 0.96f, 0.92f);
            ring.intensity = 1.9f;
            ring.range = 7f;
            ring.shadows = LightShadows.Soft;
            ring.enabled = false;

            // Bedside Warm Lamp
            GameObject bedObj = new GameObject("Bedside_LampLight");
            bedObj.transform.SetParent(lightsObj.transform);
            bedObj.transform.position = new Vector3(-3.8f, 0.9f, -1.8f);
            bed = bedObj.AddComponent<Light>();
            bed.type = LightType.Point;
            bed.color = new Color(1f, 0.82f, 0.58f);
            bed.intensity = 0.65f;
            bed.range = 5.5f;
            bed.shadows = LightShadows.Soft;

            // Studio Reflection Probe (Box Projection for accurate floor & marble reflections)
            GameObject probeObj = new GameObject("Studio_ReflectionProbe");
            probeObj.transform.SetParent(lightsObj.transform);
            probeObj.transform.position = new Vector3(1.5f, 1.5f, 1.0f);
            ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(16f, 6f, 12f);
            probe.center = Vector3.zero;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.hdr = false;

            // Light Probe Group for smooth dynamic character lighting
            GameObject lpgObj = new GameObject("Apartment_LightProbeGroup");
            lpgObj.transform.SetParent(lightsObj.transform);
            LightProbeGroup lpg = lpgObj.AddComponent<LightProbeGroup>();
            Vector3[] probePositions = new Vector3[18];
            int idx = 0;
            float[] xPos = new float[] { -4.5f, 0f, 4.0f };
            float[] zPos = new float[] { -3.0f, 0f, 3.5f };
            float[] yPos = new float[] { 0.8f, 2.0f };
            for (int y = 0; y < yPos.Length; y++)
            {
                for (int x = 0; x < xPos.Length; x++)
                {
                    for (int z = 0; z < zPos.Length; z++)
                    {
                        probePositions[idx++] = new Vector3(xPos[x], yPos[y], zPos[z]);
                    }
                }
            }
            lpg.probePositions = probePositions;
        }

        private static void CreateAtmosphericDustMotes(GameObject root)
        {
            GameObject dustObj = new GameObject("Atmospheric_DustMotes");
            dustObj.transform.SetParent(root.transform);
            dustObj.transform.position = new Vector3(1.5f, 1.5f, 1.5f);

            ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 7f;
            main.startSpeed = 0.05f;
            main.startSize = 0.022f;
            main.startColor = new Color(1f, 0.98f, 0.88f, 0.16f); // Soft golden sunlit dust
            main.maxParticles = 35; // Kept low for mobile performance

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 2.5f, 8f);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.008f, 0.015f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);

            // Assign dedicated URP Particle Unlit Material
            Material dustMat = CreateParticleMaterial("Mat_DustParticle", new Color(1f, 0.98f, 0.88f, 0.20f), "Assets/Textures/Particle_SoftDot.png");
            ParticleSystemRenderer psr = dustObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = dustMat;
            }
        }

        private static void BuildApartmentArchitecture(GameObject root)
        {
            Material floorMat = CreatePBRMaterial("Mat_WoodFloor_PBR", Color.white, "Assets/Textures/WoodParquet_Albedo.png", "Assets/Textures/WoodParquet_Normal.png", 0.65f, 0.04f, new Vector2(4f, 4f));
            Material wallMat = CreatePBRMaterial("Mat_ModernWall_PBR", Color.white, "Assets/Textures/ModernPlaster_Albedo.png", "Assets/Textures/ModernPlaster_Normal.png", 0.25f, 0.01f, new Vector2(4f, 3f));
            Material ceilingMat = CreatePBRMaterial("Mat_Ceiling_PBR", Color.white, "Assets/Textures/ModernPlaster_Albedo.png", "Assets/Textures/ModernPlaster_Normal.png", 0.15f, 0f, new Vector2(4f, 3f));
            Material studioAccentMat = CreatePBRMaterial("Mat_StudioAcoustic_PBR", Color.white, "Assets/Textures/AcousticWall_Albedo.png", "Assets/Textures/AcousticWall_Normal.png", 0.35f, 0.08f, new Vector2(2f, 2f));
            Material glassMat = CreatePBRMaterial("Mat_WindowGlass", new Color(0.72f, 0.88f, 0.98f, 0.25f), "", "", 0.95f, 0.1f);
            Material baseboardMat = CreatePBRMaterial("Mat_Baseboard", new Color(0.18f, 0.18f, 0.22f), "", "", 0.45f, 0.05f);
            Material frameMat = CreatePBRMaterial("Mat_WindowFrame", new Color(0.14f, 0.14f, 0.16f), "", "", 0.60f, 0.25f);
            Material curtainMat = CreatePBRMaterial("Mat_CurtainFabric", new Color(0.86f, 0.84f, 0.92f), "Assets/Textures/FabricLinen_Albedo.png", "Assets/Textures/FabricLinen_Normal.png", 0.2f, 0f, new Vector2(2f, 4f));
            Material doorMat = CreatePBRMaterial("Mat_DoorWood", new Color(0.38f, 0.26f, 0.16f), "Assets/Textures/WoodParquet_Albedo.png", "Assets/Textures/WoodParquet_Normal.png", 0.45f, 0.05f);
            Material metalMat = CreatePBRMaterial("Mat_ChromeMetal", new Color(0.88f, 0.88f, 0.92f), "", "", 0.92f, 0.95f);

            // Floor (14m x 10m)
            CreateBox("Floor", root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(14f, 0.1f, 10f), floorMat);

            // Ceiling (14m x 10m)
            CreateBox("Ceiling", root.transform, new Vector3(0f, 3.05f, 0f), new Vector3(14f, 0.1f, 10f), ceilingMat);

            // Outer Perimeter Walls
            CreateBox("Wall_North", root.transform, new Vector3(0f, 1.5f, 5f), new Vector3(14f, 3f, 0.2f), wallMat);
            CreateBox("Wall_South", root.transform, new Vector3(0f, 1.5f, -5f), new Vector3(14f, 3f, 0.2f), wallMat);
            CreateBox("Wall_West", root.transform, new Vector3(-7f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallMat);
            CreateBox("Wall_East", root.transform, new Vector3(7f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallMat);

            // Interior Partition: Separates Bedroom (West) from Living/Studio/Kitchen (East)
            CreateBox("Partition_BedroomWall", root.transform, new Vector3(-2.2f, 1.5f, -1.5f), new Vector3(0.2f, 3f, 7f), wallMat);

            // Architectural Baseboards (Chân tường viền quanh các phòng)
            CreateBox("Baseboard_North", root.transform, new Vector3(0f, 0.06f, 4.89f), new Vector3(13.8f, 0.12f, 0.02f), baseboardMat);
            CreateBox("Baseboard_South", root.transform, new Vector3(0f, 0.06f, -4.89f), new Vector3(13.8f, 0.12f, 0.02f), baseboardMat);
            CreateBox("Baseboard_West", root.transform, new Vector3(-6.89f, 0.06f, 0f), new Vector3(0.02f, 0.12f, 9.8f), baseboardMat);
            CreateBox("Baseboard_East", root.transform, new Vector3(6.89f, 0.06f, 0f), new Vector3(0.02f, 0.12f, 9.8f), baseboardMat);
            CreateBox("Baseboard_PartitionE", root.transform, new Vector3(-2.09f, 0.06f, -1.5f), new Vector3(0.02f, 0.12f, 6.8f), baseboardMat);
            CreateBox("Baseboard_PartitionW", root.transform, new Vector3(-2.31f, 0.06f, -1.5f), new Vector3(0.02f, 0.12f, 6.8f), baseboardMat);

            // Architectural Crown Moldings (Phào trần viền mép trần)
            CreateBox("Molding_North", root.transform, new Vector3(0f, 2.96f, 4.89f), new Vector3(13.8f, 0.08f, 0.03f), baseboardMat);
            CreateBox("Molding_South", root.transform, new Vector3(0f, 2.96f, -4.89f), new Vector3(13.8f, 0.08f, 0.03f), baseboardMat);
            CreateBox("Molding_West", root.transform, new Vector3(-6.89f, 2.96f, 0f), new Vector3(0.03f, 0.08f, 9.8f), baseboardMat);
            CreateBox("Molding_East", root.transform, new Vector3(6.89f, 2.96f, 0f), new Vector3(0.03f, 0.08f, 9.8f), baseboardMat);

            // Studio Back Wall Accent (behind streamer desk)
            CreateBox("Studio_AcousticWall", root.transform, new Vector3(3.2f, 1.5f, 4.88f), new Vector3(5.5f, 2.8f, 0.08f), studioAccentMat);

            // Studio Ambient Neon Strips (Magenta & Cyan)
            Material neonPink = CreateEmissivePBRMaterial("Mat_NeonStrip_Pink", Color.magenta, new Color(1f, 0.18f, 0.70f) * 3f);
            CreateBox("Studio_NeonTopStrip", root.transform, new Vector3(3.2f, 2.92f, 4.84f), new Vector3(5.5f, 0.04f, 0.04f), neonPink);

            // Bedroom Window with frames and curtains
            GameObject bedWin = new GameObject("Bedroom_WindowGroup");
            bedWin.transform.SetParent(root.transform);
            CreateBox("Window_Bedroom_Glass", bedWin.transform, new Vector3(-6.98f, 1.6f, 2f), new Vector3(0.04f, 1.8f, 2.4f), glassMat);
            CreateBox("Window_Bedroom_FrameTop", bedWin.transform, new Vector3(-6.95f, 2.52f, 2f), new Vector3(0.08f, 0.06f, 2.5f), frameMat);
            CreateBox("Window_Bedroom_FrameBottom", bedWin.transform, new Vector3(-6.95f, 0.68f, 2f), new Vector3(0.08f, 0.06f, 2.5f), frameMat);
            CreateBox("Window_Bedroom_CurtainL", bedWin.transform, new Vector3(-6.9f, 1.55f, 0.65f), new Vector3(0.12f, 2.1f, 0.35f), curtainMat);
            CreateBox("Window_Bedroom_CurtainR", bedWin.transform, new Vector3(-6.9f, 1.55f, 3.35f), new Vector3(0.12f, 2.1f, 0.35f), curtainMat);

            // Studio Panoramic Window with frames and sheer drapes
            GameObject studioWin = new GameObject("Studio_WindowGroup");
            studioWin.transform.SetParent(root.transform);
            CreateBox("Window_Studio_Glass", studioWin.transform, new Vector3(6.98f, 1.6f, 1.5f), new Vector3(0.04f, 2.0f, 4.2f), glassMat);
            CreateBox("Window_Studio_FrameTop", studioWin.transform, new Vector3(6.95f, 2.62f, 1.5f), new Vector3(0.08f, 0.06f, 4.3f), frameMat);
            CreateBox("Window_Studio_FrameBottom", studioWin.transform, new Vector3(6.95f, 0.58f, 1.5f), new Vector3(0.08f, 0.06f, 4.3f), frameMat);
            CreateBox("Window_Studio_CurtainL", studioWin.transform, new Vector3(6.9f, 1.6f, -0.75f), new Vector3(0.12f, 2.2f, 0.45f), curtainMat);
            CreateBox("Window_Studio_CurtainR", studioWin.transform, new Vector3(6.9f, 1.6f, 3.75f), new Vector3(0.12f, 2.2f, 0.45f), curtainMat);

            // Interior Bedroom Door with Doorframe and Handle
            GameObject doorObj = new GameObject("Bedroom_DoorGroup");
            doorObj.transform.SetParent(root.transform);
            doorObj.transform.position = new Vector3(-2.2f, 1.05f, 2.4f);
            CreateBox("Door_Leaf", doorObj.transform, Vector3.zero, new Vector3(0.06f, 2.1f, 0.9f), doorMat);
            CreateBox("Door_Handle", doorObj.transform, new Vector3(0.06f, 0f, -0.35f), new Vector3(0.08f, 0.03f, 0.14f), metalMat);
        }

        private static void BuildFurnitureAndProps(GameObject root, out Transform sitPoint)
        {
            Material deskMat = CreatePBRMaterial("Mat_DeskBlack", new Color(0.12f, 0.12f, 0.14f), "", "", 0.65f, 0.1f);
            Material metalMat = CreatePBRMaterial("Mat_ChromeMetal", new Color(0.85f, 0.85f, 0.88f), "", "", 0.90f, 0.92f);
            Material bedWoodMat = CreatePBRMaterial("Mat_BedWood", new Color(0.42f, 0.28f, 0.18f), "Assets/Textures/WoodParquet_Albedo.png", "Assets/Textures/WoodParquet_Normal.png", 0.45f, 0.05f);
            Material bedClothMat = CreatePBRMaterial("Mat_BedSheet_PBR", Color.white, "Assets/Textures/FabricLinen_Albedo.png", "Assets/Textures/FabricLinen_Normal.png", 0.2f, 0f, new Vector2(3f, 3f));
            Material pillowMat = CreatePBRMaterial("Mat_PillowSilk", new Color(0.92f, 0.94f, 0.98f), "", "", 0.4f, 0f);
            Material chairLeatherMat = CreatePBRMaterial("Mat_GamingChair_Leather", new Color(0.12f, 0.12f, 0.15f), "Assets/Textures/Leather_Albedo.png", "Assets/Textures/Leather_Normal.png", 0.55f, 0.05f);
            Material chairCyanMat = CreateEmissivePBRMaterial("Mat_GamingChair_CyanGlow", new Color(0f, 0.8f, 0.9f), new Color(0f, 0.8f, 0.9f) * 1.5f);
            Material marbleMat = CreatePBRMaterial("Mat_MarbleCounter_PBR", Color.white, "Assets/Textures/MarbleCounter_Albedo.png", "Assets/Textures/MarbleCounter_Normal.png", 0.85f, 0.05f);
            Material screenObsMat = CreateEmissivePBRMaterial("Mat_Screen_OBSPreview", Color.white, Color.white * 1.8f, "Assets/Textures/Monitor_CenterScreen.png");
            Material screenChatMat = CreateEmissivePBRMaterial("Mat_Screen_ChatAnalytics", Color.white, Color.white * 1.6f, "Assets/Textures/Monitor_LeftChat.png");
            Material screenMixerMat = CreateEmissivePBRMaterial("Mat_Screen_AudioMixer", Color.white, Color.white * 1.6f, "Assets/Textures/Monitor_RightMixer.png");
            Material wardrobeWoodMat = CreatePBRMaterial("Mat_WardrobeWhite", new Color(0.92f, 0.92f, 0.94f), "", "", 0.35f, 0.02f);
            Material plantPotMat = CreatePBRMaterial("Mat_PlantCeramic", new Color(0.96f, 0.96f, 0.98f), "", "", 0.85f, 0.05f);
            Material plantLeafMat = CreatePBRMaterial("Mat_PlantFoliage", new Color(0.15f, 0.52f, 0.22f), "", "", 0.35f, 0.02f);
            Material trophyGoldMat = CreatePBRMaterial("Mat_IdolGoldAward", new Color(0.95f, 0.80f, 0.25f), "", "", 0.92f, 0.95f);
            Material rugBedMat = CreatePBRMaterial("Mat_Rug_Bedroom", new Color(0.88f, 0.84f, 0.78f), "Assets/Textures/FabricLinen_Albedo.png", "Assets/Textures/FabricLinen_Normal.png", 0.15f, 0f, new Vector2(2f, 2f));
            Material rugStudioMat = CreatePBRMaterial("Mat_Rug_Studio", new Color(0.18f, 0.20f, 0.26f), "Assets/Textures/FabricLinen_Albedo.png", "Assets/Textures/FabricLinen_Normal.png", 0.25f, 0.05f, new Vector2(3f, 3f));
            Material posterConcertMat = CreatePBRMaterial("Mat_Poster_Concert", Color.white, "Assets/Textures/Poster_IdolConcert.png", "", 0.65f, 0.1f);
            Material posterAwardMat = CreatePBRMaterial("Mat_Poster_Award", Color.white, "Assets/Textures/Poster_GoldPlayAward.png", "", 0.75f, 0.2f);
            Material neonCyan = CreateEmissivePBRMaterial("Mat_NeonStrip_Cyan", Color.cyan, new Color(0f, 0.9f, 1f) * 2.5f);
            Material sofaMat = CreatePBRMaterial("Mat_SofaFabric", new Color(0.24f, 0.26f, 0.32f), "Assets/Textures/FabricLinen_Albedo.png", "Assets/Textures/FabricLinen_Normal.png", 0.25f, 0f, new Vector2(2f, 2f));

            // ==========================================
            // 1. STREAMING STUDIO SETUP
            // ==========================================
            GameObject studioObj = new GameObject("Studio_StreamingSetup");
            studioObj.transform.SetParent(root.transform);
            studioObj.transform.position = new Vector3(3.2f, 0f, 3.8f);

            // Studio Rug under desk & chair
            CreateBox("Studio_FloorRug", studioObj.transform, new Vector3(0f, 0.01f, -0.6f), new Vector3(3.6f, 0.015f, 2.6f), rugStudioMat);

            // Large Curved Gaming Desk
            GameObject deskObj = CreateBox("Desk_Main", studioObj.transform, new Vector3(0f, 0.38f, 0f), new Vector3(2.6f, 0.76f, 1.1f), deskMat);
            StreamingDesk streamingDesk = deskObj.AddComponent<StreamingDesk>();

            // Desk metallic legs (Z-shape design)
            CreateBox("Desk_LegLeft", deskObj.transform, new Vector3(-1.18f, -0.05f, 0f), new Vector3(0.08f, 0.66f, 0.85f), metalMat);
            CreateBox("Desk_LegRight", deskObj.transform, new Vector3(1.18f, -0.05f, 0f), new Vector3(0.08f, 0.66f, 0.85f), metalMat);

            // Neon LED strip mounted under back of desk
            CreateBox("Desk_UnderglowLED", deskObj.transform, new Vector3(0f, -0.34f, 0.5f), new Vector3(2.4f, 0.03f, 0.03f), neonCyan);

            // Center Primary 32" Curved Monitor (OBS Broadcast)
            GameObject centerMonObj = CreateBox("Monitor_Center_32in", studioObj.transform, new Vector3(0f, 1.15f, 0.25f), new Vector3(1.2f, 0.65f, 0.06f), deskMat);
            GameObject centerDisplay = CreateBox("Center_OBS_Screen", centerMonObj.transform, new Vector3(0f, 0f, -0.032f), new Vector3(1.15f, 0.60f, 0.01f), screenObsMat);
            CreateBox("Center_Stand", centerMonObj.transform, new Vector3(0f, -0.38f, 0.08f), new Vector3(0.08f, 0.35f, 0.08f), metalMat);

            // Left Secondary Vertical Monitor (Live Chat & Analytics)
            GameObject leftMonObj = CreateBox("Monitor_Left_Vertical", studioObj.transform, new Vector3(-0.95f, 1.18f, 0.15f), new Vector3(0.48f, 0.85f, 0.06f), deskMat);
            leftMonObj.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            CreateBox("Left_Chat_Screen", leftMonObj.transform, new Vector3(0f, 0f, -0.032f), new Vector3(0.44f, 0.80f, 0.01f), screenChatMat);

            // Right Secondary Monitor (Audio EQ & Media)
            GameObject rightMonObj = CreateBox("Monitor_Right_Mixer", studioObj.transform, new Vector3(0.95f, 1.12f, 0.15f), new Vector3(0.85f, 0.52f, 0.06f), deskMat);
            rightMonObj.transform.localRotation = Quaternion.Euler(0f, -22f, 0f);
            CreateBox("Right_Mixer_Screen", rightMonObj.transform, new Vector3(0f, 0f, -0.032f), new Vector3(0.80f, 0.48f, 0.01f), screenMixerMat);

            streamingDesk.monitorScreens = centerMonObj;
            streamingDesk.screenActiveMaterial = screenObsMat;

            // Desktop Gaming PC Tower (Glass side panel with internal glow)
            GameObject pcTower = CreateBox("PC_Tower_RGB", studioObj.transform, new Vector3(1.05f, 1.05f, -0.05f), new Vector3(0.24f, 0.52f, 0.50f), deskMat);
            CreateBox("PC_GlassPanel", pcTower.transform, new Vector3(-0.122f, 0f, 0f), new Vector3(0.01f, 0.46f, 0.44f), chairCyanMat);

            // Mechanical RGB Keyboard & Gaming Mouse
            CreateBox("Keyboard_RGB", studioObj.transform, new Vector3(-0.1f, 0.775f, -0.2f), new Vector3(0.48f, 0.025f, 0.16f), deskMat);
            CreateBox("MousePad_Stitched", studioObj.transform, new Vector3(0.25f, 0.765f, -0.2f), new Vector3(0.85f, 0.005f, 0.35f), deskMat);
            CreateBox("Gaming_Mouse", studioObj.transform, new Vector3(0.35f, 0.78f, -0.2f), new Vector3(0.08f, 0.035f, 0.12f), deskMat);

            // Streamer Studio Microphone on Boom Arm
            GameObject micArm = CreateBox("Mic_BoomArm", studioObj.transform, new Vector3(-0.55f, 1.05f, -0.1f), new Vector3(0.03f, 0.45f, 0.03f), metalMat);
            micArm.transform.localRotation = Quaternion.Euler(-18f, 35f, 0f);
            GameObject micBody = CreateBox("Condenser_Microphone", micArm.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.20f, 0.08f), metalMat);
            CreateBox("Mic_PopFilter", micBody.transform, new Vector3(0f, 0f, -0.1f), new Vector3(0.14f, 0.14f, 0.01f), deskMat);

            // Streamer Studio Ring Light (Tripod Stand + Ring Geometry)
            GameObject ringLightObj = new GameObject("Studio_RingLight_Prop");
            ringLightObj.transform.SetParent(studioObj.transform);
            ringLightObj.transform.localPosition = new Vector3(0f, 0f, -1.5f);

            CreateBox("RingLight_Tripod", ringLightObj.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.05f, 1.3f, 0.05f), metalMat);
            Material ringGlowMat = CreateEmissivePBRMaterial("Mat_RingLight_Glow", Color.white, new Color(1f, 0.95f, 0.85f) * 2.5f);
            GameObject ringMesh = CreateBox("RingLight_Head", ringLightObj.transform, new Vector3(0f, 1.45f, 0f), new Vector3(0.48f, 0.48f, 0.04f), ringGlowMat);
            StudioRingLight studioRingLightComp = ringLightObj.AddComponent<StudioRingLight>();
            studioRingLightComp.ringMeshRenderer = ringMesh.GetComponent<Renderer>();

            // Ergonomic Gaming Chair
            GameObject chairObj = new GameObject("GamingChair");
            chairObj.transform.SetParent(studioObj.transform);
            chairObj.transform.localPosition = new Vector3(0f, 0f, -0.7f);

            CreateBox("Chair_Seat", chairObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.60f, 0.12f, 0.58f), chairLeatherMat);
            CreateBox("Chair_Backrest", chairObj.transform, new Vector3(0f, 0.95f, 0.24f), new Vector3(0.56f, 0.95f, 0.10f), chairLeatherMat);
            CreateBox("Chair_HeadCushion", chairObj.transform, new Vector3(0f, 1.35f, 0.20f), new Vector3(0.32f, 0.16f, 0.08f), chairCyanMat);
            CreateBox("Chair_ArmrestL", chairObj.transform, new Vector3(-0.32f, 0.68f, 0f), new Vector3(0.08f, 0.35f, 0.30f), chairLeatherMat);
            CreateBox("Chair_ArmrestR", chairObj.transform, new Vector3(0.32f, 0.68f, 0f), new Vector3(0.08f, 0.35f, 0.30f), chairLeatherMat);
            CreateBox("Chair_BaseStand", chairObj.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.40f, 0.08f), metalMat);

            // Sitting Point for Player
            GameObject sitPointObj = new GameObject("StreamerSitPoint");
            sitPointObj.transform.SetParent(chairObj.transform);
            sitPointObj.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            sitPointObj.transform.localRotation = Quaternion.identity;
            sitPoint = sitPointObj.transform;
            streamingDesk.chairSitPoint = sitPoint;

            // ==========================================
            // 2. IDOL SHOWCASE, POSTERS, & LOUNGE
            // ==========================================
            // Framed Posters on Acoustic Wall
            CreateBox("Poster_ConcertTour", studioObj.transform, new Vector3(-1.8f, 1.8f, 1.02f), new Vector3(0.9f, 1.3f, 0.02f), posterConcertMat);
            CreateBox("Poster_MilestoneAward", studioObj.transform, new Vector3(1.8f, 1.8f, 1.02f), new Vector3(0.8f, 0.8f, 0.02f), posterAwardMat);

            // Display Shelf Unit against Partition Wall
            GameObject shelfObj = new GameObject("Idol_DisplayBookshelf");
            shelfObj.transform.SetParent(root.transform);
            shelfObj.transform.position = new Vector3(-2.05f, 0f, 1.0f);
            CreateBox("Shelf_Frame", shelfObj.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.40f, 2.2f, 1.6f), deskMat);
            CreateBox("Shelf_TrophyGold", shelfObj.transform, new Vector3(0.05f, 1.5f, 0f), new Vector3(0.18f, 0.28f, 0.18f), trophyGoldMat);
            CreateBox("Shelf_BooksBatch1", shelfObj.transform, new Vector3(0.05f, 0.95f, -0.4f), new Vector3(0.24f, 0.22f, 0.45f), chairCyanMat);
            CreateBox("Shelf_BooksBatch2", shelfObj.transform, new Vector3(0.05f, 0.5f, 0.3f), new Vector3(0.24f, 0.22f, 0.50f), pillowMat);

            // Lounge Sofa & Coffee Table Area
            GameObject loungeObj = new GameObject("Lounge_SofaArea");
            loungeObj.transform.SetParent(root.transform);
            loungeObj.transform.position = new Vector3(4.5f, 0f, -1.8f);

            CreateBox("Sofa_SeatBase", loungeObj.transform, new Vector3(0f, 0.28f, 0f), new Vector3(2.2f, 0.38f, 0.95f), sofaMat);
            CreateBox("Sofa_Backrest", loungeObj.transform, new Vector3(0f, 0.65f, 0.40f), new Vector3(2.2f, 0.55f, 0.22f), sofaMat);
            CreateBox("Sofa_CushionL", loungeObj.transform, new Vector3(-0.65f, 0.65f, 0.25f), new Vector3(0.42f, 0.35f, 0.12f), pillowMat);
            CreateBox("Sofa_CushionR", loungeObj.transform, new Vector3(0.65f, 0.65f, 0.25f), new Vector3(0.42f, 0.35f, 0.12f), chairCyanMat);
            CreateBox("Coffee_Table", loungeObj.transform, new Vector3(0f, 0.24f, -0.95f), new Vector3(1.1f, 0.36f, 0.65f), marbleMat);

            // Wall-mounted 55" OLED TV & Soundbar
            CreateBox("LivingRoom_TV", root.transform, new Vector3(1.2f, 1.6f, -4.88f), new Vector3(1.8f, 1.05f, 0.05f), deskMat);
            CreateBox("TV_Soundbar", root.transform, new Vector3(1.2f, 1.0f, -4.88f), new Vector3(1.2f, 0.10f, 0.08f), metalMat);

            // Tall Potted Plant in Corner
            GameObject plantObj = new GameObject("PottedPlant_FiddleLeaf");
            plantObj.transform.SetParent(root.transform);
            plantObj.transform.position = new Vector3(6.2f, 0f, 4.2f);
            CreateBox("Plant_Pot", plantObj.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.45f, 0.70f, 0.45f), plantPotMat);
            CreateBox("Plant_Soil", plantObj.transform, new Vector3(0f, 0.68f, 0f), new Vector3(0.42f, 0.05f, 0.42f), deskMat);
            CreateBox("Plant_Stalk1", plantObj.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.06f, 0.95f, 0.06f), plantLeafMat);
            CreateBox("Plant_LeafClusterA", plantObj.transform, new Vector3(0.1f, 1.55f, 0.1f), new Vector3(0.45f, 0.35f, 0.45f), plantLeafMat);
            CreateBox("Plant_LeafClusterB", plantObj.transform, new Vector3(-0.08f, 1.3f, -0.08f), new Vector3(0.40f, 0.30f, 0.40f), plantLeafMat);

            // Wall Air Conditioner Unit (Split AC)
            GameObject acObj = CreateBox("AirConditioner_Wall", root.transform, new Vector3(4.5f, 2.65f, 4.88f), new Vector3(1.1f, 0.32f, 0.22f), wardrobeWoodMat);
            CreateBox("AC_DisplayLED", acObj.transform, new Vector3(0.35f, -0.05f, -0.112f), new Vector3(0.10f, 0.04f, 0.01f), neonCyan);

            // ==========================================
            // 3. BEDROOM SETUP
            // ==========================================
            GameObject bedSetupObj = new GameObject("Bedroom_Setup");
            bedSetupObj.transform.SetParent(root.transform);
            bedSetupObj.transform.position = new Vector3(-4.5f, 0f, -2.5f);

            // Cozy Bed Rug
            CreateBox("Bedroom_Rug", bedSetupObj.transform, new Vector3(0f, 0.01f, 0f), new Vector3(2.8f, 0.015f, 3.2f), rugBedMat);

            // Bed Frame & Mattress
            GameObject bedObj = CreateBox("Bed_Main", bedSetupObj.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.8f, 0.5f, 2.3f), bedWoodMat);
            CreateBox("Bed_Headboard", bedSetupObj.transform, new Vector3(0f, 0.85f, -1.15f), new Vector3(1.9f, 1.2f, 0.15f), bedWoodMat);
            CreateBox("Bed_Mattress", bedSetupObj.transform, new Vector3(0f, 0.48f, 0.05f), new Vector3(1.7f, 0.35f, 2.1f), bedClothMat);
            CreateBox("Bed_Duvet", bedSetupObj.transform, new Vector3(0f, 0.58f, 0.35f), new Vector3(1.74f, 0.18f, 1.4f), bedClothMat);

            // Pillows
            CreateBox("Pillow_Left", bedSetupObj.transform, new Vector3(-0.45f, 0.65f, -0.75f), new Vector3(0.65f, 0.15f, 0.42f), pillowMat);
            CreateBox("Pillow_Right", bedSetupObj.transform, new Vector3(0.45f, 0.65f, -0.75f), new Vector3(0.65f, 0.15f, 0.42f), pillowMat);
            bedObj.AddComponent<Bed>();

            // Nightstand & Alarm Clock
            GameObject nightstandObj = CreateBox("Bedside_Nightstand", bedSetupObj.transform, new Vector3(-1.35f, 0.32f, -0.85f), new Vector3(0.60f, 0.64f, 0.55f), bedWoodMat);
            GameObject alarmClockObj = CreateBox("Digital_AlarmClock", nightstandObj.transform, new Vector3(0f, 0.37f, 0f), new Vector3(0.24f, 0.12f, 0.14f), deskMat);
            CreateBox("Alarm_Display", alarmClockObj.transform, new Vector3(0f, 0f, 0.072f), new Vector3(0.20f, 0.08f, 0.01f), chairCyanMat);
            alarmClockObj.AddComponent<AlarmClock>();

            // Bedside Lamp
            CreateBox("Bedside_LampBase", nightstandObj.transform, new Vector3(0.15f, 0.45f, -0.1f), new Vector3(0.12f, 0.25f, 0.12f), metalMat);
            CreateBox("Bedside_LampShade", nightstandObj.transform, new Vector3(0.15f, 0.62f, -0.1f), new Vector3(0.22f, 0.18f, 0.22f), pillowMat);

            // Wardrobe (Closet)
            GameObject wardrobeObj = CreateBox("Wardrobe_IdolOutfits", root.transform, new Vector3(-6.2f, 1.25f, -1.0f), new Vector3(1.1f, 2.5f, 1.8f), wardrobeWoodMat);
            CreateBox("Wardrobe_HandleL", wardrobeObj.transform, new Vector3(0.56f, 0f, -0.15f), new Vector3(0.04f, 0.45f, 0.04f), metalMat);
            CreateBox("Wardrobe_HandleR", wardrobeObj.transform, new Vector3(0.56f, 0f, 0.15f), new Vector3(0.04f, 0.45f, 0.04f), metalMat);
            wardrobeObj.AddComponent<Wardrobe>();

            // ==========================================
            // 4. KITCHENETTE & BAR
            // ==========================================
            GameObject kitchenObj = new GameObject("Kitchen_Setup");
            kitchenObj.transform.SetParent(root.transform);
            kitchenObj.transform.position = new Vector3(0f, 0f, -3.8f);

            CreateBox("Kitchen_Counter", kitchenObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(3.2f, 0.9f, 0.8f), marbleMat);

            GameObject coffeeMachineObj = CreateBox("Espresso_CoffeeMachine", kitchenObj.transform, new Vector3(-0.8f, 1.08f, 0f), new Vector3(0.35f, 0.35f, 0.35f), metalMat);
            CreateBox("Coffee_Cup", coffeeMachineObj.transform, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.1f, 0.12f, 0.1f), pillowMat);
            coffeeMachineObj.AddComponent<CoffeeMachine>();

            GameObject fridgeObj = CreateBox("Refrigerator_Smart", kitchenObj.transform, new Vector3(1.2f, 1.0f, 0f), new Vector3(0.85f, 2.0f, 0.75f), deskMat);
            CreateBox("Fridge_Handle", fridgeObj.transform, new Vector3(-0.35f, 0f, 0.4f), new Vector3(0.05f, 0.6f, 0.05f), metalMat);
            fridgeObj.AddComponent<Refrigerator>();

            // ==========================================
            // 5. BATHROOM SETUP
            // ==========================================
            GameObject bathroomObj = new GameObject("Bathroom_Setup");
            bathroomObj.transform.SetParent(root.transform);
            bathroomObj.transform.position = new Vector3(-5.5f, 0f, 3.8f);

            GameObject sinkObj = CreateBox("Vanity_Sink", bathroomObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.88f, 0.65f), marbleMat);
            CreateBox("Chrome_Faucet", sinkObj.transform, new Vector3(0f, 0.55f, -0.15f), new Vector3(0.06f, 0.22f, 0.15f), metalMat);
            CreateBox("Vanity_Mirror", sinkObj.transform, new Vector3(0f, 1.15f, 0.3f), new Vector3(1.0f, 0.8f, 0.04f), CreatePBRMaterial("Mat_BathMirror", new Color(0.95f, 0.98f, 1f), "", "", 0.98f, 0.95f));
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
            player.transform.position = new Vector3(-3.2f, 1.0f, -2.0f);
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

            // Deep atmospheric twilight blue clear color
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.22f, 1f);

            UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true; // Enabled with mobile-safe profile
            camData.renderShadows = true;

            camObj.AddComponent<AudioListener>();

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

            // SAFE AREA CONTAINER
            GameObject safeAreaObj = CreateUIEmpty("SafeAreaContainer", canvasObj.transform);
            SetRectTransform(safeAreaObj, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            safeAreaObj.AddComponent<SafeAreaHelper>();

            // Sprites
            Sprite roundedBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UIRoundedBox.png");
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UICircle.png");
            Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UIRing.png");
            Sprite phoneWallpaper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Phone_Wallpaper.png");

            Sprite iconMsg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Messages.png");
            Sprite iconSoc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Social.png");
            Sprite iconCam = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Camera.png");
            Sprite iconSch = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Schedule.png");
            Sprite iconPho = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Photos.png");
            Sprite iconNot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/AppIcon_Notes.png");

            // =========================================================================
            // TOP STATUS BAR (Modern Frosted Glass, Zero Overlap)
            // =========================================================================
            GameObject topBar = CreateUIEmpty("TopStatusBar", safeAreaObj.transform);
            SetRectTransform(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-40f, 62f));
            AddImage(topBar, new Color(0.06f, 0.04f, 0.12f, 0.90f), roundedBoxSprite);

            // Left Section: Pause Button + Phase Badge
            Button pauseBtn = CreateSimpleButton("MobilePauseBtn", topBar.transform, new Vector2(40f, 0f), new Vector2(50f, 44f), "||", defaultFont, new Color(0.35f, 0.25f, 0.55f, 0.9f), roundedBoxSprite);
            SetRectTransform(pauseBtn.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(50f, 44f));

            GameObject phasePill = CreateUIEmpty("PhasePill", topBar.transform);
            SetRectTransform(phasePill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(75f, 0f), new Vector2(300f, 44f));
            AddImage(phasePill, new Color(0.12f, 0.10f, 0.25f, 0.85f), roundedBoxSprite);

            Text phaseText = AddText(phasePill, "PhaseText", " Buổi Sáng (07:00 - 11:30)", defaultFont, 18, Color.cyan, TextAnchor.MiddleLeft);
            SetRectTransform(phaseText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, 0f), new Vector2(-20f, 0f));

            // Center Section: Energy Bar & Mood Bar
            Slider energySlider = CreateStyledSlider("EnergySlider", topBar.transform, new Vector2(400f, 0f), "⚡ ", defaultFont, new Color(1f, 0.85f, 0.2f), roundedBoxSprite);
            Slider moodSlider = CreateStyledSlider("MoodSlider", topBar.transform, new Vector2(620f, 0f), "💖 ", defaultFont, new Color(1f, 0.35f, 0.75f), roundedBoxSprite);

            // Right Section: Followers Pill + Revenue Pill + Phone Button
            GameObject followersPill = CreateUIEmpty("FollowersPill", topBar.transform);
            SetRectTransform(followersPill, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-280f, 0f), new Vector2(150f, 40f));
            AddImage(followersPill, new Color(0.14f, 0.08f, 0.22f, 0.85f), roundedBoxSprite);
            Text followersText = AddText(followersPill, "FollowersText", "♥ 128,400", defaultFont, 18, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(followersText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject revenuePill = CreateUIEmpty("RevenuePill", topBar.transform);
            SetRectTransform(revenuePill, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-115f, 0f), new Vector2(155f, 40f));
            AddImage(revenuePill, new Color(0.08f, 0.18f, 0.12f, 0.85f), roundedBoxSprite);
            Text revenueText = AddText(revenuePill, "RevenueText", "15,500,000 đ", defaultFont, 18, new Color(0.35f, 1f, 0.45f), TextAnchor.MiddleCenter);
            SetRectTransform(revenueText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button phoneBtn = CreateSimpleButton("MobilePhoneBtn", topBar.transform, new Vector2(-10f, 0f), new Vector2(95f, 44f), "📱 PHONE", defaultFont, new Color(0.12f, 0.65f, 0.45f, 0.95f), roundedBoxSprite);
            SetRectTransform(phoneBtn.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(95f, 44f));

            // =========================================================================
            // QUEST CARD (Left Side Floating Card)
            // =========================================================================
            GameObject questBox = CreateUIEmpty("QuestCard", safeAreaObj.transform);
            SetRectTransform(questBox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -85f), new Vector2(360f, 95f));
            AddImage(questBox, new Color(0.06f, 0.05f, 0.14f, 0.88f), roundedBoxSprite);

            GameObject questAccent = CreateUIEmpty("QuestAccent", questBox.transform);
            SetRectTransform(questAccent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f));
            AddImage(questAccent, new Color(1f, 0.8f, 0.2f, 1f));

            Text questTitle = AddText(questBox, "QuestTitle", "🎯 [NHIỆM VỤ] Tắt chuông báo thức", defaultFont, 17, Color.yellow, TextAnchor.UpperLeft);
            SetRectTransform(questTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15f, -12f), new Vector2(-30f, 26f));

            Text questDesc = AddText(questBox, "QuestDesc", "Thức dậy và nhấn nút tắt chuông báo thức bên cạnh giường.", defaultFont, 14, new Color(0.9f, 0.9f, 0.95f), TextAnchor.UpperLeft);
            SetRectTransform(questDesc.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(15f, 10f), new Vector2(-30f, -42f));

            // =========================================================================
            // RETICLE & INTERACTION PROMPT
            // =========================================================================
            GameObject reticle = CreateUIEmpty("ReticleDot", safeAreaObj.transform);
            SetRectTransform(reticle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            AddImage(reticle, Color.white, circleSprite);

            GameObject interactPromptBox = CreateUIEmpty("InteractionPromptBox", safeAreaObj.transform);
            SetRectTransform(interactPromptBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(360f, 42f));
            AddImage(interactPromptBox, new Color(0.05f, 0.05f, 0.10f, 0.88f), roundedBoxSprite);
            Text interactPromptText = AddText(interactPromptBox, "PromptText", "[E] Tương tác", defaultFont, 17, Color.yellow, TextAnchor.MiddleCenter);
            SetRectTransform(interactPromptText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // =========================================================================
            // NOTIFICATION TOAST
            // =========================================================================
            GameObject notifBox = CreateUIEmpty("NotificationToast", safeAreaObj.transform);
            SetRectTransform(notifBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(580f, 48f));
            AddImage(notifBox, new Color(0.08f, 0.28f, 0.18f, 0.95f), roundedBoxSprite);
            Text notifText = AddText(notifBox, "NotifText", "Thông báo", defaultFont, 16, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(notifText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // =========================================================================
            // LIVESTREAM OVERLAY UI
            // =========================================================================
            GameObject streamOverlay = CreateUIEmpty("LivestreamOverlay", safeAreaObj.transform);
            SetRectTransform(streamOverlay, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            StreamOverlayUI overlayUI = streamOverlay.AddComponent<StreamOverlayUI>();
            overlayUI.overlayRoot = streamOverlay;

            GameObject liveHeader = CreateUIEmpty("LiveHeader", streamOverlay.transform);
            SetRectTransform(liveHeader, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(-40f, 54f));
            AddImage(liveHeader, new Color(0.04f, 0.04f, 0.08f, 0.92f), roundedBoxSprite);

            Text liveBadge = AddText(liveHeader, "LiveBadge", "● TRỰC TIẾP", defaultFont, 18, Color.red, TextAnchor.MiddleLeft);
            SetRectTransform(liveBadge.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(150f, 36f));

            Text viewersTxt = AddText(liveHeader, "LiveViewers", "👁 3,500", defaultFont, 18, Color.white, TextAnchor.MiddleLeft);
            SetRectTransform(viewersTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(140f, 36f));
            overlayUI.viewersText = viewersTxt;

            Text likesTxt = AddText(liveHeader, "LiveLikes", "❤️ 1,200", defaultFont, 18, new Color(1f, 0.4f, 0.5f), TextAnchor.MiddleLeft);
            SetRectTransform(likesTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(320f, 0f), new Vector2(140f, 36f));
            overlayUI.likesText = likesTxt;

            Text durationTxt = AddText(liveHeader, "LiveDuration", "⏱ 00:00", defaultFont, 18, Color.yellow, TextAnchor.MiddleLeft);
            SetRectTransform(durationTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(470f, 0f), new Vector2(140f, 36f));
            overlayUI.durationText = durationTxt;

            Text earningsTxt = AddText(liveHeader, "LiveEarnings", "⭐ +0 đ", defaultFont, 18, Color.green, TextAnchor.MiddleRight);
            SetRectTransform(earningsTxt.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(180f, 36f));
            overlayUI.liveEarningsText = earningsTxt;

            Button endStreamBtn = CreateSimpleButton("EndStreamButton", liveHeader.transform, new Vector2(-15f, 0f), new Vector2(160f, 38f), "KẾT THÚC LIVE", defaultFont, new Color(0.85f, 0.2f, 0.2f, 0.95f), roundedBoxSprite);
            SetRectTransform(endStreamBtn.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-15f, 0f), new Vector2(160f, 38f));
            overlayUI.endStreamButton = endStreamBtn;

            // Live Chat Panel (Right Side)
            GameObject chatPanel = CreateUIEmpty("LiveChatBox", streamOverlay.transform);
            SetRectTransform(chatPanel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-20f, -40f), new Vector2(380f, -160f));
            AddImage(chatPanel, new Color(0.04f, 0.05f, 0.09f, 0.88f), roundedBoxSprite);

            Text[] chatLines = new Text[8];
            for (int i = 0; i < 8; i++)
            {
                Text line = AddText(chatPanel, $"ChatLine_{i}", $"Chat message {i}", defaultFont, 15, Color.white, TextAnchor.MiddleLeft);
                SetRectTransform(line.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(15f, 15f + i * 36f), new Vector2(-30f, 32f));
                chatLines[i] = line;
            }
            overlayUI.chatLineTexts = chatLines;

            // Question Modal
            GameObject qModal = CreateUIEmpty("InteractiveQuestionModal", streamOverlay.transform);
            SetRectTransform(qModal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 360f));
            AddImage(qModal, new Color(0.07f, 0.09f, 0.16f, 0.96f), roundedBoxSprite);
            overlayUI.questionModal = qModal;

            Text qTopic = AddText(qModal, "QuestionTopic", "Fan hỏi ý kiến của bạn", defaultFont, 20, Color.yellow, TextAnchor.MiddleCenter);
            SetRectTransform(qTopic.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(-40f, 60f));
            overlayUI.questionTopicText = qTopic;

            Button cBtn1 = CreateSimpleButton("Choice1", qModal.transform, new Vector2(0f, 30f), new Vector2(550f, 50f), "Câu trả lời A", defaultFont, new Color(0.18f, 0.35f, 0.65f), roundedBoxSprite);
            Button cBtn2 = CreateSimpleButton("Choice2", qModal.transform, new Vector2(0f, -30f), new Vector2(550f, 50f), "Câu trả lời B", defaultFont, new Color(0.18f, 0.35f, 0.65f), roundedBoxSprite);
            Button cBtn3 = CreateSimpleButton("Choice3", qModal.transform, new Vector2(0f, -90f), new Vector2(550f, 50f), "Câu trả lời C", defaultFont, new Color(0.18f, 0.35f, 0.65f), roundedBoxSprite);
            overlayUI.choiceBtn1 = cBtn1;
            overlayUI.choiceText1 = cBtn1.GetComponentInChildren<Text>();
            overlayUI.choiceBtn2 = cBtn2;
            overlayUI.choiceText2 = cBtn2.GetComponentInChildren<Text>();
            overlayUI.choiceBtn3 = cBtn3;
            overlayUI.choiceText3 = cBtn3.GetComponentInChildren<Text>();

            // Incident Alert Banner
            GameObject incBanner = CreateUIEmpty("IncidentAlertBanner", streamOverlay.transform);
            SetRectTransform(incBanner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(650f, 110f));
            AddImage(incBanner, new Color(0.5f, 0.1f, 0.1f, 0.95f), roundedBoxSprite);
            overlayUI.incidentBanner = incBanner;

            Text incTitle = AddText(incBanner, "IncTitle", "⚠️ SỰ CỐ LIVESTREAM!", defaultFont, 20, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(incTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 32f));
            overlayUI.incidentTitleText = incTitle;

            Text incDesc = AddText(incBanner, "IncDesc", "Mô tả sự cố", defaultFont, 15, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(incDesc.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-40f, 32f));
            overlayUI.incidentDescText = incDesc;

            Button incActionBtn = CreateSimpleButton("IncActionBtn", incBanner.transform, new Vector2(0f, -32f), new Vector2(280f, 38f), "Khắc phục ngay", defaultFont, Color.green, roundedBoxSprite);
            overlayUI.incidentActionButton = incActionBtn;
            overlayUI.incidentActionBtnText = incActionBtn.GetComponentInChildren<Text>();

            // =========================================================================
            // REALISTIC IPHONE SIMULATION UI (Titanium Rim, Bezel, Dynamic Island, Home Bar)
            // =========================================================================
            GameObject phoneRoot = CreateUIEmpty("SmartphoneModal", safeAreaObj.transform);
            SetRectTransform(phoneRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 720f));
            PhoneUIController phoneCtrl = phoneRoot.AddComponent<PhoneUIController>();
            phoneCtrl.phoneRoot = phoneRoot;

            // Outer Titanium Shell
            GameObject phoneFrame = CreateUIEmpty("PhoneFrame", phoneRoot.transform);
            SetRectTransform(phoneFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(phoneFrame, new Color(0.24f, 0.25f, 0.28f, 1f), roundedBoxSprite);
            phoneCtrl.phoneFrameRect = phoneFrame.GetComponent<RectTransform>();

            // Slim Black Bezel Screen Area
            GameObject phoneScreen = CreateUIEmpty("PhoneScreen", phoneFrame.transform);
            SetRectTransform(phoneScreen, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -12f));
            AddImage(phoneScreen, Color.black, roundedBoxSprite);

            // Wallpaper Image Layer
            GameObject phoneWall = CreateUIEmpty("PhoneWallpaper", phoneScreen.transform);
            SetRectTransform(phoneWall, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(phoneWall, Color.white, phoneWallpaper);

            // Status Bar (Time, Signal, Wifi, Battery)
            GameObject statusBar = CreateUIEmpty("PhoneStatusBar", phoneScreen.transform);
            SetRectTransform(statusBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 32f));

            Text phoneClock = AddText(statusBar, "PhoneClock", "09:41", defaultFont, 15, Color.white, TextAnchor.MiddleLeft);
            SetRectTransform(phoneClock.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(70f, 24f));
            phoneCtrl.phoneClockText = phoneClock;

            Text batteryText = AddText(statusBar, "BatteryText", "5G  98% 🔋", defaultFont, 14, Color.white, TextAnchor.MiddleRight);
            SetRectTransform(batteryText.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(110f, 24f));
            phoneCtrl.batteryPercentText = batteryText;

            // Dynamic Island Pill
            GameObject dynamicIsland = CreateUIEmpty("DynamicIsland", statusBar.transform);
            SetRectTransform(dynamicIsland, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 28f));
            AddImage(dynamicIsland, Color.black, roundedBoxSprite);

            // Camera dot on Dynamic Island
            GameObject lensDot = CreateUIEmpty("CameraLensDot", dynamicIsland.transform);
            SetRectTransform(lensDot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 0f), new Vector2(10f, 10f));
            AddImage(lensDot, new Color(0.12f, 0.16f, 0.28f, 1f), circleSprite);

            // -------------------------------------------------------------
            // HOME SCREEN APPS GRID
            // -------------------------------------------------------------
            GameObject homeScreen = CreateUIEmpty("HomeScreenGrid", phoneScreen.transform);
            SetRectTransform(homeScreen, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-20f, -100f));
            phoneCtrl.homeScreenPanel = homeScreen;

            // Row 1 Apps: Messages, Social (IdolGram), Camera
            Button bMsg = CreateAppIconButton("App_Messages", homeScreen.transform, new Vector2(-100f, 160f), iconMsg, "Tin Nhắn", defaultFont);
            Button bSoc = CreateAppIconButton("App_Social", homeScreen.transform, new Vector2(0f, 160f), iconSoc, "IdolGram", defaultFont);
            Button bCam = CreateAppIconButton("App_Camera", homeScreen.transform, new Vector2(100f, 160f), iconCam, "Camera", defaultFont);

            // Row 2 Apps: Schedule, Photos, Notes
            Button bSch = CreateAppIconButton("App_Schedule", homeScreen.transform, new Vector2(-100f, 40f), iconSch, "Lịch Trình", defaultFont);
            Button bPho = CreateAppIconButton("App_Photos", homeScreen.transform, new Vector2(0f, 40f), iconPho, "Ảnh Fan", defaultFont);
            Button bNot = CreateAppIconButton("App_Notes", homeScreen.transform, new Vector2(100f, 40f), iconNot, "Ghi Chú", defaultFont);

            phoneCtrl.appBtnMessages = bMsg;
            phoneCtrl.appBtnSocial = bSoc;
            phoneCtrl.appBtnCamera = bCam;
            phoneCtrl.appBtnSchedule = bSch;
            phoneCtrl.appBtnPhotos = bPho;
            phoneCtrl.appBtnNotes = bNot;

            // -------------------------------------------------------------
            // APP PANELS (Full window screens)
            // -------------------------------------------------------------
            // 1. Messages App Panel
            GameObject msgPanel = CreateUIEmpty("MessagesAppPanel", phoneScreen.transform);
            SetRectTransform(msgPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(msgPanel, new Color(0.08f, 0.08f, 0.12f, 0.98f), roundedBoxSprite);
            phoneCtrl.messagesPanel = msgPanel;

            Text msgTitle = AddText(msgPanel, "MsgHeader", "💬 Tin Nhắn Fanclub & Quản Lý", defaultFont, 16, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(msgTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f));

            Text msgContent = AddText(msgPanel, "MsgText", "<b>[QUẢN LÝ IDOL]:</b>\n'Tối nay nhớ lên sóng đúng 19:30 nhé em. Nhớ kiểm tra micro và góc camera!'\n\n<b>[TRƯỞNG FANCLUB]:</b>\n'Tụi em đã chuẩn bị hashtag trending đẩy top cho anh rồi ạ! Chúc idol live bùng nổ!'\n\n<b>[NHÃN HÀNG GAMING]:</b>\n'Nhãn hàng gửi tặng set tai nghe mới, chúc mừng buổi stream thành công!'", defaultFont, 14, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(msgContent.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, -20f), new Vector2(-30f, -60f));

            // 2. Social App Panel (IdolGram)
            GameObject socPanel = CreateUIEmpty("SocialAppPanel", phoneScreen.transform);
            SetRectTransform(socPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(socPanel, new Color(0.10f, 0.08f, 0.16f, 0.98f), roundedBoxSprite);
            phoneCtrl.socialPanel = socPanel;

            Text socTitle = AddText(socPanel, "SocTitle", "💖 IdolGram Official", defaultFont, 18, Color.magenta, TextAnchor.UpperCenter);
            SetRectTransform(socTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f));

            Text socFollowers = AddText(socPanel, "SocFollowers", "Followers: 128,400", defaultFont, 17, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(socFollowers.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(0f, 30f));
            phoneCtrl.socialFollowersText = socFollowers;

            Button postBtn = CreateSimpleButton("PostSocialBtn", socPanel.transform, new Vector2(0f, -20f), new Vector2(250f, 48f), "Đăng Ảnh Selfie Mới", defaultFont, new Color(0.85f, 0.2f, 0.5f), roundedBoxSprite);
            phoneCtrl.postButton = postBtn;

            Text socStatus = AddText(socPanel, "SocStatus", "Đăng ảnh mới để tương tác cùng hàng trăm nghìn người hâm mộ!", defaultFont, 13, Color.gray, TextAnchor.MiddleCenter);
            SetRectTransform(socStatus.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -85f), new Vector2(-30f, 60f));
            phoneCtrl.socialStatusText = socStatus;

            // 3. Camera App Panel
            GameObject camPanel = CreateUIEmpty("CameraAppPanel", phoneScreen.transform);
            SetRectTransform(camPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(camPanel, Color.black, roundedBoxSprite);
            phoneCtrl.cameraPanel = camPanel;

            AddText(camPanel, "CamViewfinder", "[4K 60FPS IDOL SELFIE]", defaultFont, 16, Color.cyan, TextAnchor.MiddleCenter);
            Button shutterBtn = CreateSimpleButton("ShutterBtn", camPanel.transform, new Vector2(0f, -210f), new Vector2(76f, 76f), "CHỤP", defaultFont, Color.white, circleSprite);
            phoneCtrl.shutterButton = shutterBtn;

            GameObject camFlash = CreateUIEmpty("CameraFlash", phoneScreen.transform);
            SetRectTransform(camFlash, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(camFlash, Color.white);
            camFlash.SetActive(false);
            phoneCtrl.cameraFlashImage = camFlash;

            // 4. Schedule App Panel
            GameObject schedPanel = CreateUIEmpty("ScheduleAppPanel", phoneScreen.transform);
            SetRectTransform(schedPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(schedPanel, new Color(0.08f, 0.10f, 0.14f, 0.98f), roundedBoxSprite);
            phoneCtrl.schedulePanel = schedPanel;

            Text schedText = AddText(schedPanel, "SchedSummary", "Lịch trình hôm nay", defaultFont, 14, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(schedText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, -15f), new Vector2(-30f, -30f));
            phoneCtrl.scheduleSummaryText = schedText;

            // 5. Photos App Panel
            GameObject phoPanel = CreateUIEmpty("PhotosAppPanel", phoneScreen.transform);
            SetRectTransform(phoPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(phoPanel, new Color(0.08f, 0.08f, 0.12f, 0.98f), roundedBoxSprite);
            phoneCtrl.photosPanel = phoPanel;

            Text phoTitle = AddText(phoPanel, "PhoTitle", "📸 Thư Viện Ảnh Idol", defaultFont, 17, Color.cyan, TextAnchor.UpperCenter);
            SetRectTransform(phoTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f));

            Text phoContent = AddText(phoPanel, "PhoDesc", "• Đêm nhạc Concert Tour 2026: 25,000 khán giả\n• Lễ trao giải Nút Vàng Sáng Tạo\n• Buổi fan meeting thân mật đầu năm\n• Hậu trường quay MV teaser", defaultFont, 14, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(phoContent.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, -30f), new Vector2(-30f, -60f));

            // 6. Notes App Panel
            GameObject notesPanel = CreateUIEmpty("NotesAppPanel", phoneScreen.transform);
            SetRectTransform(notesPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-16f, -68f));
            AddImage(notesPanel, new Color(0.16f, 0.14f, 0.10f, 0.98f), roundedBoxSprite);
            phoneCtrl.notesPanel = notesPanel;

            Text notTitle = AddText(notesPanel, "NotTitle", "📝 Ghi Chú Livestream Tối Nay", defaultFont, 17, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(notTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f));

            Text notContent = AddText(notesPanel, "NotContent", "1. Chào hỏi FC lúc 19:30\n2. Kể chuyện hậu trường quay video ngắn\n3. Trả lời câu hỏi khó của fan thông thái\n4. Xử lý bình tĩnh nếu mic rè hoặc mạng lag\n5. Đọc lời cảm ơn danh sách Top Donator", defaultFont, 14, Color.white, TextAnchor.UpperLeft);
            SetRectTransform(notContent.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(15f, -30f), new Vector2(-30f, -60f));

            // Home Indicator Bar (Swipe/Click to Return Home)
            GameObject homeIndicator = CreateUIEmpty("HomeIndicatorBar", phoneScreen.transform);
            SetRectTransform(homeIndicator, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(130f, 18f));
            AddImage(homeIndicator, new Color(1f, 1f, 1f, 0.75f), roundedBoxSprite);
            Button homeBtn = homeIndicator.AddComponent<Button>();
            phoneCtrl.homeIndicatorButton = homeBtn;

            // =========================================================================
            // PAUSE MENU UI (With Audio Controls: Music & SFX Toggles + Volume Sliders)
            // =========================================================================
            GameObject pauseRoot = CreateUIEmpty("PauseMenu", safeAreaObj.transform);
            SetRectTransform(pauseRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(pauseRoot, new Color(0f, 0f, 0f, 0.88f));
            PauseMenuUI pauseUI = pauseRoot.AddComponent<PauseMenuUI>();
            pauseUI.pauseMenuRoot = pauseRoot;

            AddText(pauseRoot, "PauseTitle", "TẠM DỪNG TRÒ CHƠI", defaultFont, 32, Color.white, TextAnchor.MiddleCenter);

            Button resumeBtn = CreateSimpleButton("ResumeBtn", pauseRoot.transform, new Vector2(0f, 85f), new Vector2(260f, 46f), "Tiếp Tục", defaultFont, new Color(0.2f, 0.6f, 0.3f), roundedBoxSprite);
            Button restartBtn = CreateSimpleButton("RestartBtn", pauseRoot.transform, new Vector2(0f, 25f), new Vector2(260f, 46f), "Chơi Lại Ngày Này", defaultFont, new Color(0.7f, 0.2f, 0.2f), roundedBoxSprite);
            pauseUI.resumeButton = resumeBtn;
            pauseUI.restartButton = restartBtn;

            // Audio Controls in Pause Menu
            Button musicTogBtn = CreateSimpleButton("MusicToggleBtn", pauseRoot.transform, new Vector2(-110f, -45f), new Vector2(180f, 42f), "Âm Nhạc: [BẬT]", defaultFont, new Color(0.25f, 0.35f, 0.55f), roundedBoxSprite);
            pauseUI.musicToggleButton = musicTogBtn;
            pauseUI.musicToggleText = musicTogBtn.GetComponentInChildren<Text>();

            Slider musicSl = CreateStyledSlider("MusicVolumeSlider", pauseRoot.transform, new Vector2(100f, -45f), "Âm Lượng Nhạc", defaultFont, Color.cyan, roundedBoxSprite);
            pauseUI.musicVolumeSlider = musicSl;

            Button sfxTogBtn = CreateSimpleButton("SfxToggleBtn", pauseRoot.transform, new Vector2(-110f, -105f), new Vector2(180f, 42f), "Hiệu Ứng: [BẬT]", defaultFont, new Color(0.25f, 0.35f, 0.55f), roundedBoxSprite);
            pauseUI.sfxToggleButton = sfxTogBtn;
            pauseUI.sfxToggleText = sfxTogBtn.GetComponentInChildren<Text>();

            Slider sfxSl = CreateStyledSlider("SfxVolumeSlider", pauseRoot.transform, new Vector2(100f, -105f), "Âm Lượng SFX", defaultFont, Color.green, roundedBoxSprite);
            pauseUI.sfxVolumeSlider = sfxSl;

            // =========================================================================
            // END OF DAY SUMMARY UI
            // =========================================================================
            GameObject summaryRoot = CreateUIEmpty("DaySummaryModal", safeAreaObj.transform);
            SetRectTransform(summaryRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(summaryRoot, new Color(0.04f, 0.05f, 0.10f, 0.96f));
            DaySummaryUI summaryUI = summaryRoot.AddComponent<DaySummaryUI>();
            summaryUI.summaryRoot = summaryRoot;

            Text sumTitle = AddText(summaryRoot, "SumTitle", "TỔNG KẾT MỘT NGÀY CỦA IDOL STREAMER", defaultFont, 28, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(sumTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(0f, 50f));

            Text rankTxt = AddText(summaryRoot, "RankText", "HẠNG S (XUẤT SẮC)", defaultFont, 34, Color.yellow, TextAnchor.UpperCenter);
            SetRectTransform(rankTxt.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(0f, 55f));
            summaryUI.rankText = rankTxt;

            Text peakTxt = AddText(summaryRoot, "PeakTxt", "Lượng xem đỉnh cao: 15,200 người xem", defaultFont, 20, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(peakTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(0f, 36f));
            summaryUI.peakViewersText = peakTxt;

            Text followGainedTxt = AddText(summaryRoot, "FollowGainedTxt", "Lượng fan mới theo dõi: +3,800 followers", defaultFont, 20, Color.white, TextAnchor.MiddleCenter);
            SetRectTransform(followGainedTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 36f));
            summaryUI.followersGainedText = followGainedTxt;

            Text earnTxt = AddText(summaryRoot, "EarnTxt", "Thu nhập quà tặng buổi live: +3,500,000 VNĐ", defaultFont, 20, Color.green, TextAnchor.MiddleCenter);
            SetRectTransform(earnTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(0f, 36f));
            summaryUI.sessionEarningsText = earnTxt;

            Text commentTxt = AddText(summaryRoot, "CommentTxt", "Quản lý nhận xét: 'Một ngày làm việc xuất sắc!'", defaultFont, 18, Color.cyan, TextAnchor.MiddleCenter);
            SetRectTransform(commentTxt.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(-100f, 50f));
            summaryUI.managerCommentText = commentTxt;

            Button playAgainBtn = CreateSimpleButton("PlayAgainBtn", summaryRoot.transform, new Vector2(0f, -200f), new Vector2(280f, 54f), "BẮT ĐẦU NGÀY MỚI", defaultFont, Color.yellow, roundedBoxSprite);
            summaryUI.playAgainButton = playAgainBtn;

            // =========================================================================
            // MOBILE TOUCH CONTROLS UI (Safe Area & Clean Non-Overlapping Position)
            // =========================================================================
            GameObject mobControls = CreateUIEmpty("MobileTouchControls", safeAreaObj.transform);
            SetRectTransform(mobControls, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            MobileTouchControlsUI mobUI = mobControls.AddComponent<MobileTouchControlsUI>();
            mobUI.controlsRoot = mobControls;

            // Left: Circular Joystick
            GameObject joyBg = CreateUIEmpty("JoystickBG", mobControls.transform);
            SetRectTransform(joyBg, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(140f, 140f), new Vector2(160f, 160f));
            AddImage(joyBg, new Color(1f, 1f, 1f, 0.22f), ringSprite);
            mobUI.joystickBackground = joyBg.GetComponent<RectTransform>();

            GameObject joyHandle = CreateUIEmpty("JoystickHandle", joyBg.transform);
            SetRectTransform(joyHandle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
            AddImage(joyHandle, new Color(1f, 1f, 1f, 0.65f), circleSprite);
            mobUI.joystickHandle = joyHandle.GetComponent<RectTransform>();

            // Right: Tactile Circular Interact Button
            Button interactBtn = CreateSimpleButton("MobileInteractBtn", mobControls.transform, new Vector2(-130f, 130f), new Vector2(120f, 120f), "TƯƠNG\nTÁC", defaultFont, new Color(0.12f, 0.55f, 0.95f, 0.88f), circleSprite);
            SetRectTransform(interactBtn.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-130f, 130f), new Vector2(120f, 120f));
            mobUI.interactButton = interactBtn;

            mobUI.phoneButton = phoneBtn;
            mobUI.pauseButton = pauseBtn;

            // =========================================================================
            // TITLE / INTRO LOADING SCREEN
            // =========================================================================
            GameObject titleObj = CreateUIEmpty("TitleLoadingScreen", canvasObj.transform);
            SetRectTransform(titleObj, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CanvasGroup titleCg = titleObj.AddComponent<CanvasGroup>();
            AddImage(titleObj, new Color(0.04f, 0.04f, 0.08f, 1f));

            TitleLoadingScreenUI titleUI = titleObj.AddComponent<TitleLoadingScreenUI>();
            titleUI.canvasGroup = titleCg;

            Text titleTxt = AddText(titleObj, "TitleText", "MỘT NGÀY CỦA IDOL STREAMER", defaultFont, 38, Color.yellow, TextAnchor.MiddleCenter);
            SetRectTransform(titleTxt.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(800f, 70f));
            titleUI.titleText = titleTxt;

            Text subtitleTxt = AddText(titleObj, "SubtitleText", "★ Idol Streamer Life Simulator 3D ★", defaultFont, 22, Color.cyan, TextAnchor.MiddleCenter);
            SetRectTransform(subtitleTxt.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(600f, 40f));
            titleUI.subtitleText = subtitleTxt;

            Slider titleProgress = CreateStyledSlider("LoadingBar", titleObj.transform, new Vector2(0f, -80f), "Đang tải căn hộ Idol...", defaultFont, new Color(0.2f, 0.8f, 0.95f), roundedBoxSprite);
            SetRectTransform(titleProgress.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(360f, 26f));
            titleUI.loadingProgressBar = titleProgress;

            Button startTapBtn = CreateSimpleButton("StartTapBtn", titleObj.transform, new Vector2(0f, -140f), new Vector2(280f, 50f), "CHẠM ĐỂ BẮT ĐẦU", defaultFont, new Color(0.2f, 0.7f, 0.4f), roundedBoxSprite);
            SetRectTransform(startTapBtn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(280f, 50f));
            titleUI.startButton = startTapBtn;

            // =========================================================================
            // ON-SCREEN DEBUG MONITOR HUD
            // =========================================================================
            GameObject debugObj = CreateUIEmpty("DebugHUD", safeAreaObj.transform);
            SetRectTransform(debugObj, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            debugObj.AddComponent<DebugScreenHUD>();

            // =========================================================================
            // WIRE HUD MANAGER
            // =========================================================================
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

        private static Button CreateAppIconButton(string name, Transform parent, Vector2 pos, Sprite iconSprite, string label, Font font)
        {
            GameObject btnObj = CreateUIEmpty(name, parent);
            SetRectTransform(btnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(76f, 96f));

            // Icon Image
            GameObject iconObj = CreateUIEmpty("IconImg", btnObj.transform);
            SetRectTransform(iconObj, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(66f, 66f));
            AddImage(iconObj, Color.white, iconSprite);

            // Label Text below
            Text lbl = AddText(btnObj, "Label", label, font, 13, Color.white, TextAnchor.LowerCenter);
            SetRectTransform(lbl.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 22f));

            Button btn = btnObj.AddComponent<Button>();
            return btn;
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

        private static Image AddImage(GameObject obj, Color color, Sprite sprite = null)
        {
            Image img = obj.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
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

        private static Button CreateSimpleButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Font font, Color bgColor, Sprite sprite = null)
        {
            GameObject btnObj = CreateUIEmpty(name, parent);
            SetRectTransform(btnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            AddImage(btnObj, bgColor, sprite);
            Button btn = btnObj.AddComponent<Button>();

            if (!string.IsNullOrEmpty(label))
            {
                Text txt = AddText(btnObj, "Text", label, font, 16, Color.white, TextAnchor.MiddleCenter);
                SetRectTransform(txt.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            return btn;
        }

        private static Slider CreateStyledSlider(string name, Transform parent, Vector2 pos, string label, Font font, Color fillColor, Sprite sprite = null)
        {
            GameObject sliderObj = CreateUIEmpty(name, parent);
            SetRectTransform(sliderObj, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, new Vector2(200f, 24f));

            GameObject bg = CreateUIEmpty("Background", sliderObj.transform);
            SetRectTransform(bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(bg, new Color(0.12f, 0.12f, 0.18f, 0.85f), sprite);

            GameObject fillArea = CreateUIEmpty("Fill Area", sliderObj.transform);
            SetRectTransform(fillArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(2f, 0f), new Vector2(-4f, -4f));
            GameObject fill = CreateUIEmpty("Fill", fillArea.transform);
            SetRectTransform(fill, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image fillImg = AddImage(fill, fillColor, sprite);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.value = 1f;

            if (!string.IsNullOrEmpty(label))
            {
                Text lbl = AddText(sliderObj, "Label", label, font, 13, Color.white, TextAnchor.MiddleLeft);
                SetRectTransform(lbl.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(0f, 18f));
            }

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
