using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The "Breakable Floor" tilemap layer: solid like ground until Bessie's Ground Pound lands nearby, which
    /// removes the tiles (and their collision) within a radius so whatever lies beneath is revealed.
    /// Nothing else can break these tiles. The layer is part of the level prefab, so reloading the level
    /// restores it.
    /// </summary>
    [RequireComponent(typeof(Tilemap), typeof(TilemapCollider2D))]
    public class BreakableFloorLayer : MonoBehaviour
    {
        private Tilemap _tilemap;
        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _composite;

        private void Awake()
        {
            Cache();
        }

        private void Cache()
        {
            if (_tilemap == null)
            {
                _tilemap = GetComponent<Tilemap>();
                _tilemapCollider = GetComponent<TilemapCollider2D>();
                _composite = GetComponent<CompositeCollider2D>();
            }
        }

        /// <summary>Removes every tile whose centre is within radius of the point. Returns how many were removed.</summary>
        public int BreakInRadius(Vector2 worldPoint, float radius)
        {
            Cache();

            Vector3Int min = _tilemap.WorldToCell(worldPoint - Vector2.one * radius);
            Vector3Int max = _tilemap.WorldToCell(worldPoint + Vector2.one * radius);

            int removed = 0;
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (!_tilemap.HasTile(cell))
                    {
                        continue;
                    }

                    Vector2 center = _tilemap.GetCellCenterWorld(cell);
                    if (Vector2.Distance(center, worldPoint) <= radius)
                    {
                        _tilemap.SetTile(cell, null);
                        removed++;
                    }
                }
            }

            if (removed > 0)
            {
                // Rebuild the collision now so the hole is passable on the very next physics step.
                _tilemapCollider.ProcessTilemapChanges();
                if (_composite != null)
                {
                    _composite.GenerateGeometry();
                }
                Physics2D.SyncTransforms();
            }

            return removed;
        }
    }
}
