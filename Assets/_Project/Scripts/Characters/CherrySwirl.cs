using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// The cherry power-up's look (CharacterController2D.IsPoweredUp): a whirlwind of the played character's own
    /// ability art circling it - Cluck's eggs, Horace's horseshoes, Billy's mud balls and so on - borrowed from the
    /// ability prefabs (Percy: his roll pose, Bessie: her slam burst). The pieces orbit on a flat ellipse at
    /// staggered heights, in front of the character on the near half and behind it on the far half, and blink for
    /// the last <see cref="WarningSeconds"/> so the player knows it is running out. A swap mid-power changes the art
    /// to the new character's. Added to the player by CharacterController2D.Awake.
    /// </summary>
    [RequireComponent(typeof(CharacterController2D))]
    public class CherrySwirl : MonoBehaviour
    {
        private const int Count = 6;
        private const float LapSeconds = 0.55f;
        private const float PieceSize = 0.55f;          // world units, the art's longest side
        private const float RadiusX = 1.05f, RadiusY = 0.3f;
        private const float Spread = 1.1f;              // height between the lowest and highest piece
        private const float SpinDegreesPerSecond = 420f;
        public const float WarningSeconds = 0.75f;

        private CharacterController2D _player;
        private SpriteRenderer _visual;
        private SpriteRenderer[] _pieces;
        private CharacterType? _artFor;
        private float _baseScale = 1f;

        private void Awake()
        {
            _player = GetComponent<CharacterController2D>();
            _visual = _player.Visual;
            var holder = new GameObject("CherrySwirl").transform;
            holder.SetParent(transform, false);
            _pieces = new SpriteRenderer[Count];
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject($"Piece{i}");
                go.transform.SetParent(holder, false);
                _pieces[i] = go.AddComponent<SpriteRenderer>();
                _pieces[i].enabled = false;
            }
        }

        private void LateUpdate()
        {
            bool on = _player.IsPoweredUp && !_player.IsDying;
            if (!on)
            {
                SetShown(false);
                return;
            }

            if (_artFor != _player.Character)
            {
                ApplyArt();
            }

            // Blink through the last moments (scaled time, like the power itself).
            float left = _player.PowerUpRemaining;
            bool shown = left > WarningSeconds || ((int)(Time.time * 10f) % 2 == 0);
            SetShown(shown);
            if (!shown) { return; }

            float size = _player.Data != null ? _player.Data.visualScale : 1f;
            Vector3 centre = _player.ColliderBounds.center + Vector3.up * 0.15f * size;
            int order = _visual != null ? _visual.sortingOrder : 10;
            float turn = Time.time / LapSeconds * Mathf.PI * 2f;
            for (int i = 0; i < Count; i++)
            {
                float angle = turn + i * Mathf.PI * 2f / Count;
                float depth = -Mathf.Sin(angle);   // +1 = the near side
                float height = (i / (float)(Count - 1) - 0.5f) * Spread + Mathf.Sin(Time.time * 3f + i) * 0.08f;
                var t = _pieces[i].transform;
                t.position = centre + new Vector3(Mathf.Cos(angle) * RadiusX * size, (Mathf.Sin(angle) * RadiusY + height) * size, 0f);
                t.localScale = Vector3.one * (_baseScale * (0.85f + 0.2f * depth) * size);
                t.rotation = Quaternion.Euler(0f, 0f, Time.time * SpinDegreesPerSecond + i * 60f);
                _pieces[i].sortingOrder = order + (depth >= 0f ? 2 : -1);
            }
        }

        private void SetShown(bool shown)
        {
            foreach (var p in _pieces)
            {
                p.enabled = shown && p.sprite != null;
            }
        }

        private void ApplyArt()
        {
            _artFor = _player.Character;
            Sprite sprite = SwirlSprite(_player);
            float longest = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            _baseScale = PieceSize / Mathf.Max(longest, 0.01f);
            foreach (var p in _pieces)
            {
                p.sprite = sprite;
            }
        }

        // The character's ability art, from the prefab its ability spawns (each carries its sprite).
        private static Sprite SwirlSprite(CharacterController2D player)
        {
            var prefabs = player.Prefabs;
            GameObject prefab = player.Character switch
            {
                CharacterType.Cluck => prefabs.egg,
                CharacterType.Bessie => prefabs.poundEffect,
                CharacterType.Woolly => prefabs.woolTuft,
                CharacterType.Ducky => prefabs.waterSpout,
                CharacterType.Horace => prefabs.horseshoe,
                CharacterType.Gerald => prefabs.featherGust,
                CharacterType.Billy => prefabs.mudBall,
                _ => null
            };
            if (prefab != null)
            {
                var renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null && renderer.sprite != null) { return renderer.sprite; }
            }

            // Percy (and any character without one): the ability pose, else the idle frame.
            var set = player.Data != null ? player.Data.spriteSet : null;
            if (set == null) { return null; }
            return set.abilityRight != null ? set.abilityRight : set.idleRight;
        }
    }
}
