using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Website demo (Meadow Ruins only) build tooling, after Farm Fury: Arcade's WebDemoBuilder. See WEB_DEMO_PLAN.md at
    /// the repo root.
    ///
    /// Unity only puts an asset into a build if the scene (or a Resources folder) references it, so the other worlds
    /// are kept out of the download by removing references here - DataManager's level list, the world music, the
    /// other worlds' menu backdrops, the cosmetics - not by deleting their files.
    ///
    /// Run Setup (Farm Fury Stampede > Phase 5a > Run Setup) puts every world's references back: after it, run
    /// "Trim To Meadow Ruins" again (Build Website does it anyway).
    /// </summary>
    public static class WebDemoBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string OutputPath = "Builds/Website";

        [MenuItem("Farm Fury Stampede/Web Demo/Trim To Meadow Ruins")]
        public static void TrimToDemoContent()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            int levels = 0, cosmetics = 0, music = 0, worldArt = 0;

            var data = Object.FindAnyObjectByType<DataManager>(FindObjectsInactive.Include);
            if (data != null)
            {
                var so = new SerializedObject(data);
                levels = RemoveWhere(so.FindProperty("allLevels"), o => o is LevelData l && !WebDemo.IsPlayable(l.worldType));
                cosmetics = RemoveWhere(so.FindProperty("allCosmetics"), _ => true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogError("[WebDemoBuilder] No DataManager in the scene.");
            }

            var audio = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (audio != null)
            {
                var so = new SerializedObject(audio);
                var list = so.FindProperty("worldMusic");
                for (int i = list.arraySize - 1; i >= 0; i--)
                {
                    var world = list.GetArrayElementAtIndex(i).FindPropertyRelative("world");
                    if (!WebDemo.IsPlayable((WorldType)world.enumValueIndex))
                    {
                        list.DeleteArrayElementAtIndex(i);
                        music++;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // The other worlds stay on World Select as badges (their Full Game panel shows one), so only their menu
            // backdrops and cards go.
            foreach (string guid in AssetDatabase.FindAssets("t:WorldData"))
            {
                var world = AssetDatabase.LoadAssetAtPath<WorldData>(AssetDatabase.GUIDToAssetPath(guid));
                if (world == null || WebDemo.IsPlayable(world.worldType)) { continue; }
                bool changed = world.selectCardArt != null || world.levelSelectBackground != null || world.levelCompleteBackground != null
                    || world.levelFailedBackground != null || world.newCharacterBackground != null || world.characterSwapBackground != null;
                world.selectCardArt = null;
                world.levelSelectBackground = null;
                world.levelCompleteBackground = null;
                world.levelFailedBackground = null;
                world.newCharacterBackground = null;
                world.characterSwapBackground = null;
                if (changed)
                {
                    EditorUtility.SetDirty(world);
                    worldArt++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WebDemoBuilder] Trimmed to Meadow Ruins: removed {levels} level(s), {cosmetics} cosmetic(s), " +
                      $"{music} world music track(s); cleared the menu art of {worldArt} world(s).");
        }

        /// <summary>Website build into Builds/Website, for farmfurygames.com/play/stampede/: gzip with Unity's
        /// decompression fallback (the cPanel host needs no special headers), hashed file names (it caches for a day).</summary>
        [MenuItem("Farm Fury Stampede/Web Demo/Build Website")]
        public static void BuildWebsite()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            TrimToDemoContent();
            ApplyWebSizeSettings();

            PlayerSettings.productName = "Farm Fury Stampede";
            PlayerSettings.companyName = "Tenbucks Mobile";
            PlayerSettings.WebGL.template = "PROJECT:StampedeEmbed";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.runInBackground = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"[WebDemoBuilder] Build {summary.result} -> {OutputPath}: {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{summary.totalErrors} error(s), {summary.totalTime}.");
            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        // ---- download size (Arcade's numbers) ------------------------------------------------------------------

        // Above this source size (longest side, px) a texture is a full-screen backdrop and keeps up to 2048.
        private const int BackdropThreshold = 1000;
        private const int BackdropMaxSize = 2048;
        private const int SpriteMaxSize = 512;
        // Characters and robots are drawn ~1.5 units tall (about 110 px on a 1080p screen): 256 is still ~2x.
        private const int ActorMaxSize = 256;
        private const long MusicBytesThreshold = 300 * 1024;

        private static bool IsActor(string path) => path.Contains("/Sprites/Characters/") || path.Contains("/Sprites/Robots/");

        // DXT only compresses textures whose (downscaled) sides are multiples of 4; the rest use ASTC (Unity would
        // otherwise ship them uncompressed). Browsers decode on the CPU where the GPU lacks the format.
        private static TextureImporterFormat ChooseWebFormat(int w, int h, int maxSize, bool isBackdrop)
        {
            float scale = Mathf.Max(w, h) > maxSize ? (float)maxSize / Mathf.Max(w, h) : 1f;
            int rw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
            int rh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
            if (rw % 4 == 0 && rh % 4 == 0)
            {
                return TextureImporterFormat.DXT5Crunched;
            }
            return isBackdrop ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
        }

        /// <summary>WebGL-only texture/audio overrides and player settings; never touches the mobile settings. Safe to re-run.</summary>
        [MenuItem("Farm Fury Stampede/Web Demo/Apply Web Size Settings")]
        public static void ApplyWebSizeSettings()
        {
            int textures = 0, audio = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Sprites" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) { continue; }
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                bool isBackdrop = Mathf.Max(w, h) > BackdropThreshold && !IsActor(path);
                int maxSize = isBackdrop ? BackdropMaxSize : IsActor(path) ? ActorMaxSize : SpriteMaxSize;
                var format = ChooseWebFormat(w, h, maxSize, isBackdrop);

                var settings = importer.GetPlatformTextureSettings("WebGL");
                if (settings.overridden && settings.maxTextureSize == maxSize && settings.format == format && settings.compressionQuality == 50)
                {
                    continue;
                }
                settings.overridden = true;
                settings.maxTextureSize = maxSize;
                settings.format = format;
                settings.crunchedCompression = format == TextureImporterFormat.DXT5Crunched;
                settings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                textures++;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer) { continue; }
                bool isLong = new FileInfo(path).Length > MusicBytesThreshold;
                var current = importer.GetOverrideSampleSettings("WebGL");
                var wanted = current;
                wanted.compressionFormat = AudioCompressionFormat.AAC;
                wanted.quality = isLong ? 0.4f : 0.6f;
                wanted.loadType = isLong ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                if (importer.ContainsSampleSettingsOverride("WebGL") && current.compressionFormat == wanted.compressionFormat
                    && Mathf.Approximately(current.quality, wanted.quality) && current.loadType == wanted.loadType
                    && importer.loadInBackground == isLong)
                {
                    continue;
                }
                importer.SetOverrideSampleSettings("WebGL", wanted);
                importer.loadInBackground = isLong;
                importer.SaveAndReimport();
                audio++;
            }

            var target = NamedBuildTarget.WebGL;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            AssetDatabase.SaveAssets();
            Debug.Log($"[WebDemoBuilder] Web size settings: {textures} texture(s) and {audio} audio clip(s) updated.");
        }

        // Removes every element of an object-reference list that matches; returns how many went.
        private static int RemoveWhere(SerializedProperty list, System.Func<Object, bool> remove)
        {
            if (list == null || !list.isArray) { return 0; }
            int removed = 0;
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var element = list.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null || remove(element.objectReferenceValue))
                {
                    element.objectReferenceValue = null;   // a non-null reference must be nulled before it can be deleted
                    list.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            return removed;
        }
    }
}
