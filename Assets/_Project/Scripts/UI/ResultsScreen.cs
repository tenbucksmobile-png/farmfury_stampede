using System;
using FarmFuryStampede.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>End-of-attempt panel: level complete (stars, crops, unlocks) or out of lives (returns to Level Select).</summary>
    public class ResultsScreen
    {
        public GameObject Root { get; private set; }
        public Text Title { get; private set; }
        public Text Body { get; private set; }
        public Button ReplayButton { get; private set; }
        public Button ContinueButton { get; private set; }

        private readonly Image[] _stars;
        private readonly MenuArt _art;
        private readonly Image _board;
        private readonly Image _newCharacterSign;
        private readonly Image _worldUnlockedSign;

        public ResultsScreen(Transform canvas, Action onReplay, Action onContinue, MenuArt art)
        {
            _art = art ?? new MenuArt();
            var root = UIKit.NewRect("Results", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var panel = UIKit.Panel(root, "Panel", UIKit.Dark);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));

            Title = UIKit.Label(panel.transform, "Title", "", 64, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(Title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(840f, 90f));

            _stars = UIKit.StarBar(panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -150f), 64f);

            // Star board art (Cluck with 1/2/3 gold stars) replaces the square stars when present. The unlock signs
            // flank it: New Character on the left, World Unlocked on the right.
            _board = UIKit.Picture(panel.transform, "StarBoard", null);
            UIKit.Place(_board.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(320f, 320f));
            _newCharacterSign = UIKit.Picture(panel.transform, "NewCharacterSign", null);
            UIKit.Place(_newCharacterSign.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -265f), new Vector2(290f, 125f));
            _worldUnlockedSign = UIKit.Picture(panel.transform, "WorldUnlockedSign", null);
            UIKit.Place(_worldUnlockedSign.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(330f, -265f), new Vector2(290f, 125f));

            Body = UIKit.Label(panel.transform, "Body", "", 36, TextAnchor.UpperCenter);
            UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(820f, 240f));

            ReplayButton = UIKit.MakeButton(panel.transform, "ReplayButton", "Replay", UIKit.Card, () => onReplay?.Invoke(), 38);
            UIKit.Place(ReplayButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 30f), new Vector2(340f, 80f));
            ContinueButton = UIKit.MakeButton(panel.transform, "ContinueButton", "Level Select", UIKit.Good, () => onContinue?.Invoke(), 38);
            UIKit.Place(ContinueButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 30f), new Vector2(340f, 80f));

            Root.SetActive(false);
        }

        public void ShowComplete(GameManager gm)
        {
            var run = gm.RunState;
            Title.text = run.bossCleared ? "BOSS DEFEATED!" : "LEVEL COMPLETE!";
            UIKit.SetStars(_stars, run.starsEarned);
            var board = _art.StarBoard(run.starsEarned);
            _board.sprite = board;
            _board.gameObject.SetActive(board != null);
            foreach (var s in _stars) s.gameObject.SetActive(board == null);
            // Over the board art, the body text moves down below it.
            UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, board != null ? -435f : -240f), new Vector2(820f, 200f));
            ShowSign(_newCharacterSign, _art.newCharacterSign, run.newlyUnlockedCharacters.Count > 0);
            ShowSign(_worldUnlockedSign, _art.worldUnlockedSign, run.worldUnlocked);

            string body = $"Crops: {run.cropsCollectedThisRun}/{run.totalNormalCrops + run.totalSecretCrops}    Deaths: {run.deathsThisRun}";
            if (run.newlyUnlockedCharacters.Count > 0)
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
            Title.text = "OUT OF LIVES";
            foreach (var s in _stars) s.gameObject.SetActive(false);
            _board.gameObject.SetActive(false);
            _newCharacterSign.gameObject.SetActive(false);
            _worldUnlockedSign.gameObject.SetActive(false);
            UIKit.Place(Body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(820f, 200f));
            Body.text = "Returning to Level Select...";
            ReplayButton.gameObject.SetActive(false);
            Root.SetActive(true);
        }

        private static void ShowSign(Image sign, Sprite sprite, bool show)
        {
            sign.sprite = sprite;
            sign.gameObject.SetActive(show && sprite != null);
        }

        public void Hide()
        {
            Root.SetActive(false);
        }
    }
}
