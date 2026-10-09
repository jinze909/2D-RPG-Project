# Optional exploration supplies — eighth-round implementation contract

The clearing now offers one optional explore -> discover -> E -> restore -> fight
loop. Small detours support the existing tactical encounter: herbs sustain a hurt
player, while one mana rune can fund the existing six-MP burst. This adds an
exploration/timing choice without replacing saved progression, enemy roles or
actual-loss feedback. It does not add a Boss, inventory or world-state save.

## Ground points and admission

| Point | Player-foot coordinates | Per-attempt reserve |
| --- | --- | --- |
| Western herbs | (-5.6, -2.35) | At most 4 HP |
| Eastern rune | (5.6, 1.15) | At most 6 MP |

Both points are inside the existing seal and use the actual player-foot position,
not the hero's sprite/body center. Discovery requires distance at most 2.4 and a
clear Default-wall LOS. Discovery remains remembered for this attempt. E use
requires distance at most 0.8 plus LOS, a living/unpaused/incomplete run, discovery
and an unclaimed charge. Inclusive boundaries are deliberate.

ClearingSupplies validates finite resources, positive living HP, valid maxima and
bounded current HP/MP. It returns the actual representable clamped gain. Runtime
applies that gain to the same actor-owned PlayerStats snapshot used by damage,
mana costs and the resource HUD. Full, invalid, blocked/out-of-range or no-effective-
gain attempts preserve the charge. Coarse float rounding cannot exceed the
four/six-unit budget. A positive partial restoration consumes the whole charge.

Each point has one independent charge per run. There is no extra MP cost or
cooldown: the charge is the consumption limit. Supplies never bank coins, change
upgrade ranks, complete the objective or become persistent profile data. Existing
combat inputs, attack animation/effects, mana costs and cooldowns remain authoritative.

## Input, lifecycle and feedback

E near the north beacon retains the existing objective logic. Elsewhere it uses
the nearest discovered point in use range with LOS, or existing objective guidance
when no supply is available. Completed-run E still retries reward storage, and
terminal R cannot leak another action into the new attempt.

Accepted restoration displays the actual gain and invokes the existing Reward cue
once. Discovery/rejection is silent; full/spent/invalid hints explain why E will
not restore resources. HUD priority is available beacon -> nearby supply status
-> transient feedback -> movement guide, all in the existing edge slot. Pause,
death and completion suppress unavailable supply actions. No extra persistent HUD
panel or opaque combat overlay is added.

Disable/resume preserves discoveries and claimed charges. Only genuine retry or
new-run Reset restocks both points and clears their notices. Pause freezes the
simulation clock; terminal/disable/retry clears claim pulses without restocking
merely because a component was disabled.

## Pixel props and animation

ClearingSupplyVisuals creates two fixed nonblocking props using the clearing's
existing cached PPU1 Point square sprite and shared palette. Prop dimensions are
in 1/30-unit world pixels to match the established art scale; no new PPU30 raster
or texture is created. Herbs use a cross/leaf silhouette;
the rune uses a diamond, so kind does not depend only on color. Unseen props are
subdued, discovered reserves bright, claimed reserves empty with a notch.

A successful claim runs a 0.45 simulation-second border pulse with integer-pixel
growth and a palette-color change. All pieces are cached at construction; Refresh
reuses them. Recorded tests check no new GameObject/Texture/Sprite after startup,
not a zero-managed-allocation or native-profiler guarantee. The prop owner destroys
its roots; shared sprites/texture remain owned by ClearingVisuals.

## Verification and delivery boundary

Published foundation 2c99ecfc62d476bbe4bf30adb874c8b52254498e has exact successful
CI37995620781/all 12 steps, 303 project + 6 distribution checks and 23-source/86-real-
reference API compilation. The execution outage is resolved. Runtime/prop/HUD
integration is reviewed and normally merged through PR7. Fresh final validation passes 316 project + 6
distribution = 322 checks: 13 new pure supply groups, 12 new presentation groups
and one added geometry group retain all earlier checks. Actual-method presentation
totals 81, retaining the previous 69; scene contracts total 15. The rune E -> K
burst loop passes. The integrated 24 production scripts compile
against real Unity2022.3 engine/Editor/uGUI references, Input System substituted.
54 unique meta GUIDs/145 serialized references/six LFS objects/eight licensed
bundles are intact. The actual chained handoff validates 100/100 with three file
references. Implementation30588a2 exactCI37996558184 passed all12steps and
uploaded Artifact11647800082; PR7 normally merged atba41be1 with equal tree.
Final source/CI/ZIP/CRC/source/member SHA-256/HTTPS publication identities resolve
in the independent iteration-008-delivery.json receipt after this source record.

Pure tests execute real discovery/charge/capped-gain rules; presentation tests
execute real Runtime/Hud/props with explicit recording boundaries. Geometry
checks read actual wall/gate/player-footprint literals. None establishes native
collision, device input, rendered legibility, audio audition, balance or play.
Original hero/animation/save/gate resources and mature combat remain preserved.

Native acceptance is optional and unrun. When supported, walk to both reserves
before unlocking the seal; check LOS around walls, full-resource E preservation,
partial restoration, rune -> K burst, pause/disable/real retry and beacon/save E
priority. Inspect silhouettes/pulse/font overlap and actual resource balance.

Current source paths: Assets/Scripts/Gameplay/ClearingSupplies.cs,
ClearingRuntime.cs, ClearingSupplyVisuals.cs and ClearingHud.cs; pure suite
tests/exploration/ClearingSuppliesChecks.cs with tools/run_supply_checks.py;
actual-method suite tests/presentation/PresentationBehaviorChecks.cs and
serialized geometry suite tests/clearing_scene/test_scene_contracts.py.
