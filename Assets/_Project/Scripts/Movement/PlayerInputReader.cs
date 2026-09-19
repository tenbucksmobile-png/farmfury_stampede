using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Keyboard (A/D, arrows, Space) and gamepad bindings for editor testing. Touch controls arrive
    /// with the HUD in Phase 5. Actions are created in code so no .inputactions asset is required.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _jump;

        /// <summary>Horizontal axis, -1 to 1.</summary>
        public float Move => _move.ReadValue<float>();

        public bool JumpHeld => _jump.IsPressed();

        /// <summary>True only on the frame the jump button went down. Read from Update.</summary>
        public bool JumpPressedThisFrame => _jump.WasPressedThisFrame();

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
        }

        private void OnEnable()
        {
            _move.Enable();
            _jump.Enable();
        }

        private void OnDisable()
        {
            _move.Disable();
            _jump.Disable();
        }

        private void OnDestroy()
        {
            _move.Dispose();
            _jump.Dispose();
        }
    }
}
