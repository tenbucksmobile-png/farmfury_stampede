using UnityEngine;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Picks the visual's sprite from the controller's live state (defeated, airborne, running, idle) using the
    /// character's CharacterSpriteSet. Does nothing for characters without a set, which keep their placeholder
    /// sprite. Sits on the same object as the SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        private CharacterController2D _controller;
        private SpriteRenderer _renderer;
        private float _runTime;
        private const float AirborneGrace = 0.08f;

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController2D>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            var set = _controller.Data != null ? _controller.Data.spriteSet : null;
            if (set == null)
            {
                return;
            }

            bool right = _controller.Facing >= 0;
            Sprite idle = right ? set.idleRight : set.idleLeft;
            Sprite chosen;

            if (_controller.IsDying)
            {
                chosen = set.defeat != null ? set.defeat : idle;
            }
            // Rising, or clearly airborne: a one-step ground-check blip mustn't flash the (bigger) jump frame.
            else if (!_controller.IsGrounded && (_controller.Velocity.y > 0.1f || _controller.AirTime > AirborneGrace))
            {
                Sprite jump = right ? set.jumpRight : set.jumpLeft;
                chosen = jump != null ? jump : idle;
            }
            else if (Mathf.Abs(_controller.Velocity.x) > 0.5f)
            {
                Sprite[] frames = right ? set.runRight : set.runLeft;
                if (frames != null && frames.Length > 0)
                {
                    float speedScale = Mathf.Clamp(Mathf.Abs(_controller.Velocity.x) / Mathf.Max(0.1f, _controller.MoveSpeed), 0.4f, 1f);
                    _runTime += Time.deltaTime * set.runFramesPerSecond * speedScale;
                    chosen = frames[(int)_runTime % frames.Length];
                }
                else
                {
                    chosen = idle;
                }
            }
            else
            {
                _runTime = 0f;
                chosen = idle;
            }

            if (chosen != null)
            {
                _renderer.sprite = chosen;
            }
        }
    }
}
