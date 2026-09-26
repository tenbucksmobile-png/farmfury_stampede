using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Level Select for one world: a grid of level tiles (regular levels, then the boss).
    ///
    /// Two looks. A world with a <see cref="WorldData.levelSelectBackground"/> follows the Level Select mockup: the
    /// backdrop (world name baked in along the top) and wooden plaques, six per row, played in order: every slot
    /// starts as the padlock except the first, which is the question mark (the next level to play); completing a
    /// level turns it into the Cluck board with its 1-3 gold stars and the next slot into the question mark. The boss
    /// slot (after the last level) always shows the boss shield - untappable until every level is done - and its star
    /// board once beaten. No numbers or secret icons on the art look. Every tile's art is drawn at the same visible size
    /// (scaled by how much of its frame the art fills) in an evenly spaced grid sized for 12 slots (11 levels + boss,
    /// two rows of six) inside the device safe area and below the backdrop's title, so tiles never overlap each
    /// other, the title or a notch; a short last row is centred. The backdrop covers the screen with any overflow
    /// cropped off the bottom, never the top, so the baked-in title stays whole and inside the safe area.
    /// A world without a backdrop keeps the plain code-built card grid (numbers, names, stars, the secret "?" icon).
    /// </summary>
    public class LevelSelectScreen
    {
        public class Tile
        {
            public LevelData level;
            public GameObject root;
            public Button button;
            public bool unlocked;
            public bool completed;
            public int stars;
            public GameObject secretIcon;   // plain look only
            public GameObject lockLabel;    // plain look only
            public float artScale;          // art rect size / tile size (art look)
            public Vector2 artOffset;       // art centre offset, in art-rect sizes (art look)
            public RectTransform art;       // art look
        }

        public GameObject Root { get; private set; }
        public WorldType World { get; private set; }
        public IReadOnlyList<Tile> Tiles => _tiles;

        private readonly List<Tile> _tiles = new();
        private readonly Transform _gridParent;
        private readonly Text _title;
        private readonly Text _empty;
        private readonly Action<LevelData> _onPick;
        private readonly MenuArt _art;
        private readonly Image _backdrop;
        private readonly Button _plainBack;
        private readonly Button _artBack;
        private bool _artLook;

        // Plain look: 5 cards per row under the text title.
        private const int PlainColumns = 5;
        private const float PlainTileWidth = 320f, PlainTileHeight = 240f, PlainTopRowY = 110f, PlainRowStep = 270f;

        // Art look. Every world backdrop has its title in the top ~37% of the image; the grid starts below that.
        private const int ArtColumns = 6;
        private const int ArtMinRows = 2;              // sized for 12 slots even while a world has fewer levels
        private const float TitleBand = 0.37f;
        private const float EdgeMargin = 16f;          // inside the safe area
        private const float TileFill = 0.8f;           // tile size as a fraction of its cell: the rest is the gap
        // Art rect relative to the tile, so every tile's visible art is the same height. Measured off the 500px art:
        // the padlock / question plaques and the boss shield fill ~99% of their frame; the Cluck boards have a
        // transparent margin (the board is 70% of the frame tall) and sit off-centre in it, so their rect is bigger
        // and nudged to centre the board on the tile.
        private const float PlaqueScale = 1f / 0.99f;
        private const float BoardScale = 1f / 0.698f;
        private const float ShieldScale = 1f / 0.986f;
        private static readonly Vector2 BoardCentreOffset = new(0.5f - 0.485f, 0.519f - 0.5f);   // x right, y up, in frames
        private const float BackButtonSize = UIKit.RoundButtonSize;

        public LevelSelectScreen(Transform canvas, Action<LevelData> onPick, Action onBack, MenuArt art)
        {
            _onPick = onPick;
            _art = art ?? new MenuArt();

            var root = UIKit.NewRect("LevelSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = UIKit.Dark;
            _backdrop = UIKit.Backdrop(root);
            _backdrop.rectTransform.pivot = new Vector2(0.5f, 1f);   // crop overflow off the bottom, keep the title whole

            _title = UIKit.Label(root, "Title", "", 60, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1300f, 90f));

            _gridParent = UIKit.NewRect("Grid", root);
            UIKit.Stretch((RectTransform)_gridParent);

            // Buttons stay inside the device safe area.
            var safe = UIKit.NewRect("SafeArea", root);
            var fitter = safe.gameObject.AddComponent<SafeAreaFitter>();
            fitter.Changed += Layout;

            _plainBack = UIKit.MakeButton(safe, "BackButton", "< Worlds", new Color(0.25f, 0.3f, 0.45f, 1f), () => onBack?.Invoke(), 34);
            UIKit.Place(_plainBack.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(240f, 70f));

            // Mockup back button: round wooden button in the top-left corner of the safe area.
            _artBack = UIKit.MakeButton(safe, "BackButtonRound", _art.backButton != null ? "" : "<",
                new Color(0.62f, 0.38f, 0.17f, 1f), () => onBack?.Invoke(), 72);
            UIKit.Place(_artBack.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin, -EdgeMargin),
                new Vector2(BackButtonSize, BackButtonSize));
            if (_art.backButton != null)
            {
                _artBack.image.sprite = _art.backButton;
                _artBack.image.color = Color.white;
                _artBack.image.preserveAspect = true;
            }

            _empty = UIKit.Label(root, "Empty", "No levels in this world yet", 44, TextAnchor.MiddleCenter, UIKit.Muted);
            UIKit.Place(_empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 80f));

            Root.SetActive(false);
        }

        /// <summary>Rebuilds the grid for the world from the current save.</summary>
        public void Show(WorldType world)
        {
            World = world;
            var data = DataManager.Instance.GetWorldData(world);
            _title.text = data != null ? data.displayName : world.ToString();
            var background = data != null ? data.levelSelectBackground : null;
            _artLook = background != null && _art.levelTileLocked != null;
            UIKit.SetBackdrop(_backdrop, background);
            _title.gameObject.SetActive(background == null);   // the backdrop has the world name baked in
            _plainBack.gameObject.SetActive(!_artLook);
            _artBack.gameObject.SetActive(_artLook);

            foreach (var tile in _tiles)
            {
                UnityEngine.Object.Destroy(tile.root);
            }
            _tiles.Clear();

            var levels = DataManager.Instance.GetWorldLevels(world);
            _empty.gameObject.SetActive(levels.Count == 0);
            var save = SaveManager.Instance;

            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var tile = new Tile
                {
                    level = level,
                    unlocked = save.IsLevelUnlocked(level),
                    completed = save.IsLevelCompleted(level.levelId),
                    stars = save.GetLevelStars(level.levelId),
                };
                bool secretUnfound = tile.completed && level.hasCharacterGatedSecret && !save.IsSecretFound(level.levelId);

                if (_artLook)
                {
                    BuildArtTile(tile);
                }
                else
                {
                    BuildPlainTile(tile, i, secretUnfound, background != null);
                }

                var captured = level;
                tile.button.interactable = tile.unlocked;
                tile.button.onClick.AddListener(() => _onPick?.Invoke(captured));
                _tiles.Add(tile);
            }

            Layout();
        }

        // ------------------------------------------------------------ art look

        private void BuildArtTile(Tile tile)
        {
            var level = tile.level;
            Sprite sprite;
            bool board = tile.unlocked && tile.completed && tile.stars > 0;
            if (board)
            {
                sprite = _art.StarBoard(tile.stars);
                tile.artScale = BoardScale;
                tile.artOffset = BoardCentreOffset;
            }
            else if (level.isBossLevel && _art.bossShield != null)
            {
                sprite = _art.bossShield;   // the boss keeps its badge, locked or not
                tile.artScale = ShieldScale;
            }
            else if (!tile.unlocked)
            {
                sprite = _art.levelTileLocked;
                tile.artScale = PlaqueScale;
            }
            else
            {
                sprite = _art.levelTileNext;
                tile.artScale = PlaqueScale;
            }

            // The tile root is an invisible, tile-sized hit area; the art is a non-raycast child that may be drawn
            // bigger (transparent margins), so one tile's margin can never swallow a tap meant for its neighbour.
            var hit = UIKit.Panel(_gridParent, $"Tile_{level.levelId}", new Color(1f, 1f, 1f, 0f));
            var art = UIKit.Picture(hit.transform, "Art", sprite);
            art.gameObject.SetActive(true);
            if (sprite == null)
            {
                art.color = tile.unlocked ? UIKit.Card : UIKit.CardLocked;   // missing art: still a visible tile
            }

            var button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = art;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = Color.white;   // the padlock art already says "locked"; don't grey it too
            button.colors = colors;

            tile.root = hit.gameObject;
            tile.button = button;
            tile.art = art.rectTransform;
        }

        /// <summary>
        /// Positions the art tiles for the current screen and safe area: a 6-wide grid (at least two rows) filling
        /// the space between the backdrop title and the bottom of the safe area. Runs on Show and whenever the
        /// safe area changes.
        /// </summary>
        private void Layout()
        {
            if (!_artLook || _tiles.Count == 0)
            {
                return;
            }

            var size = ((RectTransform)Root.transform).rect.size;
            float w = size.x, h = size.y;
            var (safeMin, safeMax) = SafeAreaFitter.Normalized();
            float left = -w * 0.5f + safeMin.x * w + EdgeMargin;
            float right = -w * 0.5f + safeMax.x * w - EdgeMargin;
            float bottom = -h * 0.5f + safeMin.y * h + EdgeMargin;
            float safeTop = -h * 0.5f + safeMax.y * h;

            // The backdrop covers the screen with its top edge on the screen's top, so its title band hangs from there.
            var sprite = _backdrop.sprite;
            float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 16f / 9f;
            float imageHeight = Mathf.Max(h, w / aspect);
            float titleBottom = h * 0.5f - imageHeight * TitleBand;
            float top = Mathf.Min(titleBottom, safeTop - EdgeMargin);

            int rows = Mathf.Max(ArtMinRows, Mathf.CeilToInt(_tiles.Count / (float)ArtColumns));
            float cellW = (right - left) / ArtColumns;
            float cellH = (top - bottom) / rows;
            float tileSize = Mathf.Min(cellW, cellH) * TileFill;
            float centreX = (left + right) * 0.5f;

            for (int i = 0; i < _tiles.Count; i++)
            {
                int row = i / ArtColumns, col = i % ArtColumns;
                int inRow = Mathf.Min(ArtColumns, _tiles.Count - row * ArtColumns);
                var centre = new Vector2(centreX + (col - (inRow - 1) * 0.5f) * cellW, top - cellH * (row + 0.5f));

                var tile = _tiles[i];
                UIKit.Place((RectTransform)tile.root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre,
                    new Vector2(tileSize, tileSize));
                float artSize = tileSize * tile.artScale;
                UIKit.Place(tile.art, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), tile.artOffset * artSize,
                    Vector2.one * artSize);
            }
        }

        // ------------------------------------------------------------ plain look

        private void BuildPlainTile(Tile tile, int index, bool secretUnfound, bool overArt)
        {
            var level = tile.level;
            int col = index % PlainColumns, row = index / PlainColumns;
            var tileColor = tile.unlocked ? (level.isBossLevel ? new Color(0.45f, 0.2f, 0.2f, 1f) : UIKit.Card) : UIKit.CardLocked;
            tileColor.a = overArt ? 0.88f : 1f;
            var panel = UIKit.Panel(_gridParent, $"Tile_{level.levelId}", tileColor);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2((col - 2) * (PlainTileWidth + 20f), PlainTopRowY - row * PlainRowStep), new Vector2(PlainTileWidth, PlainTileHeight));

            string number = level.isBossLevel ? "BOSS" : (index + 1).ToString();
            var numberLabel = UIKit.Label(panel.transform, "Number", number, level.isBossLevel ? 46 : 64, TextAnchor.MiddleCenter);
            UIKit.Place(numberLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(300f, 80f));
            var nameLabel = UIKit.Label(panel.transform, "Name", level.displayName, 26, TextAnchor.MiddleCenter, tile.unlocked ? Color.white : UIKit.Muted);
            UIKit.Place(nameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(300f, 70f));

            var bar = UIKit.StarBar(panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-45f, 18f), 30f);
            UIKit.SetStars(bar, tile.stars);

            var lockLabel = UIKit.Label(panel.transform, "Lock", "LOCKED", 34, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.5f));
            UIKit.Place(lockLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(300f, 44f));
            lockLabel.gameObject.SetActive(!tile.unlocked);
            if (!tile.unlocked)
            {
                foreach (var star in bar)
                {
                    star.gameObject.SetActive(false);
                }
            }

            var icon = UIKit.Panel(panel.transform, "SecretIcon", new Color(0.85f, 0.3f, 0.9f, 1f));
            UIKit.Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(50f, 50f));
            var mark = UIKit.Label(icon.transform, "Mark", "?", 36);
            UIKit.Stretch(mark.rectTransform);
            icon.gameObject.SetActive(secretUnfound);

            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;

            tile.root = panel.gameObject;
            tile.button = button;
            tile.secretIcon = icon.gameObject;
            tile.lockLabel = lockLabel.gameObject;
        }
    }
}
