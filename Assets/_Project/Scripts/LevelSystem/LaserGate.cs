using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Crop Factory's laser gate (LevelBuilder.LaserGate): an emitter post on the floor firing a beam straight up. The
    /// beam switches on and off in a steady rhythm, flickering faintly just before it fires; touching it while it is on
    /// costs a life. Off, it is harmless and the player walks (or jumps) through. Scaled time.
    /// </summary>
    public class LaserGate : MonoBehaviour
    {
        [Tooltip("The beam art, stretched over the beam's height (its alpha shows the state).")]
        public SpriteRenderer beam;
        [Tooltip("Beam height above the post's base, in units.")]
        public float height = 7f;
        [Tooltip("Half the beam's lethal width, in units.")]
        public float halfWidth = 0.3f;
        [Tooltip("Seconds for one on/off cycle.")]
        public float period = 3f;
        [Tooltip("Fraction of the cycle the beam is on.")]
        [Range(0.1f, 0.9f)] public float onFraction = 0.5f;
        [Tooltip("Where in the cycle it starts, 0-1.")]
        [Range(0f, 1f)] public float phase;

        // Seconds of faint flicker before the beam fires.
        private const float WarnSeconds = 0.45f;

        private float _time;
        private int _playerMask;

        private void Awake()
        {
            _time = phase * period;
            _playerMask = LayerMask.GetMask("Player");
            Refresh();
        }

        // The cycle: off first, then on for its last onFraction.
        private float Into => period > 0f ? Mathf.Repeat(_time, period) : 0f;
        private float OnAt => period * (1f - onFraction);
        private bool IsOn => Into >= OnAt;
        private bool IsWarning => !IsOn && Into >= OnAt - WarnSeconds;

        private void FixedUpdate()
        {
            _time += Time.fixedDeltaTime;
            if (!IsOn || GameManager.Instance == null) { return; }

            Vector2 centre = (Vector2)transform.position + Vector2.up * (height * 0.5f);
            var hit = Physics2D.OverlapBox(centre, new Vector2(halfWidth * 2f, height), 0f, _playerMask);
            if (hit != null && hit.GetComponentInParent<CharacterController2D>() != null)
            {
                GameManager.Instance.RespawnPlayer();
            }
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (beam == null) { return; }
            float alpha = IsOn ? 0.85f + 0.15f * Mathf.Sin(Time.time * 30f)
                : IsWarning ? (Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.3f : 0.08f)
                : 0f;
            beam.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
