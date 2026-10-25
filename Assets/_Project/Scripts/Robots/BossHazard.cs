using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// What a world boss throws at the player (CommanderBoss attacks): a shockwave that runs along the ground from a
    /// slam until a wall, a ledge edge or its range, or an energy orb lobbed in an arc that bursts on the ground.
    /// Touching either costs a life (not while invulnerable). Made in code and drawn with the world's art
    /// (Sprites/UI/BossAttacks, via CommanderBoss), or a generated glow in the hazard colour without it; cleared when
    /// the player respawns, the level ends or the boss goes. Scaled time, so it waits while paused.
    /// </summary>
    public class BossHazard : MonoBehaviour
    {
        private enum Mode { Wave, Orb }

        private static readonly List<BossHazard> Live = new();
        private static Sprite _glow;

        private Mode _mode;
        private Vector2 _position, _velocity;
        private float _gravity, _range, _travelled, _age;
        private int _direction;
        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private float _artHeight;
        private int _groundMask, _playerMask;

        // Contact sizes; the art is drawn a little bigger (ArtWaveWidth, ArtOrbSize) so a near miss reads as one.
        private const float WaveWidth = 0.9f, WaveHeight = 0.9f, OrbRadius = 0.35f, OrbLifetime = 4f;
        private const float ArtWaveWidth = 1.7f, ArtOrbSize = 1f, OrbSpinDegrees = 360f;

        /// <summary>A shockwave starting at a point on the ground and running one way (+1 right, -1 left).</summary>
        public static void Shockwave(Vector2 groundPoint, int direction, float speed, float range, Color color, Sprite art = null)
        {
            var h = Create("BossShockwave", art, color, ArtWaveWidth, WaveHeight * 1.1f);
            h._mode = Mode.Wave;
            h._position = groundPoint;
            h._direction = direction >= 0 ? 1 : -1;
            h._velocity = new Vector2(h._direction * speed, 0f);
            h._range = range;
            h.Place();
        }

        /// <summary>An orb fired from a point with a starting velocity, falling under 'gravity'.</summary>
        public static void Orb(Vector2 from, Vector2 velocity, float gravity, Color color, Sprite art = null)
        {
            var h = Create("BossOrb", art, color, ArtOrbSize, ArtOrbSize);
            h._mode = Mode.Orb;
            h._position = from;
            h._velocity = velocity;
            h._gravity = gravity;
            h.Place();
        }

        /// <summary>Removes every live hazard (the boss is gone or the level ended).</summary>
        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i] != null) { Destroy(Live[i].gameObject); }
            }
            Live.Clear();
        }

        // Art drawn 'width' wide at its own aspect (centre-pivoted), or the glow stretched to width x height.
        private static BossHazard Create(string name, Sprite art, Color color, float width, float height)
        {
            var go = new GameObject(name);
            var h = go.AddComponent<BossHazard>();
            h._renderer = go.AddComponent<SpriteRenderer>();
            h._renderer.sortingOrder = 12;   // in front of the robots and tiles
            if (art != null)
            {
                h._renderer.sprite = art;
                float k = width / Mathf.Max(art.bounds.size.x, 0.01f);
                h._baseScale = new Vector3(k, k, 1f);
                h._artHeight = art.bounds.size.y * k;
            }
            else
            {
                h._renderer.sprite = Glow();   // 1 unit across
                h._renderer.color = color;
                h._baseScale = new Vector3(width, height, 1f);
                h._artHeight = height;
            }
            go.transform.localScale = h._baseScale;
            h._groundMask = LayerMask.GetMask("Ground");
            h._playerMask = LayerMask.GetMask("Player");
            Live.Add(h);
            return h;
        }

        private void OnDestroy()
        {
            Live.Remove(this);
        }

        private void FixedUpdate()
        {
            var game = GameManager.Instance;
            if (game == null || game.CurrentState != GameState.Playing)
            {
                if (game == null || game.CurrentState != GameState.Paused) { Destroy(gameObject); }
                return;
            }
            if (game.BossVictoryPending || (LevelLoader.Instance != null && LevelLoader.Instance.IsRespawning))
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.fixedDeltaTime;
            _age += dt;
            bool done = _mode == Mode.Wave ? StepWave(dt) : StepOrb(dt);
            Place();
            if (HitPlayer() || done)
            {
                Destroy(gameObject);
            }
        }

        // Runs along the ground; stops at a wall, at a ledge edge (no ground just ahead) or after its range.
        private bool StepWave(float dt)
        {
            float step = Mathf.Abs(_velocity.x) * dt;
            var dir = new Vector2(_direction, 0f);
            if (Physics2D.Raycast(_position + Vector2.up * 0.4f, dir, step + 0.3f, _groundMask).collider != null) { return true; }
            var ahead = new Vector2(_position.x + _direction * 0.4f, _position.y + 0.3f);
            if (Physics2D.Raycast(ahead, Vector2.down, 0.8f, _groundMask).collider == null) { return true; }
            _position.x += _direction * step;
            _travelled += step;
            return _travelled >= _range;
        }

        // Flies in an arc; bursts on anything solid or after its lifetime.
        private bool StepOrb(float dt)
        {
            _velocity.y -= _gravity * dt;
            _position += _velocity * dt;
            return _age > OrbLifetime || Physics2D.OverlapCircle(_position, OrbRadius * 0.7f, _groundMask) != null;
        }

        private bool HitPlayer()
        {
            Collider2D hit = _mode == Mode.Wave
                ? Physics2D.OverlapBox(_position + Vector2.up * WaveHeight * 0.5f, new Vector2(WaveWidth, WaveHeight), 0f, _playerMask)
                : Physics2D.OverlapCircle(_position, OrbRadius, _playerMask);
            var player = hit != null ? hit.GetComponentInParent<CharacterController2D>() : null;
            if (player == null || player.IsInvulnerable || player.IsPoweredUp) { return false; }
            GameManager.Instance.RespawnPlayer();
            return true;
        }

        private void Place()
        {
            if (_mode == Mode.Wave)
            {
                // A pulsing hump on the ground, facing the way it runs, fading over its last stretch.
                transform.position = new Vector3(_position.x, _position.y + _artHeight * 0.45f, 0f);
                float pulse = 1f + Mathf.Sin(_age * 30f) * 0.12f;
                transform.localScale = new Vector3(_baseScale.x * _direction, _baseScale.y * pulse, 1f);
                if (_renderer != null)
                {
                    var c = _renderer.color;
                    c.a = Mathf.Clamp01((_range - _travelled) / 1.5f);
                    _renderer.color = c;
                }
            }
            else
            {
                transform.position = new Vector3(_position.x, _position.y, 0f);
                float pulse = 1f + Mathf.Sin(_age * 20f) * 0.1f;
                transform.localScale = _baseScale * pulse;
                transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(_velocity.x) * _age * OrbSpinDegrees);
            }
        }

        // A soft round glow, white so it takes the hazard's colour, 1 unit across.
        private static Sprite Glow()
        {
            if (_glow != null) { return _glow; }
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(1f - d);
                    float a = Mathf.Clamp01(core * 1.8f);
                    byte white = (byte)(Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(core * 2.5f - 1f)) * 255f);
                    pixels[y * size + x] = new Color32(white, white, white, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _glow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _glow;
        }
    }
}
