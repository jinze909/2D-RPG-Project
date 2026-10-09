# Sentinel tactics — iteration 6

The existing clearing needs distinct threats after persistent growth makes repeats
useful. Keep the three-enemy fight, rewards, gate, beacon, hero and local profile;
give each enemy a different readable counter instead of adding unrelated systems.
This document is a design/implementation contract, not native play acceptance.

| Spawn | Role and counter | Admission / movement | Locked contact footprint | Windup / active / recovery |
| --- | --- | --- | --- | --- |
| 0 | Warden: leave the square | Strike within .85; approach within 3.7 | 1.9 square at enemy origin | .65 / .12 / .7 s |
| 1 | Lancer: sidestep the lane | Thrust within 2.8; approach within 5 | 3.2 × .7 lane forward from origin | .8 / .16 / 1 s |
| 2 | Seer: leave the mark | Retreat below 2.2; cast within 3.4; approach within 6 | 1.4 square at the player's captured foot | 1 / .18 / 1 s |

The Lancer stays still during its thrust. The Seer casts when retreat is blocked
and the player is visible; otherwise a wall-pinned caster would cease functioning.
All retain 54 HP, 2 damage and ten run-local coins. J/Space, K, mana/cooldowns,
shared immunity, E completion, saved rewards, result upgrades and R remain intact.
The player can move during attacks, approach between warnings and use either
existing attack against all roles. No new player input or save schema is needed.

Capture one immutable attack footprint when the rule admits a telegraph. Warning
position/orientation and contact tests share that snapshot; neither follows a
moving target. Physics-boundary LOS checks guard attack admission and accepted
contact. Movement stops throughout telegraph/active/recovery. Recovery is the
enemy cooldown. A large tick that skips the active window cannot deal late damage.
Resolve every player contact before enemy retaliation as in iteration 4.

Warning borders are cached at the role's real width/height, with a constant
two-pixel stroke at PPU30. An interior fill shows windup; an X shows the active
window. Spear/crest/rune silhouettes distinguish roles without relying on color.
Use the existing 15-color palette and cached primitives; preserve original hero
images, imports, animation clips and gait. Help stays in the existing edge slot:
`Lancer: sidestep lane`, `Seer: leave mark`. Accepted contacts/kills retain the
existing deduplicated cues; no new sound or audio-audition result is claimed.

Pause freezes the snapshot and timing. Death, completion, disable and retry clear
it together with transient presentation. Disable preserves health, saved growth,
coins, cooldowns and immunity; reset rebuilds the same roles. No per-frame texture,
mesh or component creation is needed. One managed footprint is allocated per
admitted enemy attack, not per physics tick.

## Verification and remaining acceptance

Pure production tests cover decisions, phase timing, finite/degenerate geometry,
locked targets, mixed-role contacts, cancellation, immunity and unchanged rewards.
Production Runtime/Hud methods must additionally demonstrate actual role wiring,
retreat/blocked fallback, warning/contact agreement, pause/disable/retry and safe
help rectangles using explicitly limited recording boundaries. Existing movement,
resource, progression, animation/scene/raster/LFS/meta checks remain required.

Native Unity 2022.3.53f1 import, input, wall collision, visual/glyph readability,
audition and balancing remain unrun in this environment and optional for merging.
When available: sidestep a locked lance, move out of a captured sigil, corner the
Seer, cross-check diagonal warning/contact edges and complete/retry the saved loop.

## Versioned API references

Unity 2022.3 [Rigidbody2D.MovePosition](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D.MovePosition.html)
confirms short moves on physics ticks and subsequent collider response. This
change retains the existing body contract rather than introducing a dash/teleport.
[Physics2D.Linecast](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Physics2D.Linecast.html)
provides layer-filtered LOS including origin-inside-collider hits; scene actors
remain on Ignore Raycast and walls on Default. Recording tests verify requested
endpoints/masks only; real wall response remains a native acceptance item.
