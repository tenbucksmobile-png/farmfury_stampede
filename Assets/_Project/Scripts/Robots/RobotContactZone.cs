using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Trigger zone on a robot prefab. The top zone ("StompDetector") only ever stomps; the body zone
    /// ("HurtDetector") hurts unless the contact qualifies as a stomp. Both forward to the robot, which
    /// makes the decision in one place. Stay is handled too so a player who is already overlapping when
    /// invulnerability ends, or who lands on the edge of a zone, is still resolved.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RobotContactZone : MonoBehaviour
    {
        [SerializeField] private RobotController robot;
        [SerializeField] private bool isStompZone;

        private void Awake()
        {
            if (robot == null)
            {
                robot = GetComponentInParent<RobotController>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            robot.HandlePlayerContact(other, isStompZone);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            robot.HandlePlayerContact(other, isStompZone);
        }
    }
}
