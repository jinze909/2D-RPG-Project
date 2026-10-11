# Thornwood: the first complete forest expedition

## Player loop and controls

Play `Assets/Scenes/CombatClearing.unity` in Unity2022.3.53f1. Complete the existing
three-guard -> altar E -> Guardian -> north-beacon E sequence and successfully
save its30coin reward. While alive and unpaused at that beacon, **F** enters the
Thornwood trail. A failed clearing save keeps the entrance locked; E retries it.
This is a runtime-built forest pocket in the existing scene, not a separate
serialized Scene or a full world-map/quest system.

Explore with the existing WASD/arrows/analog/D-pad movement. **J/Space** is the free
18HP light attack; **K** is the30HP burst costing6MP, with existing .45/1.2-second
cooldowns and .8MP/second regeneration. Collect all three seed pods with **E**
within .75 foot-units and clear wall LOS; defeat all three Briar Stalkers. Then
**E** at the north root cache completes this expedition and saves30coins into the
same local growth profile. Seeds and kills never deposit coins early.

The south arrow always offers **E** to return while alive/unpaused, even before
objectives finish or when the cache save fails. Return places the actor exactly
at the clearing beacon `(0,3.35)`, so the existing completed-clearing control lock
cannot strand the player away from F/shop. Reenter with F: seeds, enemies, HP/MP,
cooldowns/immunity and reward identity remain the same attempt. Traveling does
not refill resources, restock seeds, resurrect enemies or award another reward.
After forest completion the player can still walk to the exit; attacks are closed.

**R** after forest death or completion creates a genuine new forest attempt;
a dead player must retry before returning. First forest admission and genuine R
fill the same actor-owned resources and apply currently saved bonuses from captured
base maxima once. **Esc** pauses; **M** toggles the shared audio mute. A genuine
clearing R discards its old forest expedition. If the forest cache was completed
but unsaved, this restart explicitly reports loss of that unbanked reward.
No attacks or stale clearing views leak through the successful F entry frame.

## Region layout and admission

`ThornwoodLayout` is the common authority for art, actual colliders, LOS and
conservative movement checks. Bounds are x14..30/y-5..5. Four full continuous
boundary rectangles overlap at corners; six root obstacles share exact authored
rectangles. The forest's foot radius is .23; movement must accept the complete
swept segment from its last safe point, not only an endpoint beyond a trunk.
Large out-of-bounds/through-trunk movement is restored to the last accepted point.
Stalker approach also checks a conservative swept segment and Default-wall LOS.

| Location | World foot coordinates |
| --- | --- |
| South entry/return sign | (22,-3.8) |
| West seed | (18,-.8) |
| East seed | (26,.5) |
| North seed | (22,2.3) |
| North reward cache | (22,3.5) |
| Return clearing beacon | (0,3.35) |

The goal never adds a wall over the exit. Invalid IDs, nonfinite/out-of-region
coordinates, distance, paused/dead/outside-expedition actions and root-blocked
LOS cannot collect or complete. A conservative actual-C# graph finds2934 sampled
safe nodes connected by swept-radius-clear edges, including objectives/entry.
That is static/logic geometry evidence, not native collision or a player playthrough.

## Briar Stalker combat and feedback

Three new root-hound enemies use54HP each. Unlike the clearing's stationary Lancer,
they pursue clear nearby targets at1.55units/second, within6units, then admit a
locked directional pounce within2.4units. The existing Lance timing/footprint
remains authoritative: .8second windup, .16second active,1second recovery. Once
admitted, origin/aim/rectangle are immutable and movement holds until Ready.
Sidestep the advertised lane rather than follow a moving target marker.

Actual contacts require active-time captured geometry and clear original-origin
wall LOS; a pounce resolves once and deals2HP with the existing .55second shared
player immunity. Every player contact resolves before enemy retaliation, so a
stalker killed that tick cannot retaliate. Light/burst/hurt numbers report actual
clamped HP loss, including overkill. Costs/cooldowns are not refunded by travel,
disable or pause. Dead actors stop colliding; independent kill strokes briefly
survive body hiding and cannot resurrect attacks.

Six cached fixed-anchor46x36 original pixel poses cover ready, two walking frames,
crouched anticipation, extended pounce and hurt. Warnings use the same immutable
contact rectangle with inset border/phase fill and direction strokes. Three seed
pods have distinct leaf silhouettes; cache lid/lock changes when opened; a south
arrow indicates return. Dark forest floor, roots/trees and stepping trails preserve
Point/Clamp/no mipmaps/FullRect/PPU30/shared15palette. Static raster decoration aligns
to the existing pixel grid. Original blonde hero textures/Animator remain intact.
Owned forest sprites/textures are cached once and destroyed once; the borrowed
clearing square remains owned by ClearingVisuals.

Audio reuses existing accepted attack/hit/kill/hurt/reward cues and mute. Accepted
seed collection and saved cache completion use Reward; rejected requests are not
successful actions. No new music/ambience clip or native sound audition is claimed.

## Growth, save failure and same-attempt banking

`ClearingRuntime` and `ThornwoodRuntime` share one ClearingProgress instance and
its existing save-before-mutate transactions. Cache completion saves the existing
30coins and can fund the same HP/MP rank purchases when returning to the clearing
result shop. Existing rank prices/bounds and next-new-attempt bonus calculation
remain. The v1 schema/ledger equation stays unchanged: ClearedRuns now counts every
completed30coin clearing or forest expedition, not only clearing map wins.
No seed inventory, completed-map flag, enemy/position state or unfinished-expedition
persistence was added. Quitting/reloading does not restore a mid-forest attempt.

The old v1 profile keeps only LastRewardId. That is sufficient for a single live
completion, but two simultaneously retained completed activities expose a replay
risk: clearing A30 -> forest B60 -> A again could incorrectly become90. Each owner
now retains an accepted-bank latch, setting it only after Saved/AlreadyBanked and
preserving it on leave/reentry/disable. A true new attempt alone clears it. Return
and reentry therefore cannot repeat either transaction after the other ID becomes
latest. A deterministic mutant removing the clearing latch reproduces three failed
integration cases; the corrected actual paths stay60 before another genuine win.
This is live-attempt idempotency, not a new unbounded historical replay ledger.

A failed/future-protected save leaves live coins/ranks unchanged, retains the same
cache ID for E retry and leaves the return route open. Leaving/reentry preserves
an unsaved completed cache; R explicitly discards it. Save-store limits/backups/
stale-writer/future-file protection remain. No arbitrary bonus-currency migration
or automatic profile reset occurs.

## Lifecycle and evidence

Pause freezes the authoritative simulation clock, warnings, cooldowns, feedback
and numbers; it does not cancel paid actions. Component disable/leave cancels
transient contact tokens, warning/impact/number/audio deadlines while preserving
attempt effort and saved latches. Death ends combat and offers forest R; new
attempt reset clears seeds/enemies/contacts and rearms resources/reward identity.
The clearing owner delegates Update/FixedUpdate/LateUpdate while forest active,
then restores clearing HUD/camera/result control. Forest disposal precedes shared
art disposal. Guardian108HP/2damage/timings and old clearing/supplies stay untouched.

Actual fresh verification is archived with the eleventh handoff:31 pure forest
logic/layout groups,35 actual-method recording-boundary integration groups, all
retained regressions (445project+6distribution=451 total), five static envelopes
and30production-source API compile against86realUnity2022.3engine/Editor/uGUI
references. Input System remains an explicit substitute. See the final exact CI
and external delivery receipt for source publication; no planned upload is proof.

**尚未进行 Unity 原生验收**: real Editor import/EditMode/PlayMode/input/physics,
forest corner movement, camera/text/pixel/depth/animation, audible output/mix,
forest/Boss difficulty and real player playtests/build. Offline pass counts and
raster previews establish none of those experiences. Do not repeat unchanged
license/Editor/MCP probes.

Relevant checked official2022.3 APIs:
[MovePosition](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D.MovePosition.html)
(use small moves from FixedUpdate; real collision response still needs native tests),
[Physics2D.Linecast](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Physics2D.Linecast.html)
(Default-wall mask plus authored layout LOS), and
[Sprite.Create](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Sprite.Create.html)
(explicit pivot/FullRect/PPU30; native rendering not inferred).
