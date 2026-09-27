using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Draws the equipped cosmetics on the player, ported from Farm Fury: Arcade's CharacterCosmeticRenderer and
    /// adapted to side view. Sits on the player's Visual (the SpriteRenderer CharacterSpriteAnimator drives) and runs
    /// after it:
    /// - Skin (a machine): replaces the character's sprite with the machine art for the facing, every pose, and
    ///   puffs exhaust smoke while moving. A skin hides the hat (SaveManager clears it on equip).
    /// - Hat: a child sprite at the character's head (CharacterData.hatAnchor, mirrored facing left), sized to
    ///   CharacterData.hatWidth x CosmeticData.hatScale; per-character art (caps, cowboy hats) or one hat for all
    ///   (sombrero - one of four designs per level -, chef hat, crown).
    /// - Trail: Arcade's ghost trail - the trail art stamped behind the moving character, fading out.
    /// Re-reads the save when the character changes and on <see cref="Refresh"/> (purchases, Locker equips).
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterCosmeticRenderer : MonoBehaviour
    {
        private const float GhostSpawnDistance = 0.6f;
        private const float GhostLifetimeSeconds = 0.7f;
        private const float GhostHeight = 0.6f;          // trail stamps sit at mid-body
        private const float SmokeSpawnDistance = 0.35f;
        private const float SmokeLifetimeSeconds = 0.6f;
        private const float SmokeBehindOffset = 0.9f;
        private const float SmokeHeight = 0.5f;

        private CharacterController2D _controller;
        private SpriteRenderer _body;
        private SpriteRenderer _hat;
        private CosmeticData _equippedHat, _equippedSkin, _equippedTrail;
        private CharacterType? _appliedCharacter;
        private Vector3 _lastGhostPosition, _lastSmokePosition;

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController2D>();
            _body = GetComponent<SpriteRenderer>();

            var hatObject = new GameObject("EquippedHat");
            hatObject.transform.SetParent(transform, false);
            _hat = hatObject.AddComponent<SpriteRenderer>();
            _hat.sortingLayerID = _body.sortingLayerID;
            _hat.sortingOrder = _body.sortingOrder + 1;
            _hat.enabled = false;
        }

        /// <summary>Re-reads what this character has equipped.</summary>
        public void Refresh()
        {
            var data = _controller != null ? _controller.Data : null;
            var save = SaveManager.Instance;
            var dm = DataManager.Instance;
            _appliedCharacter = data != null ? data.characterType : null;
            if (data == null || save == null || dm == null)
            {
                _equippedHat = _equippedSkin = _equippedTrail = null;
                _hat.enabled = false;
                return;
            }

            _equippedSkin = Owned(dm.GetCosmeticData(save.GetEquippedCosmetic(CosmeticType.Skin, data.characterType)), data.characterType);
            _equippedHat = _equippedSkin == null
                ? Owned(dm.GetCosmeticData(save.GetEquippedCosmetic(CosmeticType.Hat, data.characterType)), data.characterType)
                : null;
            _equippedTrail = dm.GetCosmeticData(save.GetEquippedTrail());
            if (_equippedTrail != null && !save.IsCosmeticOwned(_equippedTrail.cosmeticId))
            {
                _equippedTrail = null;
            }

            _lastGhostPosition = _lastSmokePosition = transform.position;
        }

        // Only what's owned and made for this character is worn.
        private static CosmeticData Owned(CosmeticData item, CharacterType character) =>
            item != null && item.FitsCharacter(character) && SaveManager.Instance.IsCosmeticOwned(item.cosmeticId) ? item : null;

        private void LateUpdate()
        {
            var data = _controller != null ? _controller.Data : null;
            if (data == null)
            {
                return;
            }
            if (_appliedCharacter != data.characterType)
            {
                Refresh();
            }

            bool right = _controller.Facing >= 0;

            if (_equippedSkin != null)
            {
                var skin = _equippedSkin.Skin(right);
                if (skin != null)
                {
                    _body.sprite = skin;
                    _body.flipX = false;
                }
                SpawnSmokeIfMoved(right);
            }

            UpdateHat(data, right);
            SpawnGhostIfMoved();
        }

        private void UpdateHat(CharacterData data, bool right)
        {
            var sprite = _equippedHat != null ? _equippedHat.Hat(right, LevelIndex()) : null;
            _hat.enabled = sprite != null;
            if (sprite == null)
            {
                return;
            }

            _hat.sprite = sprite;
            _hat.flipX = _equippedHat.HatNeedsMirror(right);
            var anchor = data.hatAnchor;
            _hat.transform.localPosition = new Vector3(right ? anchor.x : -anchor.x, anchor.y, 0f);
            float width = Mathf.Max(0.01f, sprite.bounds.size.x);
            _hat.transform.localScale = Vector3.one * (data.hatWidth * _equippedHat.hatScale / width);
        }

        private static int LevelIndex()
        {
            var gm = GameManager.Instance;
            return gm != null && gm.CurrentLevel != null && DataManager.Instance != null
                ? DataManager.Instance.GetAllLevels().IndexOf(gm.CurrentLevel)
                : 0;
        }

        private void SpawnGhostIfMoved()
        {
            if (_equippedTrail == null || _equippedTrail.trailSprite == null || _controller.IsDying)
            {
                return;
            }
            if (Vector3.Distance(transform.position, _lastGhostPosition) < GhostSpawnDistance)
            {
                return;
            }

            _lastGhostPosition = transform.position;
            var go = new GameObject("TrailGhost");
            go.transform.position = transform.position + Vector3.up * GhostHeight;
            go.AddComponent<CosmeticTrailGhost>().Configure(_equippedTrail.trailSprite, _body.sortingLayerID, _body.sortingOrder - 1,
                1f, GhostLifetimeSeconds);
        }

        private void SpawnSmokeIfMoved(bool right)
        {
            if (!_equippedSkin.spawnsMovementSmoke || Vector3.Distance(transform.position, _lastSmokePosition) < SmokeSpawnDistance)
            {
                return;
            }

            _lastSmokePosition = transform.position;
            var go = new GameObject("MachineSmokePuff");
            go.transform.position = transform.position + new Vector3(right ? -SmokeBehindOffset : SmokeBehindOffset, SmokeHeight, 0f);
            go.AddComponent<MachineSmokePuff>().Configure(_body.sortingLayerID, _body.sortingOrder - 1, SmokeLifetimeSeconds);
        }
    }
}
