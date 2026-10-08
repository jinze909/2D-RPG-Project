# Combat Clearing — iteration 2 candidate

Open `Assets/Scenes/CombatClearing.unity` in international Unity 2022.3.53f1.
It is the candidate's first build scene; SampleScene remains available unchanged.
This is an implemented small combat/objective slice, not a recovered Starfall
checkpoint or a complete RPG. Native import, play, graphics and audio acceptance
remain pending because the cloud editor has no valid license.

## Implemented loop and controls

Move with WASD, arrows, left stick or D-pad. Clear three moss sentinels, cross the
opened north seal and press E near the beacon. Each defeated sentinel awards
10 run-local coins exactly once. No persistent economy/save or equipment is claimed.

| Action | Control | Rule |
| --- | --- | --- |
| Light strike | J / Space | 18 damage, 0.45 s cooldown, no mana |
| Mana burst | K | 30 damage, 6 MP, 1.2 s cooldown |
| Beacon | E | Nearby, alive, all three sentinels defeated |
| Retry | R | After defeat/completion; resources, enemies, gate and cooldowns reset |
| Pause | Esc | Combat/mana regeneration freeze; resume clears cached motion |
| Mute | M | Synthesized feedback cues |

Mana regenerates at 0.8 MP/s during an active run. Sentinels have 54 HP and deal
2 damage after a 0.65 s stationary orange square warning. The contact window is
0.12 s, recovery 0.7 s and shared player immunity 0.55 s. Warning and damage use
the same saved footprint. Walls obstruct damage. A player action hits each enemy
at most once; aim is fixed for that action while locomotion remains responsive.

The blonde Viola sheets/controller and PPU30 are preserved. Death now holds an
existing front pose instead of endlessly spinning through directions; this is a
temporary defeat presentation, not newly authored death art. Environment and
sentinels are original deterministic code-native blockout shapes. Four original
synthesized cues are cached once and mixed quietly with three pitch variants.
Neither their sound quality nor native visual consistency has been accepted yet.

## Architecture and state ownership

`Player.Awake` creates an independent stats clone and fills valid resource maxima.
`PlayerHealth` / `PlayerMana` use that actor's clone. Retry restores this session;
authoring assets remain unchanged. The prototype P damage key is disabled in the
clearing. ClearingRun owns cooldowns, enemy health/phases, attack tokens, run-local
rewards and completion; it owns no duplicate player HP/MP.

ClearingRuntime runs after player input, synchronizes health before accepting
actions, uses fixed simulation for contacts/regeneration and restores time scale
when a paused component/scene is disabled. The gate fills the entire gap in a
partition connected to overlapping outer walls. The beacon checks run completion
as well as actual player proximity. The Canvas HUD reserves screen edges, has no
full-screen combat panel/raycast targets, and sizes its columns to the viewport.

The older `jinze909/rpg-by-ai` reference was inspected at
`b0aae3d329c5b0b200b30b1457d0f692257b9f06`. Its foot-clearance, immutable contact
and threat-readable HUD ideas informed this implementation. Its OnGUI architecture,
assets, save data and unfinished north-arrow candidates were not transplanted.

## Required native acceptance before merge

1. Import all packages with no console errors in licensed international 2022.3.53f1.
2. Check all movement directions, quick turns, direction+attack on one frame,
   walking while attacking, analog partial speed and stop/restart. Review the
   existing walk slice height/center-pivot drift and final duplicated frame.
3. Press against all four boundaries and both partition joins; the sealed beacon
   region must be unreachable until all three sentinels die. Verify real colliders,
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
acceptance results. The candidate remains isolated until that evidence exists.
