# Iteration 10 Boss rules and bounded difficulty review

Source baseline: `ba4cd1d01ca3e421af812149de6c05588ef4af8f`, reviewed on the retained `rpg/iteration-010-guardian-polish` checkout. This read-only review changed no tracked file, HP, damage, timing, geometry, resources, rewards, or save format.

## Evidence classes and fresh verification

- **Static actual-source review:** `ClearingBoss.cs`, `ClearingRules.cs`, `SentinelTactics.cs`, live `ClearingRuntime` admission/contact/lifecycle path, `ClearingBossVisuals` collider/ownership path, scene movement speed and `BOSS_ENCOUNTER.md` contract.
- **Offline actual production C# tests:** `python3 tools/run_boss_checks.py --project-root .` returned exit 0, **19 passed / 0 failed**, without Unity doubles. These validate state/admission/contact rules, not native physics or input.
- **Deterministic informational simulation:** the temporary session C# probe compiles the current checkout's three domain sources, uses 20 ms steps, then records results in `actual-baseline-audit.txt`. Fresh execution returned exit 0 and reproduced the table below. This is a controlled model, not a player test or balance acceptance.
- **Native Unity Editor, PlayMode, physics, real Input System, rendering, animation, audio, builds and real player sessions:** not executed. All difficulty, visual clarity and feel conclusions remain **尚未进行 Unity 原生验收**.

Commands executed in the session evidence folder (temporary probe scripts are not source deliverables):

```bash
python3 /tmp/rpg-iteration-010/run_actual_boss_audit.py --project-root /workspace/2D-RPG-Project --output /tmp/rpg-iteration-010/actual-baseline-audit.txt
python3 tools/run_boss_checks.py --project-root .
```

The probe uses the repository's compiler discovery; it requires no `mono` executable on PATH, never overwrites a production source, and builds only in a temporary directory.

## State and integration review

No additional blocking state/contact/lifecycle defect was found within the inspected path. This is a bounded review result, not proof that native gameplay is bug-free.

- Live runtime opts into required Boss; dormant entry rejects damage/awakening until all three guards die, then foot-range/clear-LOS E awakens without refilling HP/MP or restoring spent supplies. Old action tokens are invalidated without refunding paid deadlines.
- Each admitted pattern captures immutable geometry and windup/active/recovery timing. Crossing 54 HP changes future admissions only. Lance/Sigil alternate, use captured caster LOS, and consume at most one contact with shared guard/player immunity.
- Runtime resolves player contacts before retaliation. Boss lethal damage cancels the live warning/contact and opens the existing seal. Killing/awakening alone neither banks nor duplicates the unchanged 30-coin reward; beacon completion remains authoritative.
- Pause skips simulation advance. A large step beyond Active does not deal delayed damage. Disable cancels old contacts, retains HP/pattern/cooldowns and original ready deadline; retry resets state while rejecting stale tokens.
- Presentation defects reproduced before checkpoint ac3aa87 have actual evidence outside this rules suite: Recovery HUD still instructs avoidance; the half-health notice is lower priority than a supply hint and expires on unscaled wall time during pause. The initial three regression cases later pass at ac3aa87 (108/0); independent review then found the paused/dead Recovery variant. This report records the original read-only audit and does not imply final/native acceptance.

## Controlled baseline results

Assumptions: guards are removed as fixture setup; a .5 s wait allows the paid light cooldown to expire before entry; isolated stationary player foot `(0,-1)` and Boss `(0,0)`; clear LOS; every admitted player attack immediately makes a valid forward contact; earliest scripted repeated presses; 10 entry HP, 20 entry MP, actual six-MP burst cost and mirrored .8 MP/s regeneration. Both player-first and Boss-first initial admission orders give the same results. Guard attrition, aiming, approach, collider response, visual perception and human key timing are not modeled.

| Scripted policy | Simulated Boss death | Light / burst actions | Warnings admitted | Accepted Boss hits | HP left |
| --- | ---: | ---: | ---: | ---: | ---: |
| Earliest light | 2.30 s | 6 / 0 | 1 | 1 | 8 |
| Earliest burst | 5.02 s | 0 / 4 | 3 | 2 | 6 |
| Light-first mixed | 1.58 s | 4 / 2 | 1 | 1 | 8 |
| Lights only during Recovery | 5.18 s | 6 / 0 | 3 | 3 | 4 |

This supports one narrow claim: ideal scripted contacts can end the rule-level encounter before a second pattern is admitted. It does not establish that real players find the Boss too easy. The recovery-only model intentionally stays inside contacts rather than dodging; its lost HP does not show that a proper counter strategy is worse.

With cooldowns expired and contacts/approach assumed, a 1.15 s Phase 1 recovery admits three lights plus one burst (84 prospective damage); .9 s Phase 2 admits two lights plus one burst (66). These legal opportunities support a truthful Recovery/strike cue without changing rules.

## Movement and resource bounds

Serialized speed is 4 units/s. Center-to-nearest-edge distances are .45 for the lane and .9 for the sigil. At unobstructed full input, adding one 1/30-unit pixel of margin gives .121 s / .233 s theoretical escape travel. Subtracting these from the phase-2 .8 s warning leaves .679 s / .567 s theoretical reaction allowance. At 25% analog magnitude, the sigil distance plus margin needs .933 s, greater than .8 s. These are geometry/movement bounds, not measured human reaction, native collision response or readability; walls, partial input and perception may reduce usable time.

Awakening retains entry attrition. Base 10 HP tolerates four accepted two-HP contacts and dies on the fifth; entry 2 HP dies on the first and entry 4 HP on the second. In the stationary zero-MP light model, entry 2 HP dies at 1.02 s; entry 4 HP clears with 2 HP. These endpoint samples do not measure actual guard-to-Boss difficulty. Unused herbs restore up to 4 HP (at most two further two-HP mistakes); the rune restores up to 6 MP (one existing burst). Detours and reserve claims under attack are not simulated.

## Decision and native acceptance protocol

**Keep 108 HP, two-HP damage and existing timing.** Informational what-if armor variants preserved under the earlier `/tmp/iteration10-balance-audit` folder did not reliably expose both patterns with mixed actions and could penalize resource use. Those variants were never implemented and are not evidence for shipping balance tuning. Current scope should finish the evidenced state/notice fixes, cached non-color counter marker, pause-preserved phase feedback and once-only cue.

When a usable licensed Editor becomes available, optionally record fresh and upgraded runs with keyboard, full analog and partial analog input. Include familiar and first-time players. Record entry HP/MP/reserve charges, admitted patterns, clear time, accepted hits and causes, recovery attacks, phase-threshold notice visibility, supply detours and bank/retry result. Check:

1. Current telegraph remains unchanged when a light/burst crosses half HP; next attack uses the phase-2 timings.
2. Recovery wording/marker appears only in Recovery; actual damage cannot occur after Active; lane/sigil outline and hit footprint align in the Game View.
3. Phase notice remains visible beside a nearby supply hint, freezes during a long pause, then expires by active simulation time; the phase cue occurs once and respects mute.
4. Death/kill/completion/disable/retry remove stale notices, marks and numbers while retaining/resetting the correct costs, supplies and saved rewards.
5. At narrow/wide resolutions and wall proximity, HUD, large Boss silhouette, phase pulse, damage digits and reserve cues remain readable; audition sound levels/mute in context.

Use those observations before choosing HP, damage or speed changes. No native connection/license retry is necessary while the environment is unchanged.

## Production domain fingerprints at fresh review

```text
c5503dee1195fd448549192ce026086f5979cd5b7e6cc322847e01c5bc2fe477  ClearingRules.cs
ab97ea87c394eae05181d4d1f7ae947d62610cbf599af7836028c201b9c39f3c  ClearingBoss.cs
7048a609e4781cc27661b3e64e78be038d984e3603d2945fde227db33cdb9189  SentinelTactics.cs
```
