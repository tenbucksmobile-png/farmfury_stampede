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
        private InputAction _ability;

        /// <summary>Horizontal axis, -1 to 1.</summary>
        public float Move => _move.ReadValue<float>();

        public bool JumpHeld => _jump.IsPressed();

        /// <summary>True only on the frame the ability button went down. Read from Update.</summary>
        public bool AbilityPressedThisFrame => _ability.WasPressedThisFrame();

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
