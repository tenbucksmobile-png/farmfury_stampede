using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Sky Islands' updraft (LevelBuilder.Updraft): a column of rising wind. While the player is inside it they are
    /// carried up at liftSpeed (CharacterController2D.NotifyInUpdraft) and can drift sideways out of it at any height;
    /// leaving the top they coast up a little further and fall as usual. The spiral art sways gently.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpdraftZone : MonoBehaviour
    {
        public float liftSpeed = 9f;
        [Tooltip("The spiral art (optional), swayed and pulsed while the level plays.")]
        public Transform art;

        private Vector3 _artScale;

        private void Awake()
        {
            if (art != null) { _artScale = art.localScale; }
        }

        private void Update()
        {
            if (art == null) { return; }
            float t = Time.time;
            art.localScale = new Vector3(_artScale.x * (1f + 0.06f * Mathf.Sin(t * 5f)), _artScale.y, _artScale.z);
            art.localRotation = Quaternion.Euler(0f, 0f, 2.5f * Mathf.Sin(t * 1.7f));
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var character = other.GetComponentInParent<CharacterController2D>();
            if (character != null)
            {
                character.NotifyInUpdraft(liftSpeed);
            }
        }
    }
}
