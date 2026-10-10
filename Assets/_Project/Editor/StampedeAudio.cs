using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Wires the dropped-in audio (Audio/Music, Audio/SFX) into the scene's AudioManager by file name, and sets each
    /// clip's import options (music streamed, effects decompressed on load). Called from Run Setup; a missing file
    /// leaves its slot empty (quiet) with a warning. To add a sound: drop the file in, add it to the tables, re-run.
    /// Unassigned so far: Music/HarvestMoon.mp3 (no obvious home); Woolly has no ability sound.
    /// </summary>
    internal static class StampedeAudio
    {
        private const string MusicDir = "Assets/_Project/Audio/Music/";
        private const string SfxDir = "Assets/_Project/Audio/SFX/";

        private const string MenuMusicFile = "Theme.mp3";

        private static readonly Dictionary<WorldType, string> WorldMusicFiles = new()
        {
            { WorldType.MeadowRuins, "MeadowRuins.mp3" },
            { WorldType.FrozenTundra, "FrozenTundra.mp3" },
            { WorldType.WatermillVillage, "WatermillVillage.mp3" },
            { WorldType.SkyIslands, "SkyIsland.mp3" },
            { WorldType.SunkenCity, "SunkenCity.mp3" },
            { WorldType.RobotMothership, "Mothership.mp3" },
        };

        private static readonly Dictionary<CharacterType, string> AbilityFiles = new()
        {
            { CharacterType.Cluck, "Clucky_abiltiy.mp3" },
            { CharacterType.Bessie, "Bessie-ability.mp3" },
            { CharacterType.Percy, "Percy_ability.mp3" },
            { CharacterType.Ducky, "DuckyTeleport.mp3" },
            { CharacterType.Horace, "Horace_ability.mp3" },
            { CharacterType.Gerald, "Gerald_ability.mp3" },
            { CharacterType.Billy, "billy_ability.mp3" },
        };

        public static void WireScene()
        {
            var audio = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (audio == null)
            {
                Debug.LogError("[StampedeAudio] No AudioManager in the scene; audio not wired.");
                return;
            }

            var so = new SerializedObject(audio);
            so.FindProperty("menuMusic").objectReferenceValue = Music(MenuMusicFile);

            var worlds = so.FindProperty("worldMusic");
            var worldClips = WorldMusicFiles.Select(kv => (kv.Key, clip: Music(kv.Value))).Where(w => w.clip != null).ToList();
            worlds.arraySize = worldClips.Count;
            for (int i = 0; i < worldClips.Count; i++)
            {
                var e = worlds.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("world").enumValueIndex = (int)worldClips[i].Key;
                e.FindPropertyRelative("clip").objectReferenceValue = worldClips[i].clip;
            }

            so.FindProperty("cropPickup").objectReferenceValue = Sfx("CornPickup.mp3");
            so.FindProperty("coinPickup").objectReferenceValue = Sfx("CoinPickup.mp3");
            so.FindProperty("rarePelletPickup").objectReferenceValue = Sfx("RarePellet_pickup.mp3");
            so.FindProperty("robotDefeated").objectReferenceValue = Sfx("EatRobot.mp3");
            so.FindProperty("bossHit").objectReferenceValue = Sfx("Robot_damage.mp3");
            so.FindProperty("playerDeath").objectReferenceValue = Sfx("Animal_death.mp3");
            so.FindProperty("abilityReady").objectReferenceValue = Sfx("PowerReady.mp3");

            var abilities = so.FindProperty("abilitySounds");
            var abilityClips = AbilityFiles.Select(kv => (kv.Key, clip: Sfx(kv.Value))).Where(a => a.clip != null).ToList();
            abilities.arraySize = abilityClips.Count;
            for (int i = 0; i < abilityClips.Count; i++)
            {
                var e = abilities.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("character").enumValueIndex = (int)abilityClips[i].Key;
                e.FindPropertyRelative("clip").objectReferenceValue = abilityClips[i].clip;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[StampedeAudio] Wired {worldClips.Count} world tracks, the menu theme and {abilityClips.Count} ability sounds.");
        }

        private static AudioClip Music(string file) => Load(MusicDir + file, AudioClipLoadType.Streaming);

        private static AudioClip Sfx(string file) => Load(SfxDir + file, AudioClipLoadType.DecompressOnLoad);

        private static AudioClip Load(string path, AudioClipLoadType loadType)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                var settings = importer.defaultSampleSettings;
                if (settings.loadType != loadType || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.loadType = loadType;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[StampedeAudio] Missing {path}; that slot stays quiet.");
            }
            return clip;
        }
    }
}
