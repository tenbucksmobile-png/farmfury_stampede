using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Data;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Edit-mode contact sheets of every hat on every character, and of every machine, so hat placement can be checked
    /// without Play mode or a manual screenshot (ported from Arcade's CosmeticPreviewRenderer, reshaped for side view).
    ///
    /// Each cell rebuilds exactly what CharacterCosmeticRenderer.UpdateHat draws at runtime: the player's Visual
    /// (scaled by CharacterData.visualScale) showing one pose from the character's CharacterSpriteSet, with the hat as
    /// a child at that frame's CharacterData.GetHatPlacement anchor, CosmeticData.Hat(right, variant) as the art
    /// (flipped when HatNeedsMirror) and scaled to the head width x hatScale. A magenta dot marks the hat anchor and a cyan
    /// dot the feet pivot, so "the hat sits too high / too far back" reads straight off the image.
    ///
    /// Output (project root, not under Assets/, so it is never imported; ignored by git):
    ///   CosmeticPreviews/hat_&lt;name&gt;.png - one sheet per hat (each sombrero design separately): columns = the eight
    ///     characters in CharacterType order, rows = Idle R, Idle L, Run R, Run L, Jump R, Jump L (the last run frame;
    ///     an empty cell = no such frame - the game then shows idle).
    ///   CosmeticPreviews/machines.png - one row per machine: its character's idle right for size, machine right, left.
    /// Runs in batch mode too, but needs graphics (no -nographics), or the sheets come out blank.
    /// </summary>
    public static class StampedeCosmeticPreview
    {
        private const string OutputDir = "CosmeticPreviews";
        private const string CharacterDir = "Assets/_Project/ScriptableObjects/Characters";
        private const int Cell = 256;
        private const float HatViewHalfHeight = 1.75f;   // fits Horace (2.1 u drawn) plus a tall hat
        private const float HatViewCentreY = 1.45f;
        private const float MachineViewHalfHeight = 2.2f;
        private const float MachineViewCentreY = 1.8f;
        private const float DotSize = 0.07f;

        private static readonly (string label, bool right, System.Func<CharacterSpriteSet, Sprite> pick)[] Poses =
        {
            ("Idle R", true, s => s.idleRight != null ? s.idleRight : First(s.runRight)),
            ("Idle L", false, s => s.idleLeft != null ? s.idleLeft : First(s.runLeft)),
            ("Run R", true, s => Last(s.runRight)),
            ("Run L", false, s => Last(s.runLeft)),
            ("Jump R", true, s => s.jumpRight),
            ("Jump L", false, s => s.jumpLeft),
        };

        [MenuItem("Farm Fury Stampede/Debug/Render Cosmetic Preview Sheets")]
        public static void RenderAll()
        {
            var characters = System.Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>()
                .Select(c => AssetDatabase.LoadAssetAtPath<CharacterData>($"{CharacterDir}/CharacterData_{c}.asset"))
                .ToArray();
            var cosmetics = AssetDatabase.FindAssets("t:CosmeticData")
                .Select(g => AssetDatabase.LoadAssetAtPath<CosmeticData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null).OrderBy(c => c.cosmeticId).ToList();

            Directory.CreateDirectory(OutputDir);
            var rig = new Rig();
            var log = new List<string>();
            try
            {
                foreach (var data in characters.Where(d => d != null))
                {
                    log.Add($"{data.characterType}: {data.hatPlacements?.Length ?? 0} hat placements, fallback R ({data.hatAnchor.x:F2}, {data.hatAnchor.y:F2}) L ({data.hatAnchorLeft.x:F2}, {data.hatAnchorLeft.y:F2}) width {data.hatWidth:F2}, visualScale {data.visualScale:F2}");
                }

                // One sheet per hat: shared hats (sombrero designs, chef hat, crown) and per-character families (caps, cowboy hats).
                var hats = cosmetics.Where(c => c.cosmeticType == CosmeticType.Hat).ToList();
                foreach (var family in hats.GroupBy(FamilyName))
                {
                    var items = family.ToList();
                    int variants = items.Max(h => h.hatVariants != null && h.hatVariants.Length > 1 ? h.hatVariants.Length : 1);
                    for (int v = 0; v < variants; v++)
                    {
                        string name = variants > 1 ? $"{family.Key}_{v + 1}" : family.Key;
                        RenderHatSheet(rig, characters, items, v, Path.Combine(OutputDir, $"hat_{name}.png"), log);
                    }
                }

                var machines = cosmetics.Where(c => c.cosmeticType == CosmeticType.Skin).ToList();
                if (machines.Count > 0)
                {
                    RenderMachineSheet(rig, characters, machines, Path.Combine(OutputDir, "machines.png"), log);
                }
            }
            finally
            {
                rig.Dispose();
            }

            Debug.Log($"[CosmeticPreview] Wrote sheets to {Path.GetFullPath(OutputDir)}. Columns: {string.Join(", ", characters.Select(c => c != null ? c.characterType.ToString() : "?"))}; " +
                      $"rows: {string.Join(", ", Poses.Select(p => p.label))}.\n" + string.Join("\n", log));
        }

        // Shared hats are their own family; per-character ones (baseball_cap_cluck, ...) group by the id minus the character.
        private static string FamilyName(CosmeticData hat)
        {
            if (hat.anyCharacter)
            {
                return hat.cosmeticId;
            }
            int cut = hat.cosmeticId.LastIndexOf('_');
            return cut > 0 ? hat.cosmeticId.Substring(0, cut) : hat.cosmeticId;
        }

        private static void RenderHatSheet(Rig rig, CharacterData[] characters, List<CosmeticData> family, int variant, string path, List<string> log)
        {
            int cols = characters.Length, rows = Poses.Length;
            rig.Begin(cols * Cell, rows * Cell, HatViewHalfHeight, HatViewCentreY);
            for (int col = 0; col < cols; col++)
            {
                var data = characters[col];
                var hat = data != null ? family.FirstOrDefault(h => h.FitsCharacter(data.characterType)) : null;
                for (int row = 0; row < rows; row++)
                {
                    var (label, right, pick) = Poses[row];
                    var body = data != null && data.spriteSet != null ? pick(data.spriteSet) : null;
                    rig.Show(data, body);
                    if (body != null && hat != null)
                    {
                        var sprite = hat.Hat(right, variant);
                        if (sprite != null)
                        {
                            rig.ShowHat(data, sprite, right, hat.HatNeedsMirror(right), hat.hatScale);
                            if (row == 0)
                            {
                                data.GetHatPlacement(body, true, out _, out float head);
                                log.Add($"{Path.GetFileName(path)} {data.characterType}: {hat.cosmeticId} hatScale {hat.hatScale:F2} drawn {head * hat.hatScale * data.visualScale:F2} u wide");
                            }
                        }
                    }
                    else if (body != null && hat == null && row == 0)
                    {
                        log.Add($"{Path.GetFileName(path)} {data.characterType}: no hat in this family");
                    }
                    rig.Render(col, row, rows);
                }
            }
            rig.Save(path);
        }

        private static void RenderMachineSheet(Rig rig, CharacterData[] characters, List<CosmeticData> machines, string path, List<string> log)
        {
            int rows = machines.Count;
            rig.Begin(3 * Cell, rows * Cell, MachineViewHalfHeight, MachineViewCentreY);
            for (int row = 0; row < rows; row++)
            {
                var machine = machines[row];
                var data = characters.FirstOrDefault(c => c != null && machine.FitsCharacter(c.characterType));
                var idle = data != null && data.spriteSet != null ? Poses[0].pick(data.spriteSet) : null;
                rig.Show(data, idle);
                rig.Render(0, row, rows);
                rig.Show(data, machine.Skin(true));
                rig.Render(1, row, rows);
                rig.Show(data, machine.Skin(false));
                rig.Render(2, row, rows);
                var skin = machine.Skin(true);
                log.Add($"machines.png row {row}: {machine.cosmeticId} on {(data != null ? data.characterType.ToString() : "?")}" +
                        (skin != null ? $", drawn {skin.bounds.size.x * (data != null ? data.visualScale : 1f):F2} x {skin.bounds.size.y * (data != null ? data.visualScale : 1f):F2} u" : ", no art"));
            }
            rig.Save(path);
        }

        private static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;
        private static Sprite Last(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[frames.Length - 1] : null;

        /// <summary>The temporary camera, render texture and player stand-in (Visual + body + hat + marker dots).</summary>
        private sealed class Rig
        {
            private readonly GameObject _cameraObject;
            private readonly Camera _camera;
            private readonly Transform _visual;
            private readonly SpriteRenderer _body, _hat, _anchorDot;
            private readonly Texture2D _dotTexture;
            private RenderTexture _target;
            private int _height;

            public Rig()
            {
                _cameraObject = new GameObject("CosmeticPreviewCamera_TEMP") { hideFlags = HideFlags.HideAndDontSave };
                _camera = _cameraObject.AddComponent<Camera>();
                _camera.orthographic = true;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0.8f, 0.85f, 0.8f, 1f);
                _camera.nearClipPlane = 0.1f;
                _camera.farClipPlane = 50f;
                _camera.enabled = false;

                _dotTexture = new Texture2D(4, 4) { hideFlags = HideFlags.HideAndDontSave };
                _dotTexture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
                _dotTexture.Apply();
                var dot = Sprite.Create(_dotTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f / DotSize);

                var visual = new GameObject("Visual_TEMP") { hideFlags = HideFlags.HideAndDontSave };
                _visual = visual.transform;
                _body = visual.AddComponent<SpriteRenderer>();
                _hat = Child("Hat", 1);
                _anchorDot = Child("AnchorDot", 2);
                _anchorDot.sprite = dot;
                _anchorDot.color = Color.magenta;
                var feetDot = Child("FeetDot", 2);
                feetDot.sprite = dot;
                feetDot.color = Color.cyan;
            }

            private SpriteRenderer Child(string name, int order)
            {
                var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(_visual, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = order;
                return renderer;
            }

            public void Begin(int width, int height, float halfHeight, float centreY)
            {
                ReleaseTarget();
                _height = height;
                _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _target.Create();
                _camera.targetTexture = _target;
                _camera.orthographicSize = halfHeight;
                _cameraObject.transform.position = new Vector3(0f, centreY, -10f);
            }

            /// <summary>A character pose with no hat (null body = an empty cell).</summary>
            public void Show(CharacterData data, Sprite body)
            {
                _visual.localScale = Vector3.one * (data != null ? data.visualScale : 1f);
                _body.sprite = body;
                _body.flipX = false;
                _hat.enabled = false;
                _anchorDot.enabled = false;
            }

            // CharacterCosmeticRenderer.UpdateHat, on the stand-in.
            public void ShowHat(CharacterData data, Sprite sprite, bool right, bool mirror, float hatScale)
            {
                data.GetHatPlacement(_body.sprite, right, out var head, out float headWidth);
                var anchor = new Vector3(head.x, head.y, 0f);
                _hat.enabled = true;
                _hat.sprite = sprite;
                _hat.flipX = mirror;
                _hat.transform.localPosition = anchor;
                float width = Mathf.Max(0.01f, sprite.bounds.size.x);
                _hat.transform.localScale = Vector3.one * (headWidth * hatScale / width);
                _anchorDot.enabled = true;
                _anchorDot.transform.localPosition = anchor;
            }

            public void Render(int col, int row, int rows)
            {
                _camera.pixelRect = new Rect(col * Cell, (rows - 1 - row) * Cell, Cell, Cell);
                _camera.Render();
            }

            public void Save(string path)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = _target;
                var texture = new Texture2D(_target.width, _height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, _target.width, _height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            private void ReleaseTarget()
            {
                if (_target != null)
                {
                    _camera.targetTexture = null;
                    _target.Release();
                    Object.DestroyImmediate(_target);
                    _target = null;
                }
            }

            public void Dispose()
            {
                ReleaseTarget();
                Object.DestroyImmediate(_visual.gameObject);
                Object.DestroyImmediate(_cameraObject);
                Object.DestroyImmediate(_dotTexture);
            }
        }
    }
}
