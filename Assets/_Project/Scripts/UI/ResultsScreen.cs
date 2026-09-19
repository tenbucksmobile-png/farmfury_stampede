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

        public ResultsScreen(Transform canvas, Action onReplay, Action onContinue)
        {
            var root = UIKit.NewRect("Results", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var panel = UIKit.Panel(root, "Panel", UIKit.Dark);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            Title = UIKit.Label(panel.transform, "Title", "", 64, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(Title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(840f, 90f));

            _stars = UIKit.StarBar(panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -150f), 64f);

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
            foreach (var s in _stars) s.gameObject.SetActive(true);

            string body = $"Crops: {run.cropsCollectedThisRun}/{run.totalNormalCrops + run.totalSecretCrops}    Deaths: {run.deathsThisRun}";
            if (run.newlyUnlockedCharacters.Count > 0)
            {
                body += $"\nNEW CHARACTER: {string.Join(", ", run.newlyUnlockedCharacters)}";
            }
            if (run.bossCleared)
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
            Body.text = "Returning to Level Select...";
            ReplayButton.gameObject.SetActive(false);
            Root.SetActive(true);
        }

        public void Hide()
        {
            Root.SetActive(false);
        }
    }
}
