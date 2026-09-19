using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Defines one enemy robot archetype: its behaviour and movement stats.</summary>
    [CreateAssetMenu(fileName = "RobotData", menuName = "Farm Fury Stampede/Robot Data")]
    public class RobotData : ScriptableObject
    {
        public RobotType robotType;
        public RobotBehaviour behaviour;
        [TextArea]
        public string behaviourDescription;

        public float moveSpeed = 3f;

        [Tooltip("Set when this robot can only be defeated/bypassed using a specific character's ability (e.g. BarrierUnit requires Billy's ChargeBreak). Leave unset for most robots.")]
        public AbilityType? requiresCharacterAbility;
    }
}
