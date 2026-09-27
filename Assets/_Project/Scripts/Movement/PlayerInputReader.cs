using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Keyboard (A/D, arrows, Space) and gamepad bindings, plus the HUD's on-screen touch buttons (left, right, jump),
    /// which write the static Touch* values below. Actions are created in code so no .inputactions asset is required.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _jump;
        private InputAction _ability;

        // On-screen buttons (HudScreen): held left/right, held jump, and a jump press waiting to be read.
        public static bool TouchLeftHeld, TouchRightHeld, TouchJumpHeld;
        private static bool _touchJumpPending;
        private static int _touchJumpFrame = -1;

        /// <summary>A touch jump press: true for the whole frame it is first read in.</summary>
        public static void PressTouchJump()
        {
            _touchJumpPending = true;
            TouchJumpHeld = true;
        }

        /// <summary>Lets go of every on-screen button (the HUD was hidden).</summary>
        public static void ReleaseTouch()
        {
            TouchLeftHeld = TouchRightHeld = TouchJumpHeld = false;
            _touchJumpPending = false;
        }

        private static bool TouchJumpPressedThisFrame
        {
            get
            {
                if (_touchJumpPending)
                {
                    _touchJumpPending = false;
                    _touchJumpFrame = Time.frameCount;
                }
                return _touchJumpFrame == Time.frameCount;
            }
        }

        private static float TouchMove => (TouchRightHeld ? 1f : 0f) - (TouchLeftHeld ? 1f : 0f);

        /// <summary>Horizontal axis, -1 to 1.</summary>
        public float Move => Mathf.Clamp(_move.ReadValue<float>() + TouchMove, -1f, 1f);

        public bool JumpHeld => _jump.IsPressed() || TouchJumpHeld;

        /// <summary>True only on the frame the ability button went down. Read from Update.</summary>
        public bool AbilityPressedThisFrame => _ability.WasPressedThisFrame();

        /// <summary>True only on the frame the jump button went down. Read from Update.</summary>
        public bool JumpPressedThisFrame => _jump.WasPressedThisFrame() | TouchJumpPressedThisFrame;

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick/x");

            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _ability = new InputAction("Ability", InputActionType.Button);
            _ability.AddBinding("<Keyboard>/leftShift");
            _ability.AddBinding("<Keyboard>/e");
            _ability.AddBinding("<Gamepad>/buttonWest");
        }

        private void OnEnable()
        {
            _move.Enable();
            _jump.Enable();
            _ability.Enable();
        }

        private void OnDisable()
        {
            _move.Disable();
            _jump.Disable();
            _ability.Disable();
        }

        private void OnDestroy()
        {
            _move.Dispose();
            _jump.Dispose();
            _ability.Dispose();
        }
    }
}
