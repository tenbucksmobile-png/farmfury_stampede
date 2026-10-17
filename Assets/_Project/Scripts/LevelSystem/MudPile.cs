using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The mud pile Billy's slung mud leaves on the ground (MudBall): a sticky trap that knocks out the first robot to
    /// walk into it, then splashes away; unused, it dries up and fades after <see cref="lifetime"/> seconds. Harmless
    /// to the player. Pooled; feet-pivoted (placed on the ground point).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class MudPile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float fadeSeconds = 0.5f;
        [SerializeField] private SpriteRenderer visual;

        private float _time;
        private bool _spent;
        private Vector3 _scale;

        private void Awake()
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            if (visual != null) { _scale = visual.transform.localScale; }
        }

        private void OnEnable()
        {
            _time = 0f;
            _spent = false;
            if (visual != null)
            {
                visual.color = Color.white;
                visual.transform.localScale = _scale;
            }
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (visual != null)
            {
                // Splats down as it lands, then wobbles a little; fades out at the end.
                float settle = Mathf.Clamp01(_time / 0.15f);
                float wobble = 1f + 0.04f * Mathf.Sin(_time * 9f) * (1f - settle * 0.5f);
                visual.transform.localScale = new Vector3(_scale.x * Mathf.Lerp(0.6f, 1f, settle) * wobble, _scale.y * Mathf.Lerp(1.4f, 1f, settle) / wobble, _scale.z);
                float fade = Mathf.Clamp01((lifetime - _time) / fadeSeconds);
                visual.color = new Color(1f, 1f, 1f, fade);
            }
            if (_time >= lifetime && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_spent || _time >= lifetime - fadeSeconds)
            {
                return;
            }
            var robot = other.GetComponentInParent<RobotController>();
            if (robot != null && robot.DefeatByAbility())
            {
                _spent = true;
                _time = Mathf.Max(_time, lifetime - fadeSeconds);   // splashes away: fade out from here
            }
        }
    }
}
