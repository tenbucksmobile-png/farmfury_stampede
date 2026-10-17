using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Crop Factory's conveyor belt (LevelBuilder.Conveyor): a solid one-tile-thick span on the Ground layer whose
    /// surface carries whoever stands on it at <see cref="speed"/> units/s (positive = to the right).
    /// CharacterController2D adds the push while grounded on it, so running with the belt is faster and against it
    /// slower (the player's 8 u/s always beats it). The belt art scrolls at the same speed inside a sprite mask.
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        [Tooltip("Belt speed in units/s; positive carries the player right, negative left.")]
        public float speed = 2.5f;
        [Tooltip("The tiled belt art (optional), scrolled along with the belt.")]
        public SpriteRenderer belt;
        [Tooltip("Width of one repeat of the belt art, in units (the scroll wraps on it).")]
        public float repeat = 1f;

        private float _offset;
        private Vector3 _beltStart;

        private void Awake()
        {
            if (belt != null) { _beltStart = belt.transform.localPosition; }
        }

        private void Update()
        {
            if (belt == null || repeat <= 0f) { return; }
            _offset = Mathf.Repeat(_offset + speed * Time.deltaTime, repeat);
            belt.transform.localPosition = _beltStart + new Vector3(_offset - repeat * 0.5f, 0f, 0f);
        }
    }
}
