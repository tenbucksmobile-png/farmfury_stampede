using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Owns a level's layered parallax background (far to near <see cref="ParallaxLayer"/> children) and
    /// reconfigures every layer whenever <see cref="LevelLoader"/> loads a new level. A level whose prefab carries its
    /// own art (LevelPrefabRoot.parallaxLayers, e.g. Frozen Tundra's aurora) swaps it into the layers one for one and
    /// hides any layer left over; a level without keeps the layers' own (Meadow Ruins) art.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ParallaxLayer[] layers;
        [Tooltip("World Y the background is vertically centred on.")]
        [SerializeField] private float baseY = 3f;

        private Sprite[] _defaultSprites;

        public void Configure(float minX, float maxX, Sprite[] worldLayers = null)
        {
            if (targetCamera == null || layers == null)
            {
                return;
            }

            if (_defaultSprites == null)
            {
                _defaultSprites = new Sprite[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    _defaultSprites[i] = layers[i] != null ? layers[i].GetComponent<SpriteRenderer>().sprite : null;
                }
            }

            bool custom = worldLayers != null && worldLayers.Length > 0;
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer == null)
                {
                    continue;
                }

                var sprite = custom ? (i < worldLayers.Length ? worldLayers[i] : null) : _defaultSprites[i];
                layer.GetComponent<SpriteRenderer>().sprite = sprite;
                layer.gameObject.SetActive(sprite != null);
                if (sprite != null)
                {
                    layer.Configure(targetCamera, minX, maxX, baseY);
                }
            }
        }
    }
}
