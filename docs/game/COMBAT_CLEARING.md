# Combat Clearing — integrated ninth-round Guardian

Open `Assets/Scenes/CombatClearing.unity` in international Unity 2022.3.53f1.
It is the candidate's first build scene; SampleScene remains available unchanged.
This is an implemented small combat/objective slice, not a recovered Starfall
checkpoint or a complete RPG. Native import, play, graphics and audio acceptance
remain pending because the cloud editor has no valid license.

## Implemented loop and controls

Move with WASD, arrows, left stick or D-pad. Clear three moss sentinels, approach
the central altar and press E to awaken the Guardian. Kill it, cross the opened
north seal and press E near the beacon. See BOSS_ENCOUNTER.md for locked patterns,
half-health timing and entry/resource contracts. Each defeated sentinel awards
10 run-local coins exactly once. E at the completed beacon saves all 30 coins;
1/2 (including keypad) buys bounded HP/MP upgrades on the result screen and R
applies them next run. See CLEARING_PROGRESSION.md for failures and save protection.
Equipment/world-state saves are absent.

| Action | Control | Rule |
| --- | --- | --- |
| Light strike | J / Space | 18 damage, 0.45 s cooldown, no mana |
| Mana burst | K | 30 damage, 6 MP, 1.2 s cooldown |
| Altar | E | Within .8 foot-units, clear wall LOS, all three guards dead; awaken once |
| Beacon | E | Nearby, alive, all three guards and Guardian defeated |
| Supply | E | Nearby discovered reserve; beacon/altar take priority |
| Upgrades | 1 / 2 or keypad | After completion/banking; +2 max HP/MP per rank next run |
| Retry | R | After defeat/completion; reset encounter; preserve bank/ranks |
| Pause | Esc | Combat/mana regeneration freeze; resume clears cached motion |
| Mute | M | Synthesized feedback cues |

Mana regenerates at 0.8 MP/s during an active run. All sentinels have 54 HP and
deal 2 damage with shared player immunity of 0.55 s. Iteration 6 gives the three
spawns distinct roles: Warden square sweep, stationary Lancer forward lane, and
Seer spacing/target-locked sigil. Role timing, movement and counters are specified
in [SENTINEL_TACTICS.md](SENTINEL_TACTICS.md). Warning and damage use
the same immutable saved footprint. Walls obstruct damage. A player action hits each enemy
at most once; aim is fixed for that action while locomotion remains responsive.
Each physics tick resolves all admitted player contacts before the surviving
sentinels' and Guardian's contacts. A lethal sentinel's index does not truncate a multi-target
burst, and a sentinel killed on that tick cannot retaliate.

The blonde Viola sheets/controller and PPU30 are preserved. Death now holds an
existing front pose instead of endlessly spinning through directions; this is a
temporary defeat presentation, not newly authored death art. Environment and
sentinels now share 15-color original deterministic pixel rasters at PPU30 with
Point filtering, no mipmaps and explicit FullRect sprites. Stone courses, quiet
moss ground, booted sentinels and carved rune/crystal shapes replace the flat
blockout. Six original synthesized cues are cached once and mixed quietly with
three pitch variants; accepted contact and kills now have distinct confirmation.
Neither their sound quality nor native visual consistency has been accepted yet.

The orange warning keeps each attack's fixed damage outline while its interior
fills over the windup. During the active window an X motif distinguishes it from
charging. Hit pulses last 0.12 seconds and kill pulses 0.3 seconds, including after
the enemy body hides. All feedback is preallocated and follows simulation time:
pause freezes it, retry/disable clears it. A multi-target action emits at most one
hit cue and one later kill upgrade. Player damage/costs, rewards and gate rules remain unchanged; the new enemy
roles have distinct windup/contact/recovery times. HUD messages replace the bottom-right help text rather than occupying
the central combat band; unavailable actions/help hide on pause, defeat or victory.

## Architecture and state ownership

`Player.Awake` creates an independent stats clone and fills valid resource maxima.
`PlayerHealth` / `PlayerMana` use that actor's clone. Retry restores this session;
authoring assets remain unchanged. The prototype P damage key is disabled in the
clearing. ClearingRun owns cooldowns, enemy health/phases, attack tokens, run-local
rewards and completion; it owns no duplicate player HP/MP.

ClearingRuntime runs after player input, synchronizes health before accepting
actions, uses fixed simulation for contacts/regeneration and restores time scale
when a paused component/scene is disabled. Disable invalidates current contact
tokens and clears warning/impact deadlines, retaining health, mana, progress,
cooldowns and immunity. Resume needs fresh attacks and telegraphs; normal pause
only freezes existing simulation state. The gate fills the entire gap in a
partition connected to overlapping outer walls. The beacon checks run completion
as well as actual player proximity. The Canvas HUD reserves screen edges, has no
full-screen combat panel/raycast targets, and sizes its columns to the viewport.

The older `jinze909/rpg-by-ai` reference was inspected at
`b0aae3d329c5b0b200b30b1457d0f692257b9f06`. Its foot-clearance, immutable contact
and threat-readable HUD ideas informed this implementation. Its OnGUI architecture,
assets, save data and unfinished north-arrow candidates were not transplanted.

## Optional native acceptance when tooling is available

1. Import all packages with no console errors in licensed international 2022.3.53f1.
2. Check all movement directions, quick turns, direction+attack on one frame,
   walking while attacking, analog partial speed and stop/restart. Review the
   existing walk slice height/center-pivot drift and final duplicated frame.
3. Press against all four boundaries and both partition joins; the sealed beacon
   region must be unreachable until all three sentinels and the Guardian die. Verify real colliders,
   foot alignment, no tunneling, and opening/re-closing on retry.
4. Check sentinel chase, warnings, one contact per attack, shared immunity,
   wall-blocked strikes, mana exhaustion/regeneration, pause/resume and rapid inputs.
5. Defeat/retry repeatedly, clear/retry, disable/enable while paused and reload the
   scene. Verify enemies stop on defeat and no MP/actions/objective can fire at 0 HP.
6. Check HUD/scene at 1920×1080, 1280×720, 1024×768, 720×720, 900×1600 and 2560×1080;
   mathematical bounds tests do not prove text layout, readability or screenshots.
7. Audition cues, variation/mute and level balance, then run a complete player
   playthrough and appropriate native EditMode/PlayMode/platform build checks.

Offline compilation/rules/geometry checks cannot establish any of these native
acceptance results. The latest user instruction permits ordinary reviewed merges
with sufficient available test/CI evidence; native checks remain explicitly unrun.
