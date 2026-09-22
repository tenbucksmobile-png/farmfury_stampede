using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Owns a level's layered parallax background (far to near <see cref="ParallaxLayer"/> children) and
    /// reconfigures every layer whenever <see cref="LevelLoader"/> loads a new level.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ParallaxLayer[] layers;
        [Tooltip("World Y the background is vertically centred on.")]
        [SerializeField] private float baseY = 3f;

        public void Configure(float minX, float maxX)
        {
            if (targetCamera == null || layers == null)
            {
                return;
            }

            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.Configure(targetCamera, minX, maxX, baseY);
                }
            }
        }
    }
}
