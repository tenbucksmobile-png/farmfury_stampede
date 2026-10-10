using System;
using FarmFuryStampede.Data;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Music and sound effects. The clips live here (filled by Run Setup from Audio/Music and Audio/SFX, see
    /// StampedeAudio): the theme on every menu, each world's track in its levels (the theme for a world without one),
    /// and one-shot effects that gameplay asks for by slot, e.g. <c>AudioManager.Play(a => a.cropPickup)</c>. A
    /// missing clip is silently skipped, so a slot without a file just stays quiet.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoSingleton<AudioManager>
    {
        [Serializable]
        public struct WorldMusic
        {
            public WorldType world;
            public AudioClip clip;
        }

        [Serializable]
        public struct CharacterSound
        {
            public CharacterType character;
            public AudioClip clip;
        }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [Tooltip("Landing, World Select, Level Select and the other menus; also any world without its own track.")]
        public AudioClip menuMusic;
        public WorldMusic[] worldMusic = new WorldMusic[0];
        [Range(0f, 1f)] public float musicVolume = 0.6f;

        [Header("Sound effects")]
        public AudioClip cropPickup;
        public AudioClip coinPickup;
        public AudioClip rarePelletPickup;
        [Tooltip("A robot defeated (stomp or ability).")]
        public AudioClip robotDefeated;
        [Tooltip("The boss takes a hit that doesn't finish it.")]
        public AudioClip bossHit;
        public AudioClip playerDeath;
        [Tooltip("The ability has finished recharging.")]
        public AudioClip abilityReady;
        [Tooltip("Played when each character uses their ability.")]
        public CharacterSound[] abilitySounds = new CharacterSound[0];
        [Range(0f, 1f)] public float sfxVolume = 1f;

        protected override void Awake()
        {
            base.Awake();

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            // Effects get their own source so the music toggle (SetMusicMuted) doesn't silence them too.
            if (sfxSource == null || sfxSource == musicSource)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                SetMusicMuted(!SaveManager.Instance.MusicOn);
            }
        }

        /// <summary>Plays the effect in the chosen slot, if there is an AudioManager and the slot has a clip.</summary>
        public static void Play(Func<AudioManager, AudioClip> slot)
        {
            var audio = Instance;
            if (audio != null && slot != null)
            {
                audio.PlaySfx(slot(audio));
            }
        }

        /// <summary>The character's ability sound (quiet for a character without one).</summary>
        public static void PlayAbility(CharacterType character)
        {
            var audio = Instance;
            if (audio == null) { return; }
            foreach (var s in audio.abilitySounds)
            {
                if (s.character == character) { audio.PlaySfx(s.clip); return; }
            }
        }

        /// <summary>The menu theme (keeps playing, not restarted, when it already is).</summary>
        public static void PlayMenuMusic()
        {
            var audio = Instance;
            if (audio != null) { audio.PlayMusic(audio.menuMusic); }
        }

        /// <summary>The world's own track, or the menu theme when it has none.</summary>
        public static void PlayWorldMusic(WorldType world)
        {
            var audio = Instance;
            if (audio == null) { return; }
            foreach (var m in audio.worldMusic)
            {
                if (m.world == world && m.clip != null) { audio.PlayMusic(m.clip); return; }
            }
            audio.PlayMusic(audio.menuMusic);
        }

        /// <summary>Settings' music toggle. Sound effects are unaffected.</summary>
        public void SetMusicMuted(bool muted)
        {
            musicSource.mute = muted;
        }

        /// <summary>Plays a one-shot sound effect; a null clip is skipped.</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>Plays looping background music; a null clip is skipped, and the clip already playing carries on.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || (musicSource.clip == clip && musicSource.isPlaying))
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();
        }
    }
}
