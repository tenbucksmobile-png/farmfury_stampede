using System.IO;
using FarmFuryStampede.Core;
using FarmFuryStampede.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Puts the project on URP (2026-11-01): the package was installed but no pipeline asset was assigned, so the game
    /// ran on the deprecated Built-in pipeline. Uses the Universal Renderer, not the 2D one: nothing here is lit, and
    /// a sprite that picks up URP's lit default material would draw black under the 2D renderer with no Light2D in the
    /// scene, while the Universal Renderer draws lit and legacy sprites alike. Shadows, lights, HDR, MSAA and depth are
    /// off, since a sprite game has no use for them on a phone. Safe to re-run.
    /// </summary>
    public static class StampedeRenderPipeline
    {
        private const string Dir = "Assets/_Project/Settings";
        private const string RendererPath = Dir + "/StampedeRenderer.asset";
        private const string PipelinePath = Dir + "/StampedeURP.asset";

        [MenuItem("Farm Fury Stampede/Debug/Apply URP Pipeline")]
        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 1;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            var so = new SerializedObject(pipeline);
            SetInt(so, "m_MainLightRenderingMode", 0);
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);
            SetBool(so, "m_MainLightShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            AssetDatabase.SaveAssets();
            Debug.Log($"[RenderPipeline] Applied {PipelinePath}; current: {GraphicsSettings.currentRenderPipeline}");
        }

        /// <summary>Renders one secret room of each world to CosmeticPreviews/room_&lt;level&gt;.png (headless, as RenderCheck).</summary>
        public static void RenderRooms()
        {
            (string level, float x)[] rooms =
            {
                ("MeadowRuins_01", 155.5f), ("FrozenTundra_02", 157.5f), ("WatermillVillage_01", 158.5f), ("SkyIslands_01", 158.5f),
                ("SunkenCity_01", 163.5f), ("RobotMothership_01", 163.5f), ("DustbowlCanyon_01", 163.5f), ("HarvestFairground_01", 163.5f), ("CropFactory_01", 163.5f),
            };
            Directory.CreateDirectory("CosmeticPreviews");
            foreach (var (name, x) in rooms)
            {
                var level = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Levels/{name}.prefab"));
                var cameraObject = new GameObject("RoomCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 9f;
                camera.transform.position = new Vector3(x, -36f, -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.5f, 0.7f, 0.9f);
                var target = new RenderTexture(640, 360, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                RenderTexture.active = null;
                File.WriteAllBytes($"CosmeticPreviews/room_{name}.png", image.EncodeToPNG());
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(level);
            }
        }

        private const string UnlockAllMenu = "Farm Fury Stampede/Debug/Unlock All Levels";

        /// <summary>The F1 panel's "All levels" switch as a menu item (ticked while on); works in or out of Play mode.</summary>
        [MenuItem(UnlockAllMenu)]
        private static void ToggleUnlockAll()
        {
            bool on = PlayerPrefs.GetInt(SaveManager.DebugAllLevelsOpenKey, 0) != 1;
            PlayerPrefs.SetInt(SaveManager.DebugAllLevelsOpenKey, on ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Debug] Unlock All Levels: {(on ? "ON" : "OFF")}");
            if (Application.isPlaying && GameFlow.Instance != null) { GameFlow.Instance.RefreshCurrentScreen(); }
        }

        [MenuItem(UnlockAllMenu, true)]
        private static bool ToggleUnlockAllTick()
        {
            Menu.SetChecked(UnlockAllMenu, PlayerPrefs.GetInt(SaveManager.DebugAllLevelsOpenKey, 0) == 1);
            return true;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var property = so.FindProperty(name);
            if (property != null) { property.intValue = value; } else { Debug.LogWarning($"[RenderPipeline] No field {name}"); }
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var property = so.FindProperty(name);
            if (property != null) { property.boolValue = value; } else { Debug.LogWarning($"[RenderPipeline] No field {name}"); }
        }

        /// <summary>
        /// Renders a level prefab to CosmeticPreviews/render_check_&lt;label&gt;.png (git-ignored), for comparing pipelines.
        /// Headless: -batchmode -executeMethod ...RenderCheck -label before -quit (needs graphics, so no -nographics).
        /// </summary>
        public static void RenderCheck()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-label");
            string label = at >= 0 && at + 1 < args.Length ? args[at + 1] : "check";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Levels/MeadowRuins_01.prefab");
            var level = Object.Instantiate(prefab);
            var cameraObject = new GameObject("RenderCheckCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.transform.position = new Vector3(20f, 4f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.5f, 0.7f, 0.9f);

            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            RenderTexture.active = null;

            Directory.CreateDirectory("CosmeticPreviews");
            File.WriteAllBytes($"CosmeticPreviews/render_check_{label}.png", image.EncodeToPNG());
            Debug.Log($"[RenderPipeline] Wrote render_check_{label}.png under {GraphicsSettings.currentRenderPipeline}");
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(level);
        }
    }
}
