# FARM FURY: STAMPEDE — Game Design Document

**v1.0** · "Charge In. Break Through. Take It Back." · A Farm Fury Universe Game
Side-Scrolling Platformer · Mobile · Free-to-Play

---

## 0. Where This Sits in the Franchise

Farm Fury now has four titles sharing a brand, cast, and villain. Each has a genuinely different core loop — this matters because it's what keeps the franchise from feeling like the same game skinned four times:

| Title | Genre | Core loop | Status |
|---|---|---|---|
| Farm Fury (main) | Physics destruction (launcher-based) | Aim, launch, wreck structures | Shelved |
| Farm Fury: Arcade | Pac-Man-style maze chase | Navigate mazes, swap characters mid-maze for combo effects | Near-launch |
| Farm Fury: Rush | Endless runner | Auto-run, swap characters mid-run to react to obstacles | Phase 2 of 6 complete |
| **Farm Fury: Stampede** | **Side-scrolling platformer** | **Pick a character, run/jump/fight through a hand-built level, find character-gated secrets** | **Phase 1 of 6 built, not yet verified in-editor** |

Stampede's job is to be the classic-platformer entry — think Super Mario Bros, not Angry Birds or Pac-Man or Subway Surfers. Deliberately, it does **not** reuse either sibling's signature mechanic:

- Arcade's hook is swapping characters *mid-maze* for combo timing.
- Rush's hook is swapping characters *mid-run* under reflex pressure.
- Stampede's hook is **choosing a character *before* a level and finding what only that character can reach** — closer to picking Mario vs. Luigi vs. Toad, or needing Yoshi to reach a hidden area. One roster, three completely different reasons to care which animal you're playing.

---

## 1. Executive Summary

**Game Title:** Farm Fury: Stampede
**Tagline:** "Charge In. Break Through. Take It Back."

**Concept:** A side-scrolling platformer set in the Farm Fury universe. Players pick one of eight farm animals and fight through hand-designed levels across six territories the Harvest Robots have occupied, running, jumping, and using each character's signature ability to defeat robots and reach the end of the level. Every level is completable by any unlocked character — but each level also hides secrets, shortcuts, and bonus areas that only one specific character's ability can reach, rewarding replay with the rest of the roster.

**Platform:** iOS and Android (primary), matching the rest of the franchise.

**Target Audience:**
- Primary: Casual mobile gamers 8–45 who grew up on (or currently play) classic platformers
- Secondary: Existing Farm Fury / Arcade / Rush players via cross-promotion
- Tertiary: Parents looking for a lighter, precision-platforming alternative to endless runners

**Genre:** Platformer / Action / Casual

**Business Model:** Free-to-play. Same ethical stance as the rest of the franchise — no energy walls, no pay-to-win, no loot boxes. Revenue from rewarded/interstitial ads, Remove Ads, cosmetics, and purchase-gated later worlds. See Section 11.

**Development Stack:** Unity 6.5, C#, Claude Code as primary engineering collaborator, Kling AI for art — matching Arcade and Rush exactly (see Section 12).

**Unique Selling Points:**
- The only Farm Fury title built as a classic run-jump-stomp platformer, not a maze or a runner
- Character choice determines which secrets a level yields, not whether you can finish it — replay value without walling off content behind difficulty or payment
- Reuses and pays off worldbuilding from the shelved original Farm Fury project (see Section 3) that has never shipped anywhere
- The Robot Overlord — already the established final villain across Farm Fury (main) and Farm Fury: Rush — gets its platformer send-off here, tying all three active-or-shelved titles to one ongoing story

---

## 2. Game Overview

**Core Gameplay Loop:** World Select → Level Select (any unlocked level) → Character Select (any unlocked character) → Gameplay (run, jump, use ability, defeat robots, find crops and secrets, reach the goal) → Level Complete or Level Failed → back to Level Select → repeat, optionally replaying with a different character for that level's character-gated secret.

**Session Length (planning assumption, to be validated like every other Farm Fury title):**

| Metric | Planning assumption |
|---|---|
| Average session | 8–15 minutes |
| Individual level | 60–180 seconds |
| Session structure | 4–8 levels per typical session |

**Player Fantasy:** The player IS the farm animal on the front line — not sneaking (Arcade) or fleeing/pursuing (Rush), but physically storming through occupied territory, level by level, animal-versus-machine.

---

## 3. The Farm Fury: Stampede Universe

**Narrative Context.** After the Harvest Robots' initial occupation (the setting of the original, shelved Farm Fury) and while Arcade's night-time crop reclamation and Rush's cross-country pursuit are both underway, Stampede is the direct assault: the animals pushing on foot through six robot-held territories, clearing each one out structure by structure, ending at the Robot Mothership itself.

**A note on worldbuilding reuse.** The original Farm Fury (physics-destruction) project is shelved with no monetisation or backend ever built — but its six worlds were fully designed and never used in a shipped product: Meadow Ruins, Frozen Tundra, Watermill Village, Sky Islands, Sunken City, and the Robot Mothership. Stampede reuses these six territories and their environmental identity (reframed as walkable, platformable levels rather than launch-trajectory backdrops) rather than reusing Arcade's Corn Field/Vegetable Patch/Orchard set. Two reasons: it gives Stampede's world progression the kind of visual variety a platformer needs (grassland → ice → village → sky → underwater ruins → space, a real escalation ladder, similar in shape to a classic Mario world progression), and it means genuinely unused design work — world descriptions, mood, the Robot Overlord's presence in the finale — finally ships somewhere instead of sitting in a shelved repository. It also avoids reusing Arcade's exact same seven farm-field names for a different genre, which would blur the two games together in a player's mind.

**The Robot Overlord.** Already established as the final antagonist in both the original Farm Fury (its World 6 finale, "the Robot Overlord's core chamber") and Farm Fury: Rush (World 4's finale boss). Stampede's own final confrontation should be built as this same character's platformer appearance — not a new, unrelated final boss — so a player who's touched any Farm Fury title recognises the endgame.

**Setting Atmosphere:**

| Aspect | Design |
|---|---|
| Time of day | Varies by world — see Section 6 |
| Mood | Determined, scrappy, cartoon-triumphant — less stealthy than Arcade, less breathless than Rush |
| Colour palette | Escalates from warm/grounded (World 1) to cold/alien (World 6) — see Section 8 |
| Music | Marching, upbeat per-world themes; a distinct motif for the Robot Overlord reused/echoed from wherever it's established in the other titles, if that motif exists |

---

## 4. Characters

All eight animals return. Each has exactly one platforming-relevant ability, translated from the same "rooted in real animal nature" identity the franchise already established in Arcade and Rush — reusing that identity rather than inventing new gimmicks keeps the character legible across all three active titles.

| Character | Ability | Effect | Obstacle specialty |
|---|---|---|---|
| Cluck the Chicken | Flutter Jump | A brief mid-air flutter that extends airtime and grants a second jump. | Wide gaps, timing-sensitive platforming |
| Bessie the Cow | Ground Pound | A heavy stomp on landing that breaks cracked floor tiles and instantly defeats any robot directly beneath her. | Hidden floors, ground-level secrets |
| Percy the Pig | Roll Dash | Curls into a ball and dashes forward at speed, defeating robots on contact and clearing small gaps. | Speed sections, low tunnels |
| Woolly the Sheep | Cloud Step | Spawns a temporary wool platform underfoot mid-air, usable once per jump. | Vertical sections, otherwise-unreachable ledges |
| Ducky the Duck | Skip Dash | Skims across water surfaces and short hazard gaps; the only character who doesn't sink or take damage from water tiles. | Water levels, flooded sections |
| Horace the Horse | Rear Vault & Horseshoe Throw | The highest single jump of any character; can also throw a horseshoe forward to defeat a robot at range. | Tall obstacles, robots on unreachable ledges |
| Gerald the Turkey | Puff Glide | Inflates and glides slowly downward, crossing wide chasms; defeats robots on contact while inflated. | Extended aerial sections, long chasms |
| Billy the Goat | Charge Break | Charges forward, breaking cracked walls and robot barriers no other character can break. | Walled-off secrets, robot-barrier shortcuts |

**Design rule:** every main path through every level must be completable by *any* unlocked character — Billy's wall-break, Ducky's water-immunity, etc. gate secrets and bonus areas, never the critical path. This mirrors Arcade's own launch rule ("every level genuinely completable by a skilled free player") extended from monetisation-gating to character-gating.

**Unlock thresholds** reuse the exact cadence already established in both Arcade (mazes completed) and Rush (runs completed) for franchise consistency — a returning player recognises the rhythm immediately:

| Character | Unlock requirement |
|---|---|
| Cluck | Available from start |
| Bessie | Available from start |
| Percy | Complete 5 levels |
| Woolly | Complete 10 levels |
| Ducky | Complete 15 levels |
| Horace | Complete 20 levels |
| Gerald | Complete 30 levels |
| Billy | Complete 40 levels |

**Enemy roster — Harvest Robots.** Reuses the same faction identity as Arcade and Rush rather than inventing new enemies:

| Robot | Platformer behaviour |
|---|---|
| Harvester (ground patrol) | Walks a fixed patrol path; stomped like a classic ground enemy |
| Scout (fast patrol) | Faster patrol, sometimes reverses on spotting the player |
| Drone (flying) | Hovers at a fixed height in a small patrol loop; must be jumped into from above or hit with Horace's horseshoe |
| Barrier Unit (stationary) | Blocks a path outright; only Billy's Charge Break clears it — the platformer's version of Rush's "requires Billy or Bessie to destroy" barrier robots |
| Chaser | Appears in specific levels, pursues at a fixed speed slightly slower than the player — pressure without being an instant-fail |

---

## 5. Gameplay Mechanics

**Movement.** Run (auto-accelerate on hold), jump (with coyote time and a jump-buffer window — hold to jump higher, tap for a short hop), and each character's unique ability on a short per-level-use limit rather than a real-time cooldown (fits a level-based game better than Arcade/Rush's timer-based cooldowns — see Section 12 for why this needs a genuinely different controller from Rush's).

**Crops & Coins.** Reuses Arcade's established "crop pellet" terminology and tiering for cross-game consistency: a common crop tier (small, everywhere) and a rarer crop tier (fewer, higher value), plus a universal coin pickup per level.

**Power-ups**, translating the classic Mario item set into franchise theming rather than borrowing Mario's actual items:
- **Super Feed** — temporarily grows the character, breaking blocks on contact without needing Bessie's Ground Pound
- **Hot Sauce** — grants a short-range thrown projectile for any character, temporarily
- **Energy Drink** — brief invincibility
- **Golden Egg** — an extra life

**Traversal feature — Irrigation Pipes.** Physical pipe/tunnel entrances that connect two points in a level or lead to a bonus area. Deliberately named differently from Arcade's "Warp Tunnel" glossary entry, even though both are Pac-Man/Mario-style teleportation devices — Arcade's is an instant maze-edge wraparound, this is a walked-into physical space, and giving them different names avoids the two mechanics blurring together across games that share a glossary.

**Scoring.** 1–3 stars per level based on crops collected, time, and whether the character-gated secret was found — not a points-chase like Arcade's chain-scoring, since a platformer's satisfaction is completion and discovery, not combo maximisation.

---

## 6. Level Design

**Six worlds, reused from the shelved Farm Fury's design work (Section 3), reframed as platformer territories:**

| World | Reused from | Platformer identity | Approx. levels |
|---|---|---|---|
| 1. Meadow Ruins | Farm Fury World 1 | Grassland tutorial world, wood/stone robot outposts | 8 |
| 2. Frozen Tundra | Farm Fury World 2 | Ice physics — reduced traction on ice tiles, frozen lake platforming | 8 |
| 3. Watermill Village | Farm Fury World 3 | Water-wheel village, timed fire-spread hazards reinterpreted as platforming obstacles | 8 |
| 4. Sky Islands | Farm Fury World 4 | Vertical platforming across floating islands, wind gusts push mid-air trajectories | 8 |
| 5. Sunken City | Farm Fury World 5 | Flooded ruins — Ducky-favoured world, underwater sections with reduced gravity | 8 |
| 6. Robot Mothership | Farm Fury World 6 | Zero-G platforming twist, Robot Overlord final boss | 6 + boss |

46 levels total for v1 launch scope — deliberately smaller than Arcade's 175 mazes or Rush's endless-plus-4-worlds, because hand-built platformer levels take materially longer to design and test than maze layouts or procedural runner chunks. Expand post-launch rather than over-scoping v1.

**Character-gated secrets.** Every level has at minimum one Billy-only wall-break secret or one Ducky-only water section or similar — the level design brief for each level should name which character(s) get a bonus area, so the content is deliberate rather than incidental.

**Boss levels.** One per world, ending in the world's own Robot Commander-tier fortress boss (reusing the "elaborate Robot Commander fortress" concept from the original Farm Fury's own World 1 boss language), culminating in the Robot Overlord finale in World 6.

---

## 7. User Interface

- **World Select** — six world cards (matching the world table above), locked/unlocked state, stars earned
- **Level Select (within world)** — grid of level tiles, lock state, star rating, a small icon marking any level with an as-yet-undiscovered character secret once a level's been completed with only one character
- **Character Select (before a level)** — grid of unlocked characters with their ability described; picking one is a level *attempt*, not a permanent choice — replay the same level with a different character freely
- **Gameplay HUD** — crops/score (top-left), lives (top-right), character portrait + ability-uses-remaining (bottom-left), pause (top-centre)
- **Level Complete** — stars earned, crops collected, whether this run found the character-gated secret
- Reuses Arcade's proven navigation pattern where sensible (Shop hub off Settings, not off Main Menu) rather than inventing new IA for the same purchases

---

## 8. Visual Design

**Art style:** Consistent with the franchise — warm cartoon aesthetic via Kling AI. Escalates in palette from warm/grounded (Meadow Ruins) to cold/mechanical (Robot Mothership), giving the world progression a visible arc the way classic platformer world palettes do.

**Sprite requirements per character:** run cycle, jump/fall, ability-activation animation, victory pose, defeat/respawn animation — a larger art lift per character than Arcade's needed (which reused a lot of directional-facing shortcuts), since a platformer's camera holds on the character far longer than a maze's top-down view does. Budget for this specifically; it's the most likely place this project's art timeline slips.

---

## 9. Audio Design

Per-world music themes, reusing the franchise's established practice from Arcade (dedicated track per world, crossfade for power-up windows). Per-character ability sound cues, matching Arcade's "mechanical cue, not a voice bark" choice (Section 9 of the Arcade GDD) rather than Rush's planned voice-bark approach — Arcade's choice already shipped and read fine; no reason to relitigate it per title.

---

## 10. Progression Systems

**Coin economy** and **cosmetics categories** (Hats, Dust Trails, Costumes as full character skins) should mirror Arcade's *actual, tested* pricing and structure (Section 11 of the Arcade GDD) rather than inventing new numbers — Arcade is the one title in the franchise with confirmed working purchases end-to-end, so its pricing is the franchise's proven baseline, not just its original design guess.

---

## 11. Monetisation

Same ethical F2P stance as the rest of the franchise: no energy walls, no pay-to-win, no loot boxes.

| Stream | Model | Reused from |
|---|---|---|
| Rewarded ads | Continue after death, double coins on level complete, refill ability-uses | Arcade's proven placements |
| Interstitial | Every N levels, skipped if Remove Ads owned | Arcade |
| Remove Ads | $4.99, includes bonus coins | Arcade's exact price point |
| Coin packs | $0.99 / $3.99 / $9.99 / $19.99 | Arcade's exact, already-store-registered tiers |
| Cosmetics | Hats/Trails $1.99, character Costumes $3.99 | Arcade's exact, already-tested tiers |
| World purchase | Worlds 1–3 free, Worlds 4–6 at $3.99 each (or a $9.99 bundle for all three) | Arcade's proven "whole world, one purchase" model, plus a bundle option Arcade doesn't have |

Reusing Arcade's exact, store-registered price points isn't just convenient — Arcade already paid the cost of finding out these numbers work (registered, purchase-tested end-to-end on iOS TestFlight per its v2.4 GDD). Stampede inventing its own pricing from scratch would be re-paying a cost the franchise already covered.

---

## 12. Technical Specifications

**Stack**, matching the rest of the franchise exactly: Unity 6.5, 2D template, Universal Render Pipeline, C#, Claude Code as primary engineering collaborator, Kling AI for art, Git/GitHub.

**A genuinely different controller from Rush's, despite the shared engine.** Rush's Phase 2 learned a real lesson worth carrying over explicitly: Unity's Physics2D ignores the Z axis entirely for collision, and anything that needs to scroll or move reliably against colliders needs a **kinematic `Rigidbody2D` moved via `MovePosition`**, not `transform.Translate` (see Rush's `CLAUDE.md`, Phase 2 architecture notes). That lesson applies directly to Stampede's character controller too — a platformer needs precise, collision-reliable movement even more than a runner does. Where Stampede's controller genuinely differs from Rush's: Rush keeps the character fixed in X while the world scrolls past it; Stampede needs the more conventional platformer setup where the *character* actually traverses a level built in Tilemap space, with a Cinemachine-style camera following it — closer to a standard 2D platformer controller than to Rush's scrolling-world trick.

**Level authoring:** Unity Tilemap + Composite Collider2D for level geometry, with level content still defined as data (ScriptableObject level descriptors referencing tilemap layouts) rather than 46 fully bespoke scenes — matching the franchise's established single-scene, data-driven convention (Arcade's `Resources.LoadAll` approach, Rush's Inspector-list approach) rather than breaking from it just because the genre changed.

**Backend:** plan for the same shared Supabase account as Rush and the franchise's stated cross-game intent — but build it the way Arcade actually proved out (Unity IAP's async `UnityIAPServices`/`StoreController` API, LevelPlay ad mediation), not the way Rush has only *planned* but not yet reached in its six-phase build order. Don't inherit Rush's unverified backend assumptions; inherit Arcade's verified ones.

**Object pooling** for robots, crops, and particle effects — a platformer with several enemies and pickups on screen per level needs this from early on, not as a later optimisation pass.

---

## 13. Development Roadmap — Six Phases

Mirrors the phase structure that's already worked for Rush (see its `CLAUDE.md`), adapted for a platformer. Each phase is its own Claude Code session, started only once the previous phase is verified working in the Unity editor.

1. **Project foundation** — folder structure, core singletons (GameManager, DataManager, SaveManager, AudioManager), ScriptableObject definitions (CharacterData, RobotData, LevelData, WorldData), enums, empty scene, placeholder data. No gameplay. **Built; Play-mode verification still pending** — see `CLAUDE.md`.
2. **Core movement & controls** — Cluck only. Kinematic Rigidbody2D + MovePosition movement, coyote time, jump buffering, one hand-built test level in Meadow Ruins. Priority is game feel — this is the phase most likely to need real iteration time, same as it was for Rush.
3. **Level system & World 1** — Tilemap-based level loading from ScriptableObject descriptors, all 8 Meadow Ruins levels, ground-patrol and drone robot AI, checkpoint/level-complete flow, crop collection.
4. **Characters & abilities** — remaining 7 characters and all 8 unique abilities, character-select-before-level UI, ability-gated secrets, the 5/10/15/20/30/40 unlock ladder.
5. **Remaining worlds, bosses & progression UI** — Worlds 2–6, per-world bosses, Robot Overlord finale, World Select/Level Select screens, HUD, pause.
6. **Monetisation & polish** — Unity IAP (Arcade's proven product catalog, adapted), LevelPlay ad mediation, Firebase Analytics, Supabase cloud save, cross-promo, tutorial, launch polish.

---

## 14. Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Character art budget underestimated | A platformer's camera holds on character sprites far longer than Arcade's top-down maze view did — budget run/jump/ability/victory/defeat animation per character explicitly, don't assume Arcade's sprite scope transfers. |
| Controller feel is wrong | This is the single highest-risk phase (Phase 2), same as it was for Rush. Don't proceed to level content until jump/movement genuinely feels good in playtesting. |
| Three franchise titles blur together for players | Deliberately different core loop (character-before-level vs. character-swap-mid-play) and deliberately different terminology (Irrigation Pipe vs. Warp Tunnel) are the mitigations designed into this document — revisit if playtesting shows they're not enough. |
| Reused shelved-project worldbuilding doesn't actually fit a platformer once built | The original Farm Fury's world descriptions were written for a launcher-physics game, not a platformer — verify each world's signature mechanic (ricochet, fire-spread, current lanes, zero-G) translates to a fun platforming obstacle before committing full art/level budget to it, world by world. |
| Reinventing monetisation numbers Arcade already validated | Mitigated by design — Section 11 explicitly reuses Arcade's tested pricing rather than proposing new figures. |

---

## 15. Appendix

**Glossary (Stampede-specific additions to the franchise glossary):**

| Term | Meaning |
|---|---|
| Irrigation Pipe | A physical pipe/tunnel entrance connecting two points in a level or leading to a bonus area — Stampede's equivalent of a Mario warp pipe. Not the same mechanic as Arcade's "Warp Tunnel." |
| Character-gated secret | A bonus area or shortcut reachable only by one specific character's ability |
| Ability-uses-remaining | Stampede's per-level ability limiter, replacing Arcade/Rush's real-time cooldown model |

**Document Version History:**
- v1.0 — Initial GDD. Concept established: Mario-Bros-style platformer, deliberately differentiated from Rush's endless-runner mechanic and Arcade's mid-maze-swap mechanic. Reuses the shelved original Farm Fury's six unshipped worlds and its Robot Overlord as final boss. Monetisation and pricing deliberately copied from Arcade's tested, store-verified model rather than newly invented.
