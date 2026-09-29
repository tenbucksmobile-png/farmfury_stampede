using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Follow camera: tracks the target's X with a smoothed look-ahead in the direction of travel; Y only moves when
    /// the target leaves a vertical dead zone. The level's ground under the target always stays in view: the nearest
    /// Ground / BreakableFloor tilemap surface below it (ledges, platforms and stone blocks on the Platforms tilemap
    /// and obstacle colliders are looked through), kept floorMargin above the bottom edge, with topMargin of headroom
    /// above the target. When the normal view can't hold both - the player high on a ledge - the camera zooms out
    /// (up to maxOrthographicSize) instead of losing the ground, and zooms back in when they come down.
    /// The reference is always the ground nearest below the player, so underground sections (a hollow under a
    /// Breakable Floor, lower or secret levels) frame their own floor the same way. Over a pit the last ground seen is
    /// kept; falling below it, the camera simply follows the player.
    /// Runs in LateUpdate against the target's interpolated transform, so it stays smooth even though physics steps at
    /// a fixed rate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private CharacterController2D controller;

        [Header("Horizontal")]
        [SerializeField] private float lookAheadDistance = 2.5f;
        [SerializeField] private float lookAheadSmoothTime = 0.35f;
        [SerializeField] private float xSmoothTime = 0.12f;

        [Header("Vertical")]
        [Tooltip("Half-height of the band the target can move in before the camera follows.")]
        [SerializeField] private float verticalDeadZone = 1.5f;
        [Tooltip("How far above the target the camera centres, so more of the path above is visible.")]
        [SerializeField] private float verticalOffset = 2.5f;
        [SerializeField] private float ySmoothTime = 0.25f;
        [Tooltip("The level floor under the target (ignoring obstacles) stays at least this far above the bottom edge.")]
        [SerializeField] private float floorMargin = 2f;
        [Tooltip("How far below the target to look for the level floor.")]
        [SerializeField] private float floorSearchDistance = 30f;
        [Tooltip("Headroom kept above the target (its feet) when zooming out to keep the ground in view.")]
        [SerializeField] private float topMargin = 2.5f;

        [Header("Zoom")]
        [Tooltip("The normal view (the camera's orthographic size when setup runs is used if this is 0).")]
        [SerializeField] private float baseOrthographicSize;
        [Tooltip("Furthest the camera zooms out to keep both the player and the ground in view.")]
        [SerializeField] private float maxOrthographicSize = 11f;
        [SerializeField] private float zoomSmoothTime = 0.35f;

        // Tilemaps that count as the level's ground (not the Platforms layer of ledges and floating platforms).
        private static readonly string[] FloorTilemaps = { "Ground", "BreakableFloor", "Ice" };

        [Header("Level Bounds (X)")]
        [SerializeField] private bool useXBounds;
        [SerializeField] private float minX;
        [SerializeField] private float maxX;

        private Camera _camera;
        private float _lookAhead;
        private float _lookAheadVelocity;
        private float _xVelocity;
        private float _yVelocity;
        private float _focusY;
        private float _lastDirection = 1f;
        private float _sizeVelocity;

        /// <summary>The furthest this camera zooms out (background layers are sized for it).</summary>
        public float MaxOrthographicSize => maxOrthographicSize;
        private float? _floorY;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (baseOrthographicSize <= 0f)
            {
                baseOrthographicSize = _camera.orthographicSize;
            }
        }

        private void Start()
        {
            SnapToTarget();
        }

        public void SetTarget(Transform newTarget, CharacterController2D newController)
        {
            target = newTarget;
            controller = newController;
            SnapToTarget();
        }

        /// <summary>Sets the horizontal extent the camera may show (per loaded level).</summary>
        public void SetBounds(float newMinX, float newMaxX)
        {
            useXBounds = true;
            minX = newMinX;
            maxX = newMaxX;
        }

        /// <summary>Jumps the camera straight to the target with no smoothing (level load, respawn).</summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            _focusY = target.position.y;
            _lookAhead = 0f;
            _lookAheadVelocity = 0f;
            _xVelocity = 0f;
            _yVelocity = 0f;
            _sizeVelocity = 0f;
            _floorY = null;
            var (y, size) = Frame(_focusY + verticalOffset, target.position);
            _camera.orthographicSize = size;
            Vector3 p = transform.position;
            p.x = ClampX(target.position.x);
            p.y = y;
            transform.position = p;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPos = target.position;

            float vx = controller != null ? controller.Velocity.x : 0f;
            if (Mathf.Abs(vx) > 0.5f)
            {
                _lastDirection = Mathf.Sign(vx);
            }
            float lookAheadTarget = Mathf.Abs(vx) > 0.5f ? _lastDirection * lookAheadDistance : 0f;
            _lookAhead = Mathf.SmoothDamp(_lookAhead, lookAheadTarget, ref _lookAheadVelocity, lookAheadSmoothTime);

            if (targetPos.y > _focusY + verticalDeadZone)
            {
                _focusY = targetPos.y - verticalDeadZone;
            }
            else if (targetPos.y < _focusY - verticalDeadZone)
            {
                _focusY = targetPos.y + verticalDeadZone;
            }

            var (y, size) = Frame(_focusY + verticalOffset, targetPos);
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, size, ref _sizeVelocity, zoomSmoothTime);

            Vector3 p = transform.position;
            p.x = Mathf.SmoothDamp(p.x, ClampX(targetPos.x + _lookAhead), ref _xVelocity, xSmoothTime);
            p.y = Mathf.SmoothDamp(p.y, y, ref _yVelocity, ySmoothTime);
            transform.position = p;
        }

        // The camera height and zoom that keep the ground under the target floorMargin above the bottom edge and
        // topMargin of space above the target: the normal size when it fits (the desired height clamped into the
        // band that satisfies both), otherwise zoomed out just enough, up to the maximum (then the player wins).
        private (float y, float size) Frame(float desiredY, Vector3 targetPos)
        {
            float floor = FloorBelow(targetPos) ?? float.NaN;
            if (float.IsNaN(floor) || targetPos.y < floor - 0.5f)
            {
                return (desiredY, baseOrthographicSize);   // no ground known, or falling below it: just follow
            }

            float bottom = floor - floorMargin;             // lowest point that must be on screen
            float top = targetPos.y + topMargin;            // highest point that must be on screen
            float needed = (top - bottom) * 0.5f;
            if (needed <= baseOrthographicSize)
            {
                float size = baseOrthographicSize;
                return (Mathf.Clamp(desiredY, top - size, bottom + size), size);
            }

            float zoomed = Mathf.Min(needed, maxOrthographicSize);
            return (zoomed < needed ? top - zoomed : (top + bottom) * 0.5f, zoomed);
        }

        // Height of the nearest ground (Ground / BreakableFloor tilemap) below the target, looking through platforms,
        // ledges and obstacles. Over a pit, the last ground seen.
        private float? FloorBelow(Vector3 targetPos)
        {
            if (controller == null)
            {
                return null;
            }

            var hits = Physics2D.RaycastAll(targetPos + Vector3.up * 0.1f, Vector2.down, floorSearchDistance, controller.GroundMask);
            foreach (var hit in hits)   // sorted nearest first
            {
                if (hit.collider.GetComponent<Tilemap>() != null && System.Array.IndexOf(FloorTilemaps, hit.collider.name) >= 0)
                {
                    _floorY = hit.point.y;
                    break;
                }
            }
            return _floorY;
        }

        private float ClampX(float x)
        {
            if (!useXBounds)
            {
                return x;
            }

            float halfWidth = _camera.orthographicSize * _camera.aspect;
            float lo = minX + halfWidth;
            float hi = maxX - halfWidth;
            return lo > hi ? (minX + maxX) * 0.5f : Mathf.Clamp(x, lo, hi);
        }
    }
}
