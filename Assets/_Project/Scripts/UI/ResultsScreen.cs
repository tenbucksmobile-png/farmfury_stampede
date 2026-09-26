using System;
using FarmFuryStampede.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// End-of-attempt screen: level complete or out of lives.
    ///
    /// Art look (the Level Complete / Level Failed mockups, when MenuArt has both backdrops): a full-screen backdrop
    /// with the title and the Farm Fury logo painted in - LevelComplete_Canvas.png also has three gold stars, and a
    /// greyed star (LevelComplete_StarEmpty.png, cut from the same art) is drawn over each star not earned. The
    /// whole 1280x720 artwork is always on screen, fitted to the screen (FitContent: never cropped, never stretched);
    /// setup paints ArtPad extra pixels onto each side (the *_Wide.png copies, edges mirrored), so a phone wider
    /// than 16:9 shows more farm at the sides instead of bars;
    /// the stars and unlock signs are children of the backdrop, placed in the art's own pixel coordinates, so they
    /// stay on the painted stars at any screen size. Buttons (inside the safe area, the shared round size):
    ///   complete - play = continue to Level Select (next level ready as the question mark; World Select after the boss), X = Level Select,
    ///              home = landing screen, cog = Shop &amp; Settings;
    ///   failed   - play = retry the level, X = Level Select, home = landing screen.
    /// Under the stars sits the score: a corn kernel and "collected / total" crops for the run, in the title's gold
    /// lettering, one star tall so it scales with them. New Character / World Unlocked signs flank it when earned.
    ///
    /// Plain look (no art): the original dark panel with square stars, the crop/death summary and text buttons.
    /// </summary>
    public class ResultsScreen
    {
        public GameObject Root { get; private set; }

        // Art look.
        public Button NextButton { get; private set; }
        public Button RetryButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Button HomeButton { get; private set; }
        public Button SettingsButton { get; private set; }

        // Plain look.
        public Text Title { get; private set; }
        public Text Body { get; private set; }
        public Button ReplayButton { get; private set; }
        public Button ContinueButton { get; private set; }

        private readonly MenuArt _art;
        private readonly bool _artLook;

        private readonly Image _backdrop;
        private readonly Image[] _emptyStars = new Image[3];
        private readonly Image _newCharacterSign;
        private readonly Image _worldUnlockedSign;
        private readonly GameObject _score;
        private readonly Text _scoreText;

        private readonly GameObject _panel;
        private readonly Image[] _stars;
        private readonly Image _board;
        private readonly Image _panelNewCharacterSign;
        private readonly Image _panelWorldUnlockedSign;

        // The mockup art is 1280x720; the painted stars (measured off LevelComplete_Canvas.png) are 76px boxes
        // centred on these points. The score goes under them, the same height as a star; the unlock signs flank it
        // in the open hills, clear of the windmill (x 730-860 from y 488 down).
        private static readonly Vector2 ArtSize = new(1280f, 720f);
        /// <summary>Pixels setup adds to each side of the results art (the *_Wide.png copies): covers phones up to ~2.2:1.</summary>
        public const int ArtPad = 160;
        private static readonly float[] StarCentresX = { 551.5f, 639.5f, 727.5f };
        private const float StarCentreY = 369.5f, StarBox = 76f;
        private static readonly Rect ScoreIconBox = new(528f, 416f, 60f, 60f);
        private static readonly Rect ScoreTextBox = new(596f, 412f, 200f, 68f);   // left-aligned, beside the kernel
        private static readonly Rect NewCharacterSignBox = new(260f, 410f, 240f, 80f);
        private static readonly Rect WorldUnlockedSignBox = new(820f, 410f, 240f, 80f);
        private static readonly Color ScoreGold = new(1f, 0.84f, 0.25f, 1f);
        private static readonly Color ScoreOutline = new(0.24f, 0.11f, 0.03f, 1f);

        private const float EdgeMargin = 70f;
        private const float BottomMargin = 40f;
        private const float ButtonGap = 30f;

        public ResultsScreen(Transform canvas, Action onNext, Action onRetry, Action onQuit, Action onHome, Action onSettings, MenuArt art)
        {
            _art = art ?? new MenuArt();
            _artLook = _art.levelCompleteBackground != null && _art.levelFailedBackground != null;

            var root = UIKit.NewRect("Results", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = _artLook ? UIKit.Dark : new Color(0f, 0f, 0f, 0.55f);

            if (_artLook)
            {
                // The widened art, fitted so its 1280x720 middle is fully on screen at its own aspect; the extra
                // side margin fills wider screens. preserveAspect as a second guard against any stretching.
                _backdrop = UIKit.Picture(root, "Backdrop", null);
                _backdrop.gameObject.SetActive(true);
                var fit = _backdrop.gameObject.AddComponent<FitContent>();
                fit.contentSize = ArtSize;
                fit.spriteSize = new Vector2(ArtSize.x + 2f * ArtPad, ArtSize.y);

                for (int i = 0; i < _emptyStars.Length; i++)
                {
                    _emptyStars[i] = UIKit.Picture(_backdrop.transform, $"EmptyStar{i + 1}", _art.levelCompleteStarEmpty);
                    PlaceInArt(_emptyStars[i].rectTransform,
                        new Rect(StarCentresX[i] - StarBox * 0.5f, StarCentreY - StarBox * 0.5f, StarBox, StarBox));
                }
                _newCharacterSign = UIKit.Picture(_backdrop.transform, "NewCharacterSign", _art.newCharacterSign);
                PlaceInArt(_newCharacterSign.rectTransform, NewCharacterSignBox);
                _worldUnlockedSign = UIKit.Picture(_backdrop.transform, "WorldUnlockedSign", _art.worldUnlockedSign);
                PlaceInArt(_worldUnlockedSign.rectTransform, WorldUnlockedSignBox);

                // Score: kernel icon + count. Best Fit sizes the text to its box, so it scales with the backdrop.
                _score = UIKit.NewRect("Score", _backdrop.transform).gameObject;
                UIKit.Stretch((RectTransform)_score.transform);
                var icon = UIKit.Picture(_score.transform, "Kernel", _art.scoreIcon);
                PlaceInArt(icon.rectTransform, ScoreIconBox);
                _scoreText = UIKit.Label(_score.transform, "Count", "", 60, TextAnchor.MiddleLeft, ScoreGold);
                _scoreText.fontStyle = FontStyle.Bold;
                _scoreText.raycastTarget = false;
                _scoreText.resizeTextForBestFit = true;
                _scoreText.resizeTextMinSize = 8;
                _scoreText.resizeTextMaxSize = 300;
                _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
                PlaceInArt(_scoreText.rectTransform, _art.scoreIcon != null ? ScoreTextBox
                    : new Rect(ScoreIconBox.xMin, ScoreTextBox.yMin, ScoreTextBox.xMax - ScoreIconBox.xMin, ScoreTextBox.height));
                if (_art.scoreIcon == null) { _scoreText.alignment = TextAnchor.MiddleCenter; }
                var outline = _scoreText.gameObject.AddComponent<Outline>();
                outline.effectColor = ScoreOutline;
                outline.effectDistance = new Vector2(3f, -3f);

                var safe = UIKit.NewRect("SafeArea", root);
                safe.gameObject.AddComponent<SafeAreaFitter>();
                float size = UIKit.RoundButtonSize;
                float left = EdgeMargin + 60f, second = left + size + ButtonGap;
                NextButton = RoundButton(safe, "NextButton", _art.playButton, ">", onNext, Vector2.zero, new Vector2(left, BottomMargin));
                RetryButton = RoundButton(safe, "RetryButton", _art.playButton, ">", onRetry, Vector2.zero, new Vector2(left, BottomMargin));
                QuitButton = RoundButton(safe, "QuitButton", _art.quitButton, "X", onQuit, Vector2.zero, new Vector2(second, BottomMargin));
                SettingsButton = RoundButton(safe, "SettingsButton", _art.settingsButton, "SET", onSettings, Vector2.right, new Vector2(-EdgeMargin, BottomMargin));
                // Home sits beside the cog on Level Complete, and in the cog's corner on Level Failed (see Layout).
                HomeButton = RoundButton(safe, "HomeButton", _art.homeButton, "HOME", onHome, Vector2.right, new Vector2(-EdgeMargin, BottomMargin));
            }
            else
            {
                var panel = UIKit.Panel(root, "Panel", UIKit.Dark);
                _panel = panel.gameObject;
                UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));

                Title = UIKit.Label(panel.transform, "Title", "", 64, TextAnchor.MiddleCenter, UIKit.Accent);
                UIKit.Place(Title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(840f, 90f));

                _stars = UIKit.StarBar(panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -150f), 64f);

                // Star board art (Cluck with 1/2/3 gold stars) replaces the square stars when present. The unlock
                // signs flank it: New Character on the left, World Unlocked on the right.
                _board = UIKit.Picture(panel.transform, "StarBoard", null);
                UIKit.Place(_board.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(320f, 320f));
                _panelNewCharacterSign = UIKit.Picture(panel.transform, "NewCharacterSign", null);
                UIKit.Place(_panelNewCharacterSign.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -265f), new Vector2(290f, 125f));
                _panelWorldUnlockedSign = UIKit.Picture(panel.transform, "WorldUnlockedSign", null);
                UIKit.Place(_panelWorldUnlockedSign.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(330f, -265f), new Vector2(290f, 125f));

                Body = UIKit.Label(panel.transform, "Body", "", 36, TextAnchor.UpperCenter);
                UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(820f, 240f));

                ReplayButton = UIKit.MakeButton(panel.transform, "ReplayButton", "Replay", UIKit.Card, () => onRetry?.Invoke(), 38);
                UIKit.Place(ReplayButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 30f), new Vector2(340f, 80f));
                ContinueButton = UIKit.MakeButton(panel.transform, "ContinueButton", "Level Select", UIKit.Good, () => onQuit?.Invoke(), 38);
                UIKit.Place(ContinueButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 30f), new Vector2(340f, 80f));
            }

            Root.SetActive(false);
        }

        public void ShowComplete(GameManager gm)
        {
            var run = gm.RunState;
            bool newCharacter = run.newlyUnlockedCharacters.Count > 0;

            if (_artLook)
            {
                SetArt(_art.levelCompleteBackground);
                for (int i = 0; i < _emptyStars.Length; i++)
                {
                    _emptyStars[i].gameObject.SetActive(_art.levelCompleteStarEmpty != null && i >= run.starsEarned);
                }
                _newCharacterSign.gameObject.SetActive(newCharacter && _art.newCharacterSign != null);
                _worldUnlockedSign.gameObject.SetActive(run.worldUnlocked && _art.worldUnlockedSign != null);
                _scoreText.text = $"{run.cropsCollectedThisRun} / {run.totalNormalCrops + run.totalSecretCrops}";
                _score.SetActive(true);
                LayoutButtons(complete: true);
                Root.SetActive(true);
                return;
            }

            Title.text = run.bossCleared ? "BOSS DEFEATED!" : "LEVEL COMPLETE!";
            UIKit.SetStars(_stars, run.starsEarned);
            var board = _art.StarBoard(run.starsEarned);
            _board.sprite = board;
            _board.gameObject.SetActive(board != null);
            foreach (var s in _stars) s.gameObject.SetActive(board == null);
            // Over the board art, the body text moves down below it.
            UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, board != null ? -435f : -240f), new Vector2(820f, 200f));
            ShowSign(_panelNewCharacterSign, _art.newCharacterSign, newCharacter);
            ShowSign(_panelWorldUnlockedSign, _art.worldUnlockedSign, run.worldUnlocked);

            string body = $"Crops: {run.cropsCollectedThisRun}/{run.totalNormalCrops + run.totalSecretCrops}    Deaths: {run.deathsThisRun}";
            if (newCharacter)
            {
                body += $"\nNEW CHARACTER: {string.Join(", ", run.newlyUnlockedCharacters)}";
            }
            if (run.worldUnlocked)
            {
                body += "\nThe next world is unlocked!";
            }
            Body.text = body;
            ReplayButton.gameObject.SetActive(true);
            Root.SetActive(true);
        }

        public void ShowFailed()
        {
            if (_artLook)
            {
                SetArt(_art.levelFailedBackground);
                foreach (var star in _emptyStars) { star.gameObject.SetActive(false); }
                _score.SetActive(false);
                _newCharacterSign.gameObject.SetActive(false);
                _worldUnlockedSign.gameObject.SetActive(false);
                LayoutButtons(complete: false);
                Root.SetActive(true);
                return;
            }

            Title.text = "OUT OF LIVES";
            foreach (var s in _stars) s.gameObject.SetActive(false);
            _board.gameObject.SetActive(false);
            _panelNewCharacterSign.gameObject.SetActive(false);
            _panelWorldUnlockedSign.gameObject.SetActive(false);
            UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(820f, 200f));
            Body.text = "";
            ReplayButton.gameObject.SetActive(true);
            Root.SetActive(true);
        }

        public void Hide()
        {
            Root.SetActive(false);
        }

        private void SetArt(Sprite sprite)
        {
            _backdrop.sprite = sprite;
            _backdrop.preserveAspect = true;
        }

        // Complete: play + X bottom-left, home + cog bottom-right. Failed: play (retry) + X bottom-left, home alone
        // in the bottom-right corner.
        private void LayoutButtons(bool complete)
        {
            NextButton.gameObject.SetActive(complete);
            RetryButton.gameObject.SetActive(!complete);
            SettingsButton.gameObject.SetActive(complete);
            float homeX = complete ? -EdgeMargin - UIKit.RoundButtonSize - ButtonGap : -EdgeMargin;
            HomeButton.image.rectTransform.anchoredPosition = new Vector2(homeX, BottomMargin);
        }

        // Places a child of the backdrop over a box given in the original 1280x720 art's pixels (origin top-left), by
        // anchors on the widened sprite, so it scales and moves with the backdrop.
        private static void PlaceInArt(RectTransform rt, Rect box)
        {
            float wide = ArtSize.x + 2f * ArtPad;
            rt.anchorMin = new Vector2((box.xMin + ArtPad) / wide, 1f - box.yMax / ArtSize.y);
            rt.anchorMax = new Vector2((box.xMax + ArtPad) / wide, 1f - box.yMin / ArtSize.y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // A round art button of the shared size in a corner of the safe area (corner (0,0) = bottom-left, (1,0) =
        // bottom-right); without art, a plain labelled square.
        private static Button RoundButton(Transform parent, string name, Sprite sprite, string fallbackLabel, Action onClick,
            Vector2 corner, Vector2 offset)
        {
            var button = UIKit.MakeButton(parent, name, sprite != null ? "" : fallbackLabel,
                sprite != null ? Color.white : new Color(0.85f, 0.55f, 0.15f, 1f), () => onClick?.Invoke(), 34);
            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.preserveAspect = true;
            }
            UIKit.Place(button.image.rectTransform, corner, corner, offset, Vector2.one * UIKit.RoundButtonSize);
            return button;
        }

        private static void ShowSign(Image sign, Sprite sprite, bool show)
        {
            sign.sprite = sprite;
            sign.gameObject.SetActive(show && sprite != null);
        }
    }
}
