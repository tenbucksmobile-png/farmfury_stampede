using System;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>Pooled prefabs abilities spawn. Assigned once on the player prefab.</summary>
    [Serializable]
    public class AbilityPrefabs
    {
        public GameObject cloudPlatform;
        public GameObject horseshoe;
        public GameObject egg;
        public GameObject poundEffect;   // Bessie's landing impact (FadeEffect)
    }
}
