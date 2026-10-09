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
        public GameObject waterSpout;    // Ducky's rolling water ring (WaterSpout)
        public GameObject splashEffect;  // its ripples and splash: WaterSpout.png (FadeEffect)
        public GameObject featherGust;   // Gerald's blown feathers (FeatherGust)
        public GameObject featherEffect; // shed feathers and the scatter: FeatherBurst.png (FadeEffect)
        public GameObject woolTuft;      // Woolly's tuft of wool (FeatherGust with the Wooly_effect art)
        public GameObject woolEffect;    // its shed wisps and the scatter: Wooly_effect.png (FadeEffect)
        public GameObject victoryEffect; // the boss victory bursts: ImpactStars.png (FadeEffect)
    }
}
