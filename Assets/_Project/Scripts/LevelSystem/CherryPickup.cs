using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The cherry power-up (Cherry.png): eating it makes the character invincible to robots for
    /// <see cref="PowerSeconds"/> - every robot it touches is defeated (the boss is only made harmless), while pits,
    /// water and hazards still cost a life - with its ability art swirling round it (CherrySwirl). Placed by
    /// LevelBuilder (at most one per level, on the main path just before its busiest stretch of robots) and baked
    /// into the level prefab, which is instantiated fresh on every load, so it is back on every replay; within an
    /// attempt it is gone once eaten. Not a crop: it counts toward no crops, stars, score or coins.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CherryPickup : MonoBehaviour
    {
        public const float PowerSeconds = 5f;   // was 3 until 2026-10-30

        [SerializeField] private SpriteRenderer visual;

        private const float BobHeight = 0.15f, BobSpeed = 2.6f;
        private const float SwayDegrees = 8f;

        private Vector3 _home;
        private float _phase;
        private bool _eaten;

        private void Start()
        {
            if (visual == null) { visual = GetComponentInChildren<SpriteRenderer>(); }
            _home = visual.transform.localPosition;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time + _phase;
            visual.transform.localPosition = _home + Vector3.up * (Mathf.Sin(t * BobSpeed) * BobHeight);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * BobSpeed * 0.5f) * SwayDegrees);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_eaten) { return; }
            var player = other.GetComponentInParent<CharacterController2D>();
            var gm = GameManager.Instance;
            if (player == null || player.IsDying || gm == null || gm.CurrentState != GameState.Playing)
            {
                return;
            }

            _eaten = true;
            player.StartPowerUp(PowerSeconds);
            AudioManager.Play(a => a.powerUp);
            gameObject.SetActive(false);
        }
    }
}
