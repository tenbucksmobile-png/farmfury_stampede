using System;
using UnityEngine;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Menu art that isn't per-world (per-world card/backdrop art lives on WorldData). Assigned to GameFlow by
    /// Phase 5a setup from Sprites/UI (StampedeUIArt); any sprite left null falls back to the plain code-built look.
    /// </summary>
    [Serializable]
    public class MenuArt
    {
        [Tooltip("World Select header banner (the wooden World Unlocked sign). Null = 'SELECT A WORLD' text.")]
        public Sprite worldSelectBanner;
        [Tooltip("Round play button in the bottom-left corner of each World Select card. Null = a green PLAY button.")]
        public Sprite playButton;
        [Tooltip("Character Select header banner (the wooden New Character sign). Null = 'Choose a character for ...' text.")]
        public Sprite characterSelectBanner;
        [Tooltip("Level Select tile for a locked level: plank with a padlock.")]
        public Sprite levelTileLocked;
        [Tooltip("Level Select tile for the next level to play (unlocked, not yet completed): plank with a question mark.")]
        public Sprite levelTileNext;
        [Tooltip("The boss's Level Select tile: shield reading BOSS.")]
        public Sprite bossShield;
        [Tooltip("Round back button, top-left of Level Select and Character Select. (Btn_back.png) Null = a plain wooden-coloured button.")]
        public Sprite backButton;
        [Tooltip("Cluck on a board with 1 / 2 / 3 gold stars: the Results panel and completed Level Select tiles.")]
        public Sprite oneStarBoard;
        public Sprite twoStarBoard;
        public Sprite threeStarBoard;
        [Tooltip("Results sign shown when the run unlocked a character.")]
        public Sprite newCharacterSign;
        [Tooltip("Results sign shown when a boss clear unlocked the next world.")]
        public Sprite worldUnlockedSign;

        /// <summary>Level Select art for an unlocked level: the star board once completed, else the question plaque.</summary>
        public Sprite LevelTile(bool completed, int stars) => completed && stars > 0 ? StarBoard(stars) : levelTileNext;

        public Sprite StarBoard(int stars) => stars switch
        {
            >= 3 => threeStarBoard,
            2 => twoStarBoard,
            1 => oneStarBoard,
            _ => null,
        };
    }
}
