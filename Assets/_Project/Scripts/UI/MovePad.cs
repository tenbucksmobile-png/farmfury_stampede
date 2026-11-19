using System.Collections.Generic;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The HUD's left/right run controls as one touch zone (2026-11-19). The two arrow images are only art; this
    /// invisible pad behind them is larger (out to the screen's left and bottom edges and a margin round the arrows)
    /// and decides left or right from where each finger is *now*, on either side of the gap between the arrows.
    /// So a thumb that drifts while running keeps running (it used to release the moment it slid off the 150 px
    /// button - worst for the left arrow, which sits by the screen edge where the thumb rests and drifts), a slide
    /// from one arrow to the other turns round without lifting, and a touch just outside the art still counts.
    /// Drag events keep coming to the pad after a finger leaves it, until it lifts; with no finger down at all it lets go.
    /// </summary>
    public class MovePad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler
    {
        /// <summary>The right arrow's art: a finger right of its left edge (less half the gap) means right.</summary>
        public RectTransform rightArrow;
        public float gap;

        private readonly Dictionary<int, bool> _fingers = new();   // pointer id -> holding right

        public void OnPointerDown(PointerEventData e) => Track(e);
        public void OnBeginDrag(PointerEventData e) => Track(e);
        public void OnDrag(PointerEventData e) => Track(e);

        public void OnPointerUp(PointerEventData e)
        {
            _fingers.Remove(e.pointerId);
            Apply();
        }

        private void Track(PointerEventData e)
        {
            if (rightArrow == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rightArrow, e.position, e.pressEventCamera, out var local);
            _fingers[e.pointerId] = local.x >= rightArrow.rect.xMin - gap * 0.5f;
            Apply();
        }

        private void Apply()
        {
            bool left = false, right = false;
            foreach (bool isRight in _fingers.Values)
            {
                if (isRight) right = true; else left = true;
            }
            PlayerInputReader.TouchLeftHeld = left;
            PlayerInputReader.TouchRightHeld = right;
        }

        // Never runs by itself: if no finger or mouse button is down any more (a lift the UI missed - a cancelled
        // touch, the app losing focus), let go.
        private void Update()
        {
            if (_fingers.Count > 0 && !AnyPointerDown()) { _fingers.Clear(); Apply(); }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _fingers.Count > 0) { _fingers.Clear(); Apply(); }
        }

        private static bool AnyPointerDown()
        {
            var screen = Touchscreen.current;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (touch.press.isPressed) return true;
                }
            }
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        private void OnDisable()
        {
            if (_fingers.Count == 0) return;
            _fingers.Clear();
            Apply();
        }
    }
}
