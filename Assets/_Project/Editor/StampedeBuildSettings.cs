using System.IO;
using FarmFuryStampede.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Player settings both stores require, pinned so they can't drift (audit 2026-10-25): IL2CPP for Android (Google
    /// Play only takes 64-bit builds, and ARM64 needs IL2CPP; the project had no backend set, leaving it to the
    /// editor's default) with ARM64 as the target; iOS is always IL2CPP. Applied on every editor load and checked again
    /// before each build, which also warns about what would get a store build rejected: an Android release build
    /// signed with the debug key.
    /// </summary>
    [InitializeOnLoad]
    public class StampedeBuildSettings : IPreprocessBuildWithReport
    {
        static StampedeBuildSettings()
        {
            EditorApplication.delayCall += Apply;   // after the editor has finished loading the project
        }

        public int callbackOrder => -100;

        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        public static void Apply()
        {
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                Debug.Log("[BuildSettings] Android scripting backend set to IL2CPP.");
            }
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
                Debug.Log("[BuildSettings] Android ARM64 target added.");
            }
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            }
        }

        /// <summary>
        /// Google Play release settings: an App Bundle, no development build, the art split into an asset pack (the
        /// base module may be at most 200 MB; the APK is ~500), LZ4HC, public symbols for Play's crash reports. The
        /// signing key stays the user's own step in Player Settings.
        /// </summary>
        [MenuItem("Farm Fury Stampede/Android/Set Up For Google Play AAB")]
        public static void SetUpGooglePlay()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
            EditorUserBuildSettings.SetPlatformSettings("Android", "CompressionType", "LZ4HC");
            PlayerSettings.Android.splitApplicationBinary = true;
            EnsureBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildSettings] Android set up for a Google Play AAB. Set the keystore, then File > Build Profiles > Build.");
        }

        // Boot.unity first, then Game.unity (see BootLoader): a black camera and the loader, nothing else.
        private static void EnsureBootScene()
        {
            if (!File.Exists(BootScenePath))
            {
                var previous = SceneManager.GetActiveScene().path;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                new GameObject("BootLoader").AddComponent<BootLoader>();
                EditorSceneManager.SaveScene(scene, BootScenePath);
                if (!string.IsNullOrEmpty(previous)) { EditorSceneManager.OpenScene(previous); }
            }
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        /// <summary>Headless check that the AAB builds (debug-signed unless a keystore is set): -executeMethod ...BuildTestAab -aabPath file.aab</summary>
        public static void BuildTestAab()
        {
            SetUpGooglePlay();
            string[] args = System.Environment.GetCommandLineArgs();
            string path = args[System.Array.IndexOf(args, "-aabPath") + 1];
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootScenePath, GameScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC,
            });
            Debug.Log($"[BuildSettings] Test AAB: {report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Apply();
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            if (report.summary.platform == BuildTarget.Android && !development && !PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogWarning("[BuildSettings] Android release build without a custom keystore: it is signed with the " +
                                 "debug key, which Google Play rejects. Set one in Player Settings > Publishing Settings.");
            }
        }
    }
}
