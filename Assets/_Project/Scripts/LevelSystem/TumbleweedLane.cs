using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Dustbowl Canyon's tumbleweeds (LevelBuilder.Tumbleweeds): along a flat lane of ground a tumbleweed rolls in from
    /// the right end towards the left end, bouncing and spinning, then is blown away and the next one comes, once per
    /// <see cref="period"/> seconds. Touching one costs a life; jump over it. Scaled time, so it waits while paused.
    /// </summary>
    public class TumbleweedLane : MonoBehaviour
    {
        [Tooltip("The tumbleweed art, a child moved along the lane.")]
        public Transform weed;
        [Tooltip("Lane length in units, leftwards from this object's position (the right end).")]
        public float length = 12f;
        [Tooltip("Rolling speed, units/s.")]
        public float speed = 4.5f;
        [Tooltip("Seconds between tumbleweeds (must be longer than one roll).")]
        public float period = 4f;
        [Tooltip("Where in the cycle it starts, 0-1.")]
        [Range(0f, 1f)] public float phase;
        [Tooltip("Radius of the tumbleweed (art and contact), units.")]
        public float radius = 0.55f;

        private const float BounceHeight = 0.55f, BounceLength = 2.2f;

        private float _time;
        private int _playerMask;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _time = phase * period;
            _playerMask = LayerMask.GetMask("Player");
            if (weed != null) { _renderer = weed.GetComponent<SpriteRenderer>(); }
            Place();
        }

        private float RollSeconds => length / Mathf.Max(speed, 0.1f);

        private void FixedUpdate()
        {
            _time += Time.fixedDeltaTime;
            Place();
            if (weed == null || !weed.gameObject.activeSelf || GameManager.Instance == null) { return; }

            var hit = Physics2D.OverlapCircle(weed.position, radius * 0.85f, _playerMask);
            if (hit != null && hit.GetComponentInParent<CharacterController2D>() != null)
            {
                GameManager.Instance.RespawnPlayer();
            }
        }

        // Rolling for the first RollSeconds of each cycle, gone for the rest.
        private void Place()
        {
            if (weed == null) { return; }
            float t = period > 0f ? Mathf.Repeat(_time, period) : 0f;
            bool rolling = t < RollSeconds;
            weed.gameObject.SetActive(rolling);
            if (!rolling) { return; }

            float travelled = t * speed;
            float hop = Mathf.Abs(Mathf.Sin(Mathf.PI * travelled / BounceLength)) * BounceHeight;
            weed.localPosition = new Vector3(-travelled, radius + hop, 0f);
            weed.localRotation = Quaternion.Euler(0f, 0f, travelled / radius * Mathf.Rad2Deg);   // rolls leftwards
            if (_renderer != null)
            {
                // Fade in and out at the ends of the lane, so it is blown in and away rather than popping.
                float edge = Mathf.Min(travelled, length - travelled);
                _renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(edge / 1.2f));
            }
        }
    }
}
