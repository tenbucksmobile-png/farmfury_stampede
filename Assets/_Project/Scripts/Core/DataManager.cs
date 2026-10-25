using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Central registry for every ScriptableObject definition. Asset lists are assigned in the
    /// Inspector (drag the contents of Assets/_Project/ScriptableObjects/... in) rather than
    /// loaded via Resources, so the ScriptableObjects folders stay outside any Resources path.
    /// </summary>
    public class DataManager : MonoSingleton<DataManager>
    {
        [Header("Source Assets (assign in Inspector)")]
        [SerializeField] private List<CharacterData> allCharacters = new();
        [SerializeField] private List<RobotData> allRobots = new();
        [SerializeField] private List<LevelData> allLevels = new();
        [SerializeField] private List<WorldData> allWorlds = new();
        [SerializeField] private List<CosmeticData> allCosmetics = new();

        private Dictionary<CharacterType, CharacterData> _characters;
        private Dictionary<RobotType, RobotData> _robots;
        private Dictionary<string, LevelData> _levels;
        private Dictionary<WorldType, WorldData> _worlds;
        private Dictionary<string, CosmeticData> _cosmetics;

        protected override void Awake()
        {
            base.Awake();
            LoadAllData();
        }

        // A duplicate id (two assets for one character, level or world) keeps the first and logs an error instead of
        // throwing: ToDictionary would have thrown in Awake and left every manager after it without data (audit).
        private void LoadAllData()
        {
            _characters = FirstOfEach(allCharacters, c => c.characterType, "character");
            _robots = FirstOfEach(allRobots, r => r.robotType, "robot");
            _levels = FirstOfEach(allLevels.Where(l => l == null || !string.IsNullOrEmpty(l.levelId)), l => l.levelId, "level");
            _worlds = FirstOfEach(allWorlds, w => w.worldType, "world");
            _cosmetics = FirstOfEach(allCosmetics.Where(c => c == null || !string.IsNullOrEmpty(c.cosmeticId)), c => c.cosmeticId, "cosmetic");

            Debug.Log($"[DataManager] Loaded {_characters.Count} characters, {_robots.Count} robots, " +
                      $"{_levels.Count} levels, {_worlds.Count} worlds, {_cosmetics.Count} cosmetics.");
        }

        private static Dictionary<TKey, TValue> FirstOfEach<TKey, TValue>(IEnumerable<TValue> items, System.Func<TValue, TKey> key, string kind)
            where TValue : Object
        {
            var map = new Dictionary<TKey, TValue>();
            foreach (var item in items)
            {
                if (item == null) { continue; }
                var k = key(item);
                if (!map.TryAdd(k, item))
                {
                    Debug.LogError($"[DataManager] Duplicate {kind} '{k}' ({item.name}); keeping {map[k].name}.");
                }
            }
            return map;
        }

        /// <summary>Looks up a character's data by type. Returns null if not found.</summary>
        public CharacterData GetCharacterData(CharacterType type)
        {
            return _characters.TryGetValue(type, out var data) ? data : null;
        }

        /// <summary>Looks up a robot's data by type. Returns null if not found.</summary>
        public RobotData GetRobotData(RobotType type)
        {
            return _robots.TryGetValue(type, out var data) ? data : null;
        }

        /// <summary>Looks up a level's data by its id. Returns null if not found.</summary>
        public LevelData GetLevelData(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && _levels.TryGetValue(levelId, out var data) ? data : null;
        }

        /// <summary>Returns every loaded world, in WorldType order (Meadow Ruins ... Robot Mothership).</summary>
        public List<WorldData> GetAllWorlds()
        {
            return _worlds.Values.OrderBy(w => (int)w.worldType).ToList();
        }

        /// <summary>A world's levels in play order: regular levels by id, then the boss level.</summary>
        public List<LevelData> GetWorldLevels(WorldType world)
        {
            return _levels.Values
                .Where(l => l.worldType == world)
                .OrderBy(l => l.isBossLevel ? 1 : 0)
                .ThenBy(l => l.levelId, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Returns every loaded character, in CharacterType order (Cluck ... Billy).</summary>
        public List<CharacterData> GetAllCharacters()
        {
            return _characters.Values.OrderBy(c => (int)c.characterType).ToList();
        }

        /// <summary>Returns every loaded level ordered by levelId (MeadowRuins_01, MeadowRuins_02, ...).</summary>
        public List<LevelData> GetAllLevels()
        {
            return _levels.Values.OrderBy(l => l.levelId, System.StringComparer.Ordinal).ToList();
        }

        /// <summary>Looks up a world's data by type. Returns null if not found.</summary>
        public WorldData GetWorldData(WorldType type)
        {
            return _worlds.TryGetValue(type, out var data) ? data : null;
        }

        /// <summary>Looks up a cosmetic by its id (empty/unknown = null, i.e. nothing equipped).</summary>
        public CosmeticData GetCosmeticData(string cosmeticId)
        {
            return !string.IsNullOrEmpty(cosmeticId) && _cosmetics.TryGetValue(cosmeticId, out var data) ? data : null;
        }

        /// <summary>Every loaded cosmetic, ordered by id (the F1 debug panel's cosmetic cycling).</summary>
        public List<CosmeticData> GetAllCosmetics()
        {
            return _cosmetics.Values.OrderBy(c => c.cosmeticId).ToList();
        }

        /// <summary>Returns every loaded character whose unlock condition is currently met.</summary>
        public List<CharacterData> GetAllUnlockedCharacters()
        {
            if (SaveManager.Instance == null)
            {
                return new List<CharacterData>();
            }

            return _characters.Values
                .Where(c => SaveManager.Instance.IsCharacterUnlocked(c.characterType))
                .ToList();
        }
    }
}
