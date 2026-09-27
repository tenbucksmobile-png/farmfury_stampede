using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Minimal audio playback stub. No real audio assets exist yet — PlaySfx/PlayMusic no-op
    /// safely on a null clip rather than throwing, since nothing calls them with real clips
    /// until later phases.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoSingleton<AudioManager>
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

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
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                SetMusicMuted(!SaveManager.Instance.MusicOn);
            }
        }

        /// <summary>Settings' music toggle. Sound effects are unaffected.</summary>
        public void SetMusicMuted(bool muted)
        {
            musicSource.mute = muted;
        }

        /// <summary>Plays a one-shot sound effect. Logs and no-ops if clip is null.</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.Log("[AudioManager] PlaySfx called with a null clip — no-op.");
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        /// <summary>Plays looping background music. Logs and no-ops if clip is null.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.Log("[AudioManager] PlayMusic called with a null clip — no-op.");
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();
        }
    }
}
