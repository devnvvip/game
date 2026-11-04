using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace StreamerGame.Editor
{
    public static class BuildScript
    {
        private const string IOSBundleId = "com.devnvvip.game";
        private const string GameProductName = "Idol Streamer Life";
        private const string CompanyName = "devnvvip";
        private const string IOSBuildPath = "Builds/iOS";

        [MenuItem("Build/Configure iOS Settings")]
        public static void ConfigureIOSSettings()
        {
            Debug.Log("[BuildScript] Configuring Player Settings for iOS...");

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = GameProductName;

            // iOS Bundle Identifier
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IOSBundleId);

            // Minimum iOS Version 15.0
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            // Scripting Backend: IL2CPP & Architecture: ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // 1 = ARM64

            // Orientation: Landscape only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Fullscreen
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildScript] iOS Player Settings configured: BundleId={IOSBundleId}, OS=15.0, IL2CPP/ARM64, Landscape.");
        }

        [MenuItem("Build/Build iOS")]
        public static void BuildIOS()
        {
            Debug.Log("[BuildScript] Pre-building scene to guarantee latest assets & materials...");
            SceneAutoBuilder.RebuildScene();

            ConfigureIOSSettings();

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                scenes = new string[] { "Assets/Scenes/MainStreamerScene.unity" };
            }

            Debug.Log($"[BuildScript] Starting iOS Xcode build to: {IOSBuildPath}");
            Debug.Log($"[BuildScript] Scenes included in build:\n - {string.Join("\n - ", scenes)}");

            if (Directory.Exists(IOSBuildPath))
            {
                try
                {
                    Directory.Delete(IOSBuildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BuildScript] Could not clean directory {IOSBuildPath}: {e.Message}");
                }
            }
            Directory.CreateDirectory(IOSBuildPath);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = IOSBuildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] iOS Xcode project build SUCCEEDED!");
                Debug.Log($"[BuildScript] Output path: {summary.outputPath}, Size: {summary.totalSize} bytes, Time: {summary.totalTime}");
            }
            else
            {
                Debug.LogError($"[BuildScript] iOS Xcode project build FAILED with result: {summary.result}");
                throw new Exception($"[BuildScript] iOS Build failed: {summary.result}");
            }
        }
    }
}
