using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Dustbowl Canyon's tumbleweeds (LevelBuilder.Tumbleweeds): once per <see cref="period"/> seconds a tumbleweed is
    /// blown in from beyond the right edge of the screen and rolls leftwards, bouncing and spinning, over the ground
    /// (following steps, dropping off ledges and into pits) until it has left the screen on the left, so none ever
    /// appears or vanishes in view (since 2026-10-25; was: it rolled only along the lane and faded in and out at its
    /// ends). Several can be rolling at once. Touching one costs a life only along the lane itself (its validated
    /// stretch, kept clear of the start and checkpoints); outside it the tumbleweed is just blowing past. Scaled
    /// time, so it waits while paused.
    /// </summary>
    public class TumbleweedLane : MonoBehaviour
    {
        [Tooltip("The tumbleweed art, a child; copied for each tumbleweed rolling at once.")]
        public Transform weed;
        [Tooltip("Lane length in units, leftwards from this object's position (the right end).")]
        public float length = 12f;
        [Tooltip("Rolling speed, units/s.")]
        public float speed = 4.5f;
        [Tooltip("Seconds between tumbleweeds.")]
        public float period = 4f;
        [Tooltip("Where in the cycle it starts, 0-1.")]
        [Range(0f, 1f)] public float phase;
        [Tooltip("Radius of the tumbleweed (art and contact), units.")]
        public float radius = 0.55f;

        private const float BounceHeight = 0.55f, BounceLength = 2.2f;
        // A tumbleweed is only sent while the lane is within this far of the camera's view, and is put away once
        // this far beyond the screen's left edge or below its bottom.
        private const float ActiveMargin = 30f, OffScreen = 1.5f;
        // Steps up to this high are hopped onto; a taller wall removes it (only ever met off to the left, as lanes
        // lie on open ground). Falls under Gravity.
        private const float MaxStepUp = 3.2f, Gravity = 22f;

        private sealed class Weed
        {
            public Transform transform;
            public float x, groundY, fallY, fallSpeed, travelled;
            public bool falling;
        }

        private readonly List<Weed> _rolling = new();
        private readonly Stack<Transform> _spare = new();
        private float _time;
        private int _playerMask, _groundMask;

        private float RightEnd => transform.position.x;
        private float LeftEnd => transform.position.x - length;

        private void Awake()
        {
            _time = phase * period;
            _playerMask = LayerMask.GetMask("Player");
            _groundMask = LayerMask.GetMask("Ground");
            if (weed != null)
            {
                weed.gameObject.SetActive(false);
                _spare.Push(weed);
            }
        }

        private void OnDisable()
        {
            for (int i = _rolling.Count - 1; i >= 0; i--) { Retire(i); }
        }

        private void FixedUpdate()
        {
            if (weed == null || GameManager.Instance == null) { return; }
            float dt = Time.fixedDeltaTime;

            float before = _time;
            _time += dt;
            if (period > 0f && Mathf.Floor(_time / period) > Mathf.Floor(before / period)) { TrySend(); }

            var view = CameraView();
            for (int i = _rolling.Count - 1; i >= 0; i--)
            {
                var w = _rolling[i];
                if (!Roll(w, dt) || w.x < view.xMin - OffScreen - radius || w.fallY < view.yMin - OffScreen - radius)
                {
                    Retire(i);
                    continue;
                }
                Place(w, view);
                if (w.x > LeftEnd - 1f && w.x < RightEnd + 1f && HitsPlayer(w))
                {
                    GameManager.Instance.RespawnPlayer();
                }
            }
        }

        // A new tumbleweed just beyond the right edge of the screen (or at the lane's right end, when that is further
        // right), if the lane is anywhere near the view.
        private void TrySend()
        {
            var view = CameraView();
            if (RightEnd < view.xMin - ActiveMargin || LeftEnd > view.xMax + ActiveMargin) { return; }

            float x = Mathf.Max(RightEnd, view.xMax + OffScreen + radius);
            if (!GroundAt(x, transform.position.y + MaxStepUp + 6f, 40f, out float ground)) { ground = transform.position.y; }

            var t = _spare.Count > 0 ? _spare.Pop() : Instantiate(weed, weed.parent);
            t.gameObject.SetActive(true);
            var w = new Weed { transform = t, x = x, groundY = ground, fallY = ground };
            _rolling.Add(w);
            Place(w, view);
        }

        private void Retire(int index)
        {
            var t = _rolling[index].transform;
            _rolling.RemoveAt(index);
            if (t == null) { return; }
            t.gameObject.SetActive(false);
            _spare.Push(t);
        }

        // Leftwards over whatever ground is there: up small steps, falling off ledges. False = it met a tall wall.
        private bool Roll(Weed w, float dt)
        {
            float step = speed * dt;
            float nextX = w.x - step;

            if (w.falling)
            {
                w.fallSpeed += Gravity * dt;
                float nextY = w.fallY - w.fallSpeed * dt;
                if (GroundAt(nextX, w.fallY + 0.2f, w.fallY - nextY + 0.4f, out float landing) && landing >= nextY)
                {
                    w.falling = false;
                    w.groundY = w.fallY = landing;
                    w.fallSpeed = 0f;
                }
                else
                {
                    w.fallY = nextY;
                }
            }
            else if (Physics2D.Raycast(new Vector2(w.x, w.groundY + radius), Vector2.left, step + radius, _groundMask).collider != null)
            {
                // A step in its way (a floating ledge overhead is not: nothing at its own height blocks it).
                if (!GroundAt(nextX - radius, w.groundY + MaxStepUp + 0.1f, MaxStepUp + 0.1f, out float stepTop)
                    || stepTop <= w.groundY + 0.05f)
                {
                    return false;   // a wall taller than it can hop
                }
                w.groundY = w.fallY = stepTop;
            }
            else if (GroundAt(nextX, w.groundY + 0.3f, 0.7f, out float ground))
            {
                w.groundY = w.fallY = ground;
            }
            else
            {
                w.falling = true;   // off a ledge or over a pit: drops onto whatever is below, or out of view
                w.fallY = w.groundY;
            }

            w.x = nextX;
            w.travelled += step;
            return true;
        }

        private void Place(Weed w, Rect view)
        {
            float hop = w.falling ? 0f : Mathf.Abs(Mathf.Sin(Mathf.PI * w.travelled / BounceLength)) * BounceHeight;
            w.transform.position = new Vector3(w.x, w.fallY + radius + hop, transform.position.z);
            w.transform.localRotation = Quaternion.Euler(0f, 0f, w.travelled / radius * Mathf.Rad2Deg);   // rolls leftwards
        }

        private bool HitsPlayer(Weed w)
        {
            var hit = Physics2D.OverlapCircle(w.transform.position, radius * 0.85f, _playerMask);
            return hit != null && hit.GetComponentInParent<CharacterController2D>() != null;
        }

        // The top of the ground under x, searching down from 'fromY' for 'distance'.
        private bool GroundAt(float x, float fromY, float distance, out float top)
        {
            var hit = Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, distance, _groundMask);
            top = hit.collider != null ? hit.point.y : 0f;
            return hit.collider != null;
        }

        // What the main camera shows, in world units (a wide default before there is one).
        private static Rect CameraView()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) { return new Rect(-1e5f, -1e5f, 2e5f, 2e5f); }
            float h = cam.orthographicSize, w = h * cam.aspect;
            Vector3 c = cam.transform.position;
            return new Rect(c.x - w, c.y - h, w * 2f, h * 2f);
        }
    }
}
