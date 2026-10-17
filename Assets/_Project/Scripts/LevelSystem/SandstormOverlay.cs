using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Dustbowl Canyon's sandstorm (LevelBuilder.Sandstorm): a screen-filling haze of blowing dust that follows the
    /// camera, drifting sideways. Most of the time it is a light haze; every <see cref="period"/> seconds a gust
    /// thickens it for a few seconds so the path ahead is hard to read, then it clears. Purely visual (no collider),
    /// in front of everything. Scaled time.
    /// </summary>
    public class SandstormOverlay : MonoBehaviour
    {
        [Tooltip("The tiled dust art (full-rect sprite), scrolled sideways.")]
        public SpriteRenderer dust;
        [Tooltip("Seconds between gusts.")]
        public float period = 11f;
        [Tooltip("Seconds a gust lasts at its thickest.")]
        public float gustSeconds = 3f;
        [Tooltip("Haze opacity between gusts and at a gust's peak.")]
        public float calmAlpha = 0.12f, gustAlpha = 0.62f;
        [Tooltip("Sideways drift of the dust, units/s (and twice that in a gust).")]
        public float drift = 6f;
        [Tooltip("Width of one repeat of the dust art, units (the scroll wraps on it).")]
        public float repeat = 8f;

        private const float RampSeconds = 1.2f;
        private float _time, _offset;
        private Camera _camera;

        private void LateUpdate()
        {
            if (dust == null) { return; }
            if (_camera == null) { _camera = Camera.main; }
            if (_camera == null) { return; }

            _time += Time.deltaTime;
            float g = Gust(Mathf.Repeat(_time, period));
            _offset = Mathf.Repeat(_offset + drift * (1f + g) * Time.deltaTime, repeat);

            // Cover the view (with a repeat to spare for the scroll) wherever the camera is and however far it zooms.
            float height = _camera.orthographicSize * 2f + 1f;
            float width = height * _camera.aspect + repeat;
            Vector3 c = _camera.transform.position;
            transform.position = new Vector3(c.x - _offset + repeat * 0.5f, c.y, 0f);
            dust.size = new Vector2(width / dust.transform.localScale.x, height / dust.transform.localScale.y);
            dust.color = new Color(1f, 1f, 1f, Mathf.Lerp(calmAlpha, gustAlpha, g));
        }

        // 0 when calm, easing up to 1 for the gust and back down, near the end of each cycle.
        private float Gust(float t)
        {
            float start = period - gustSeconds - 2f * RampSeconds;
            if (t < start) { return 0f; }
            t -= start;
            if (t < RampSeconds) { return Mathf.SmoothStep(0f, 1f, t / RampSeconds); }
            t -= RampSeconds;
            if (t < gustSeconds) { return 1f; }
            t -= gustSeconds;
            return Mathf.SmoothStep(1f, 0f, t / RampSeconds);
        }
    }
}
