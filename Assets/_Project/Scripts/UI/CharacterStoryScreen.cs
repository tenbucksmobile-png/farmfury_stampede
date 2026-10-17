using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's Character Story screen (Settings -> Character Story): tabs along the top - Story, How to Play,
    /// Characters, Cosmetics, Robots - each its own vertical scroll list of cream rows with a gold border, except
    /// Robots (since 2026-10-17, with every world's own robots): one world at a time (< and > step through them), its
    /// boss in the middle of a slowly turning ring of its robots and a card for the one tapped (RobotsPage). The copy
    /// is Stampede's (Arcade's describes its maze, power crops and swap combos, none of which exist here; Stampede
    /// has no combos, so that tab is gone). Rows are filled the first time the screen opens, since the character
    /// and robot rows need DataManager.
    /// </summary>
    public class CharacterStoryScreen : OverlayScreen
    {
        private static readonly Color TabActive = new(0.85f, 0.65f, 0.2f);
        private static readonly Color TabInactive = new(0.35f, 0.28f, 0.18f);
        private static readonly Color RowBorder = new(0.70f, 0.55f, 0.20f);
        private static readonly Color RowBackground = new(0.97f, 0.94f, 0.86f, 0.94f);
        private static readonly Color TitleColor = new(0.35f, 0.18f, 0.05f);
        private const float RowWidth = 1650f;
        private static readonly string[] TabNames = { "Story", "How to Play", "Characters", "Cosmetics", "Robots" };

        private const string IntroStory =
            "The Harvest Robots have marched out of the fields and taken the whole countryside.\n\n" +
            "From the crumbling Meadow Ruins to the Frozen Tundra, Watermill Village, the Sky Islands and the Sunken " +
            "City - all the way up to their floating Mothership - six territories have fallen under the Robot " +
            "Overlord's rule, and every last crop is being hauled away.\n\n" +
            "But the Farm Squad is done hiding. Eight animals, each with a talent all their own, are charging out to " +
            "take the land back - one level, one robot, one golden cob at a time.\n\n" +
            "Pick your animal. Find the secrets only they can reach. Topple every Commander. And when the Mothership " +
            "falls, the farm is free.\n\n" +
            "Run. Jump. Stomp. Stampede!";

        private static readonly (string title, string body)[] HowToPlay =
        {
            ("Run & Jump", "Run left and right and jump over pits and robots. Jump again in mid-air for a double " +
                "jump - it clears wide gaps and reaches high ledges."),
            ("Abilities", "Every animal has one special ability. It recharges for a few seconds after each use - " +
                "tap the coin or watch an ad to recharge it at once. Use it to beat robots " +
                "or reach places nobody else can."),
            ("Stomp the Robots", "Land on a robot from above to stomp it flat. Bump into one any other way and you " +
                "lose a life."),
            ("Lives & Checkpoints", "You have 3 lives each try. Lose one and you're back at the last checkpoint " +
                "flag. Lose the last one and you can spend 5 coins or watch an ad for one more life - or try the " +
                "level again."),
            ("Swap Animals", "Tap the swap button (or press Tab) any time to switch to another animal you've " +
                "unlocked, right where you stand. Your next level starts as whoever you finished with."),
            ("Corn & Stars", "Finish a level for 1 star. Collect at least three-quarters of the corn for a second, " +
                "and find the level's secret for the third (no secret? Finish without losing a life). More stars " +
                "earn more coins - the most the first time you clear a level, and a bonus for every new star after."),
            ("Secrets", "Every level hides a secret only one animal's ability can reach. Swap to the right friend " +
                "to get to it!"),
            ("New Friends", "Find a glowing crystal apple - a rare pellet - to unlock a new animal on the spot. " +
                "Clearing levels unlocks them too: Percy at 5, Woolly at 10, Ducky at 15, Horace at 20, Gerald at " +
                "30 and Billy at 40."),
        };

        private static readonly Dictionary<CharacterType, (string ability, string story)> Characters = new()
        {
            { CharacterType.Cluck, ("Egg Launch", "Small, quick and never short on nerve, Cluck is always first " +
                "through the gate. She doesn't wait for trouble to find her: lob an egg at a robot, on the ground or " +
                "in mid-air, and its day ends early.") },
            { CharacterType.Bessie, ("Ground Pound", "What Bessie lacks in speed she makes up for in sheer presence. " +
                "She slams the ground - cracked floors shatter, and an earthquake runs ahead of her to flatten " +
                "the first robot in its path.") },
            { CharacterType.Percy, ("Roll Dash", "Percy may look built for napping, but tuck him into a ball and " +
                "he's the fastest thing in the field. His roll flattens any robot in his path and carries him " +
                "straight over small gaps.") },
            { CharacterType.Woolly, ("Cloud Step", "Woolly never runs out of places to stand. Mid-jump she fluffs " +
                "up a puff of wool and bounces off it, climbing to ledges no one else can reach - and every puff " +
                "sends a tuft of wool flying ahead to knock a robot flat.") },
            { CharacterType.Ducky, ("Water Spout", "No pond, stream or flooded ruin has ever slowed Ducky down - " +
                "the only one of the Squad right at home in the water. She brings the river with her, too: one " +
                "flick and a spinning spout of water rolls out along the ground to wash a robot away.") },
            { CharacterType.Horace, ("Horseshoe Throw", "Horace doesn't run from a fight - he throws it. " +
                "On the ground or in mid-air, he lobs a spinning horseshoe that knocks a robot, " +
                "even a flying Drone, clean out of the sky.") },
            { CharacterType.Gerald, ("Feather Blow", "Gerald's temper is legendary, and when he puffs up the " +
                "whole farm knows it. One furious huff sends a storm of feathers flying straight ahead, and any " +
                "robot in its way - even a Drone - is blown clean off its feet.") },
            { CharacterType.Billy, ("Mud Sling", "Billy's never met a puddle he didn't want to throw at somebody. " +
                "He flings a glob of mud that splats the first robot it hits - and where it lands it leaves a sticky " +
                "mud pile that bogs down the next robot to blunder into it.") },
        };

        // What each kind of robot does (the same in every world; each world dresses them in its own look).
        private static readonly Dictionary<RobotType, string> RobotStories = new()
        {
            { RobotType.Harvester, "The workhorse of the robot army. It trundles back and forth on its patrol, turning " +
                "at walls and ledges. Simple, steady - and easy to stomp." },
            { RobotType.Scout, "Faster than a Harvester and a lot nosier: sneak up behind a Scout and it spins round " +
                "to face you. Time your jump before it turns." },
            { RobotType.Drone, "Rules of the ground don't apply to Drones - they hover out of reach. Jump into one " +
                "to knock it down, or let Horace's horseshoe do the job." },
            { RobotType.Chaser, "Sleeps until you come close, then races after you. It's quick - but not as quick " +
                "as you. Keep moving, or turn and stomp it." },
            { RobotType.Piranha, "Lurks under the river and leaps out in an arc when you cross. Harmless under the " +
                "water - stomp it while it's in the air." },
            { RobotType.Commander, "The boss of this territory. It takes three hits, gets faster after each one and " +
                "calls in reinforcements. Beat it to free the world." },
        };

        // Each world's names for its robots (types not listed keep the plain type name).
        private static readonly Dictionary<WorldType, Dictionary<RobotType, string>> RobotNames = new()
        {
            { WorldType.MeadowRuins, new() { { RobotType.Commander, "The Commander" } } },
            { WorldType.FrozenTundra, new() { { RobotType.Harvester, "Ice Harvester" }, { RobotType.Commander, "Glacier Harvester" } } },
            { WorldType.WatermillVillage, new() { { RobotType.Commander, "Millstone Mauler" } } },
            { WorldType.SkyIslands, new() { { RobotType.Commander, "Storm Baron" } } },
            { WorldType.SunkenCity, new() { { RobotType.Harvester, "Crab" }, { RobotType.Scout, "Shrimp" },
                { RobotType.Drone, "Jellyfish" }, { RobotType.Commander, "Deep Dredger" } } },
            { WorldType.RobotMothership, new() { { RobotType.Scout, "Patrol Tank" }, { RobotType.Drone, "Quad Drone" },
                { RobotType.Commander, "Robot Overlord" } } },
            { WorldType.DustbowlCanyon, new() { { RobotType.Harvester, "Shovel Harvester" }, { RobotType.Scout, "Cowboy Scout" },
                { RobotType.Drone, "Propeller Drone" }, { RobotType.Commander, "Drill Rig" } } },
            { WorldType.HarvestFairground, new() { { RobotType.Harvester, "Clown Car" }, { RobotType.Scout, "Juggler" },
                { RobotType.Drone, "Balloon Drone" }, { RobotType.Commander, "Ringmaster Bot" } } },
            { WorldType.CropFactory, new() { { RobotType.Harvester, "Reaper" }, { RobotType.Scout, "Bucket Scout" },
                { RobotType.Drone, "Vacuum Drone" }, { RobotType.Commander, "Harvester Prime" } } },
        };

        // The order robots go round the ring (the boss stands in the middle).
        private static readonly RobotType[] RingOrder =
            { RobotType.Harvester, RobotType.Scout, RobotType.Chaser, RobotType.Drone, RobotType.Piranha };

        private const int RobotsTab = 4;
        private readonly List<(WorldType world, string title, List<(RobotType type, Sprite art)> robots)> _robotWorlds = new();
        private int _robotWorld;
        private RectTransform _robotStage;
        private RobotRing _ring;
        private Text _robotWorldTitle, _robotName, _robotStory;
        private readonly List<(Image image, RobotType type)> _robotButtons = new();

        private readonly Button[] _tabs = new Button[TabNames.Length];
        private readonly RectTransform[] _lists = new RectTransform[TabNames.Length];
        private readonly GameObject[] _pages = new GameObject[TabNames.Length];
        private bool _populated;

        public CharacterStoryScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "CharacterStory", art, shop)
        {
            const float tabWidth = 310f, tabGap = 20f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                _tabs[i] = UIKit.MakeButton(Rect, $"{TabNames[i]}Tab", TabNames[i], TabInactive, () => SelectTab(index), 38);
                var label = _tabs[i].GetComponentInChildren<Text>();
                label.fontStyle = FontStyle.Bold;
                label.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
                _tabs[i].gameObject.AddComponent<Outline>().effectColor = RowBorder;
                UIKit.Place(_tabs[i].image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2((i - (TabNames.Length - 1) * 0.5f) * (tabWidth + tabGap), -50f), new Vector2(tabWidth, 90f));

                if (i == RobotsTab)
                {
                    _pages[i] = RobotsPage();   // a ring per world instead of a list
                }
                else
                {
                    (_pages[i], _lists[i]) = ScrollList(TabNames[i]);
                }
            }
        }

        protected override void OnShow()
        {
            Populate();
            SelectTab(0);
        }

        private void SelectTab(int index)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _pages[i].SetActive(i == index);
                _tabs[i].image.color = i == index ? TabActive : TabInactive;
            }
            if (_lists[index] != null) { _lists[index].anchoredPosition = Vector2.zero; }   // back to the top
        }

        // A vertical ScrollRect under the tabs, ending above the back button; returns (page, content).
        private (GameObject, RectTransform) ScrollList(string name)
        {
            var viewport = UIKit.NewRect(name + "Page", Rect);
            viewport.anchorMin = new Vector2(0.5f, 0f);
            viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.offsetMin = new Vector2(-(RowWidth + 40f) * 0.5f, 180f);
            viewport.offsetMax = new Vector2((RowWidth + 40f) * 0.5f, -170f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches drags between rows

            var content = UIKit.NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 30f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return (viewport.gameObject, content);
        }

        private void Populate()
        {
            if (_populated)
            {
                return;
            }
            _populated = true;

            TextRow(_lists[0], null, IntroStory, 40);

            foreach (var (title, body) in HowToPlay)
            {
                InfoRow(_lists[1], null, 0f, title, body);
            }

            var dm = DataManager.Instance;
            if (dm != null)
            {
                foreach (var data in dm.GetAllCharacters())
                {
                    var (ability, story) = Characters.TryGetValue(data.characterType, out var entry) ? entry : ("", data.abilityDescription);
                    string unlock = data.unlockLevelsRequired > 0 ? $"\nJoins the Squad after {data.unlockLevelsRequired} levels." : "\nReady from the start.";
                    InfoRow(_lists[2], data.selectCard != null ? data.selectCard : data.placeholderSprite, 300f,
                        data.displayName, $"Ability: {ability}\n{story}{unlock}");
                }
            }

            for (int i = 0; i < StoreProducts.Cosmetics.Length; i++)
            {
                var (name, blurb) = StoreProducts.Cosmetics[i];
                InfoRow(_lists[3], ShopArt.At(Shop.cosmeticIcons, i), 180f, name, blurb);
            }

            CollectRobotWorlds(dm);
            ShowRobotWorld(0);
        }

        // ------------------------------------------------------------ robots page

        // The page: the world's name between < and > at the top; on the left the world's boss in the middle of a slowly
        // turning ring of its robots (RobotRing); on the right a cream card with the tapped robot's name and what it
        // does. Fits the same area as the other tabs' lists.
        private GameObject RobotsPage()
        {
            var page = UIKit.NewRect("RobotsPage", Rect);
            page.anchorMin = new Vector2(0.5f, 0f);
            page.anchorMax = new Vector2(0.5f, 1f);
            page.offsetMin = new Vector2(-(RowWidth + 40f) * 0.5f, 180f);
            page.offsetMax = new Vector2((RowWidth + 40f) * 0.5f, -170f);

            var prev = UIKit.MakeButton(page, "PrevWorld", "<", TabInactive, () => ShowRobotWorld(_robotWorld - 1), 60);
            UIKit.Place(prev.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-560f, -10f), new Vector2(110f, 90f));
            var next = UIKit.MakeButton(page, "NextWorld", ">", TabInactive, () => ShowRobotWorld(_robotWorld + 1), 60);
            UIKit.Place(next.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, -10f), new Vector2(110f, 90f));
            foreach (var b in new[] { prev, next })
            {
                b.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
                b.gameObject.AddComponent<Outline>().effectColor = RowBorder;
            }
            _robotWorldTitle = UIKit.Label(page, "WorldTitle", "", 54, TextAnchor.MiddleCenter, UIKit.Accent);
            _robotWorldTitle.fontStyle = FontStyle.Bold;
            _robotWorldTitle.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Place(_robotWorldTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(980f, 90f));

            // The ring: its own stage, so the boss and the robots re-sort only among themselves.
            _robotStage = UIKit.NewRect("RobotRing", page);
            UIKit.Place(_robotStage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -55f), new Vector2(860f, 600f));
            _ring = _robotStage.gameObject.AddComponent<RobotRing>();
            _ring.radius = new Vector2(320f, 200f);

            // The card.
            var card = Row(page, "RobotCard");
            UIKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, -55f), new Vector2(760f, 520f));
            _robotName = UIKit.Label(card, "Name", "", 50, TextAnchor.UpperLeft, TitleColor);
            _robotName.fontStyle = FontStyle.Bold;
            UIKit.Place(_robotName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(680f, 70f));
            _robotStory = UIKit.Label(card, "Story", "", 34, TextAnchor.UpperLeft, Color.black);
            UIKit.Place(_robotStory.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(680f, 360f));
            return page.gameObject;
        }

        // Every world's robots, read from its own level prefabs: the first look each robot type has in that world
        // (RobotSpawnPoint.artRight), or the robot prefab's own art where the world keeps it.
        private void CollectRobotWorlds(DataManager dm)
        {
            _robotWorlds.Clear();
            if (dm == null) { return; }
            foreach (var world in dm.GetAllWorlds())
            {
                var found = new Dictionary<RobotType, Sprite>();
                foreach (var level in dm.GetWorldLevels(world.worldType))
                {
                    if (level == null || level.levelPrefab == null) { continue; }
                    foreach (var spawn in level.levelPrefab.GetComponentsInChildren<LevelSystem.RobotSpawnPoint>(true))
                    {
                        if (found.ContainsKey(spawn.robotType) || !RobotStories.ContainsKey(spawn.robotType)) { continue; }
                        Sprite art = spawn.artRight;
                        if (art == null)
                        {
                            var data = dm.GetRobotData(spawn.robotType);
                            var renderer = data != null && data.prefab != null ? data.prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
                            art = renderer != null ? renderer.sprite : null;
                        }
                        found[spawn.robotType] = art;
                    }
                }
                if (found.Count == 0) { continue; }
                var robots = new List<(RobotType, Sprite)>();
                if (found.TryGetValue(RobotType.Commander, out var boss)) { robots.Add((RobotType.Commander, boss)); }
                foreach (var type in RingOrder)
                {
                    if (found.TryGetValue(type, out var art)) { robots.Add((type, art)); }
                }
                _robotWorlds.Add((world.worldType, world.displayName, robots));
            }
        }

        private void ShowRobotWorld(int index)
        {
            if (_robotWorlds.Count == 0 || _robotStage == null) { return; }
            _robotWorld = (index % _robotWorlds.Count + _robotWorlds.Count) % _robotWorlds.Count;   // wraps both ways
            var (world, title, robots) = _robotWorlds[_robotWorld];
            _robotWorldTitle.text = title;

            for (int i = _robotStage.childCount - 1; i >= 0; i--) { Object.Destroy(_robotStage.GetChild(i).gameObject); }
            _robotButtons.Clear();

            var ringItems = new List<RectTransform>();
            foreach (var (type, art) in robots)
            {
                bool boss = type == RobotType.Commander;
                var image = UIKit.Picture(_robotStage, $"Robot_{type}", art);
                image.gameObject.SetActive(true);
                image.raycastTarget = true;
                image.color = art != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                var rt = image.rectTransform;
                UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, boss ? new Vector2(300f, 300f) : new Vector2(170f, 170f));
                var outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = UIKit.Accent;
                outline.effectDistance = new Vector2(5f, -5f);
                outline.enabled = false;
                RobotType picked = type;
                image.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectRobot(picked));
                _robotButtons.Add((image, type));
                if (!boss) { ringItems.Add(rt); }
            }
            _ring.SetItems(ringItems);
            SelectRobot(robots.Count > 0 ? robots[0].type : RobotType.Commander);   // the boss first
        }

        private void SelectRobot(RobotType type)
        {
            if (_robotWorlds.Count == 0) { return; }
            var world = _robotWorlds[_robotWorld].world;
            foreach (var (image, t) in _robotButtons)
            {
                var outline = image.GetComponent<Outline>();
                if (outline != null) { outline.enabled = t == type; }
            }
            _robotName.text = RobotName(world, type);
            _robotStory.text = RobotStories.TryGetValue(type, out string story) ? story : "";
        }

        private static string RobotName(WorldType world, RobotType type) =>
            RobotNames.TryGetValue(world, out var names) && names.TryGetValue(type, out string name) ? name
            : type == RobotType.Commander ? "The Boss" : type.ToString();

        // A bordered cream row laid out horizontally: [icon] [title + body]. Height follows the text.
        private RectTransform InfoRow(Transform list, Sprite icon, float iconSize, string title, string body)
        {
            var row = Row(list, title);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 24, 24);
            layout.spacing = 36f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            if (icon != null)
            {
                Icon(row, icon, iconSize);
            }

            var column = UIKit.NewRect("TextColumn", row);
            column.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var columnLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            columnLayout.spacing = 8f;
            columnLayout.childControlWidth = columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            var titleText = UIKit.Label(column, "Title", title, 40, TextAnchor.MiddleLeft, TitleColor);
            titleText.fontStyle = FontStyle.Bold;
            UIKit.Label(column, "Body", body, 32, TextAnchor.UpperLeft, Color.black);
            return row;
        }

        private void TextRow(Transform list, string title, string body, int fontSize)
        {
            var row = Row(list, title ?? "Intro");
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 34, 34);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            UIKit.Label(row, "Body", body, fontSize, TextAnchor.UpperLeft, Color.black);
        }

        private static RectTransform Row(Transform list, string name)
        {
            var row = UIKit.Panel(list, $"Row_{name}", RowBackground);
            var outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = RowBorder;
            outline.effectDistance = new Vector2(6f, -6f);
            return row.rectTransform;
        }

        private static void Icon(Transform row, Sprite sprite, float size)
        {
            var icon = UIKit.Picture(row, "Icon", sprite);
            var element = icon.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = size;
            element.preferredHeight = element.minHeight = size;
        }
    }
}
