using System;
using FarmFuryStampede.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's Settings panel: the SETTINGS sign and one row of four icons - Music (on/off, the icon dims while
    /// muted), Leaderboards, Character Story and Policies (the Legal screen). Opened from the menu hub and from
    /// Pause; each icon opens its screen on top of this one.
    /// </summary>
    public class SettingsScreen : OverlayScreen
    {
        private readonly Button _music;

        public SettingsScreen(Transform canvas, MenuArt art, ShopArt shop, Action onLeaderboards, Action onCharacterStory, Action onPolicies)
            : base(canvas, "Settings", art, shop)
        {
            HeaderSign(Shop.settingsSign, "SETTINGS");

            var row = Row("SettingsGrid", 4, new Vector2(IconSize, IconSize), IconSpacing, -183f);
            _music = IconButton(row, "MusicCell", Shop.musicIcon, "MUSIC", ToggleMusic);
            IconButton(row, "LeaderboardCell", Shop.leaderboardIcon, "LEADERBOARDS", onLeaderboards);
            IconButton(row, "CharacterStoryCell", Shop.characterStoryIcon, "STORY", onCharacterStory);
            IconButton(row, "PoliciesCell", Shop.policiesIcon, "POLICIES", onPolicies);
        }

        protected override void OnShow() => RefreshMusicIcon();

        private void ToggleMusic()
        {
            var save = SaveManager.Instance;
            if (save == null)
            {
                return;
            }

            save.MusicOn = !save.MusicOn;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicMuted(!save.MusicOn);
            }
            RefreshMusicIcon();
        }

        private void RefreshMusicIcon()
        {
            bool on = SaveManager.Instance == null || SaveManager.Instance.MusicOn;
            _music.image.color = on ? (Shop.musicIcon != null ? Color.white : Wood) : OwnedTint;
            if (Shop.musicIcon == null)
            {
                _music.GetComponentInChildren<Text>().text = on ? "MUSIC ON" : "MUSIC OFF";
            }
        }
    }
}
