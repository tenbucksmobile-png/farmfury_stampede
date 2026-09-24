using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Purely decorative background mover (the biplane over the farm): flies horizontally at a constant speed
    /// with a gentle bob and wraps from one end of the level to the other. No collider; it never interacts
    /// with gameplay. Bounds are baked in by LevelBuilder in the level's local space.
    /// </summary>
    public class SkyDrifter : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 100f;
        [Tooltip("Units per second; negative flies left.")]
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobPeriod = 3f;

        private float _baseY;

        public void Configure(float fromX, float toX, float unitsPerSecond)
        {
            minX = fromX;
            maxX = toX;
            speed = unitsPerSecond;
        }

        private void Start()
        {
            _baseY = transform.localPosition.y;
        }

        private void Update()
        {
            Vector3 p = transform.localPosition;
            p.x += speed * Time.deltaTime;
            if (speed > 0f && p.x > maxX) { p.x = minX; }
            else if (speed < 0f && p.x < minX) { p.x = maxX; }
            p.y = _baseY + Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(bobPeriod, 0.01f)) * bobHeight;
            transform.localPosition = p;
        }
    }
}
