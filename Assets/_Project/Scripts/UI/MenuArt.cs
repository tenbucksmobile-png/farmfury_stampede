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
        [Header("Landing screen")]
        [Tooltip("The landing poster, edges faded (FF_StampedePoster_Soft.png, made by setup from FF_StampedePoster.png): logo, sign and Cluck painted in; the buttons go on top.")]
        public Sprite landingPoster;
        [Tooltip("Behind the slightly zoomed-out poster: the same farm scene without the logo (Environment/Canvas.png).")]
        public Sprite landingBackdrop;
        [Tooltip("Round play button, bottom-left. (Btn_play.png) Null = a plain PLAY button.")]
        public Sprite playButton;
        [Tooltip("Exit button, bottom-right. (Exit.png)")]
        public Sprite exitButton;
        [Tooltip("Settings cog, bottom-right corner; opens Shop & Settings. (Btn_settings.png)")]
        public Sprite settingsButton;

        [Header("Gameplay HUD")]
        [Tooltip("One per life, top-right; each death hides one. (CluckThumbsUp.png) Null = red squares.")]
        public Sprite lifeIcon;
        [Tooltip("Round pause button, bottom-left. (Btn_pause.png) Null = a plain II button.")]
        public Sprite pauseButton;

        [Header("Level Complete / Level Failed")]
        [Tooltip("Level Complete backdrop: title, logo and three gold stars painted in (Environment/LevelComplete_Canvas.png).")]
        public Sprite levelCompleteBackground;
        [Tooltip("Level Failed backdrop: title and logo painted in (Environment/LevelFailed_Canvas.png).")]
        public Sprite levelFailedBackground;
        [Tooltip("Greyed star drawn over each painted star not earned (UI/LevelComplete_StarEmpty.png, cut from the backdrop).")]
        public Sprite levelCompleteStarEmpty;
        [Tooltip("Icon beside the Level Complete score (the corn kernel pickup, CornKernel.png).")]
        public Sprite scoreIcon;
        [Tooltip("Round X button: back to Level Select from the results screens. (Btn_quit.png)")]
        public Sprite quitButton;

        [Header("Menus")]
        [Tooltip("World Select backdrop: the sunset farm (Environment/Canvas.png). Null = the plain dark background.")]
        public Sprite worldSelectBackground;
        [Tooltip("Round home button, top-left of World Select; back to the landing screen. (Btn_home.png) Null = a plain Home button.")]
        public Sprite homeButton;
        [Tooltip("World Select header banner (the wooden World Unlocked sign). Null = 'SELECT A WORLD' text.")]
        public Sprite worldSelectBanner;
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

        public Sprite StarBoard(int stars) => stars switch
        {
            >= 3 => threeStarBoard,
            2 => twoStarBoard,
            1 => oneStarBoard,
            _ => null,
        };
    }
}
