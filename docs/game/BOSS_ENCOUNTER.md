# Moss Guardian — ninth-round encounter contract

Runtime/presentation integration and independent review are complete; GitHub/ZIP
publication follows the verified implementation checkpoint. Actual clearing opts into requiredBoss;
default ClearingRun supports existing isolated three-sentinel callers.

## Complete encounter

Defeat all three tactical guards; E within .8 of the central nonblocking altar
(0,-.7), with foot-position/wall LOS, awakens Guardian at(0,.6). Dormant cannot
be damaged. Boss awakening does not reset/heal/refill stats or reserves; it cancels
pre-awakening player tokens without refunding paid costs/cooldowns. Gate stays
sealed until108HPGuardian dies. Return north and E at beacon banks existing30coins;
Boss death/awakening alone never saves, grants upgrade or duplicates currency.
Defeat ->R retries all enemies/resources/charges and retains saved upgrades.

## Patterns and counters

Alternating Lance: captured forward4.2x.9lane, admitted within4units; sidestep.
Sigil: captured-player-foot1.8square, admitted within6units; leave mark.
Both deal2HP with existing shared.55s player immunity. Boss approaches at1unit/s
only whenReady/visible and beyond next attack range; it stops during warning,
active and recovery. LOS from captured caster guards contact. Existing J18damage
/.45s/noMP and K30damage/6MP/1.2s remain authoritative. Each action hits Boss once.

Phase1 windup1s, active.16s,recovery1.15s. At54HP or less, phase2 next attacks use
.8s warning/.16active/.9recovery. Every admitted attack freezes its durations and
footprint; crossing half HP never shortens an existing warning. A hitch that skips
active never deals late damage. Pause freezes simulation; interruption clears
contacts/snapshot but retains original ready deadline, health and next pattern.

## Presentation and validation boundary

Implemented cached original wide crowned moss/stone silhouettes and nonblocking altar,
shared palette/Point square with1/30world-unit geometry, authoritative constant
warning outline/fill/activeX, hurt/killpulse and fixed fifth actual-loss label.
Retain player slot3 and Boss slot4; use existing edge HUD for HP/phase/counter.
Mute/accepted one-shot deduplication/disposal remain. No per-frame object creation.
No new player bitmap, animation controller, scene or package rewrite is needed.
Fresh361project +6distribution =367checks pass;105presentation groups retain81
earlier groups,17scene groups retain15, and19pureBoss groups verify admission/
geometry/timing/gate/interrupts.26production scripts compile against86real2022.3
engine/Editor/uGUI references with Input System substituted. An independent reviewer
reran Boss19/presentation105/scene17 and found no blocking defect. Recorded cached
production geometry was visually inspected offline, not in native Game View.
Exact implementation/master CI and full sourceZIP must precede completion;
recording boundaries do not prove native gameplay.
Native physics,deviceinput,render/Animator,glyphs,audio,balance/build are optional
and unrun. Official2022.3 [MovePosition](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D.MovePosition.html)
and [Linecast](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Physics2D.Linecast.html)
documentation informs fixed-step movement, original-caster LOS and wall-layer filtering.
