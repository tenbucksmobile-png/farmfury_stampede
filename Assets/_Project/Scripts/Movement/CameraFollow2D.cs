using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Simple follow camera: tracks the target's X with a smoothed look-ahead in the direction of
    /// travel; Y only moves when the target leaves a vertical dead zone, and never so high that the level floor
    /// under the target (tilemap ground, not obstacles) drops out of the bottom of the view. Runs in LateUpdate against the
    /// target's interpolated transform, so it stays smooth even though physics steps at a fixed rate.
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
        [SerializeField] private float floorSearchDistance = 12f;

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

        private void Awake()
        {
            _camera = GetComponent<Camera>();
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
            Vector3 p = transform.position;
            p.x = ClampX(target.position.x);
            p.y = ClampToFloor(_focusY + verticalOffset, target.position);
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

            Vector3 p = transform.position;
            p.x = Mathf.SmoothDamp(p.x, ClampX(targetPos.x + _lookAhead), ref _xVelocity, xSmoothTime);
            p.y = Mathf.SmoothDamp(p.y, ClampToFloor(_focusY + verticalOffset, targetPos), ref _yVelocity, ySmoothTime);
            transform.position = p;
        }

        // Caps the camera height so the first tilemap surface below the target (ground, platform, ledge - but
        // not rock/haybale/barrel obstacles, which are plain colliders) stays in view. Over a pit, no cap.
        private float ClampToFloor(float desiredY, Vector3 targetPos)
        {
            if (controller == null)
            {
                return desiredY;
            }

            var hits = Physics2D.RaycastAll(targetPos, Vector2.down, floorSearchDistance, controller.GroundMask);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponent<Tilemap>() != null)
                {
                    return Mathf.Min(desiredY, hit.point.y + _camera.orthographicSize - floorMargin);
                }
            }
            return desiredY;
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
