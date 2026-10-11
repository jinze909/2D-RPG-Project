# Independent Thornwood domain and integration review

Reviewed the recovered eleventh working tree based on checkpoint
`092d3105b0af018df362a9ef807deebf7b24fd06`. This report precedes the implementation
commit; final exact-source CI and archive identities belong to the publication
record. The reviewer made no production changes or commits.

## Fresh code evidence

- `python3 tools/run_thornwood_checks.py --project-root .`: **31 passed, zero
  failed**, compiling actual domain/layout/combat/progression C# without Unity
  doubles. The conservative swept-foot layout connects **2,934** sampled walkable
  nodes, all seeds, cache, entry/exit and enemy spawns. It rejects outside bounds,
  trunk overlap and trunk tunneling. Return is explicitly checked at the saved
  clearing beacon `(0, 3.35)`, because completed clearing movement is locked.
- `python3 tools/run_thornwood_integration_checks.py --project-root .`: **35
  passed, zero failed** after the entry-frame ownership correction. This compiles
  **30 actual project C# files** with recording Unity boundaries and executes the
  production clearing/forest input, contact, reward and lifecycle methods.
- Reviewed actor-owned HP/MP and nonstacking saved bonuses, first entry versus
  retained reentry, failed save retry, completed exit movement, death/R retry,
  immutable pounce contacts, shared immunity, player-first lethal contacts,
  suspend cleanup and conservative continuous boundary protection. No additional
  blocking defect was found in this bounded review.

## Reward acceptance mutation replay

The trusted integration runner compiled isolated temporary copies of all actual
`Assets/**/*.cs`; the development checkout was never mutated. At this replay the
fixture contained 34 groups; the subsequent entry-frame regression increased it
to 35. Results are kept distinct rather than inflating the earlier counts.

| Copied-source variant | Result | Relevant evidence |
| --- | --- | --- |
| Accepted-receipt implementation | 34 pass / 0 fail, exit 0 | Ordinary saved clearing A and forest B stay at 60 coins through return/reentry and purchase. |
| Clearing getter changed to `progress != null && progress.IsCompletionBanked(rewardId)` | 31 pass / 3 fail, exit 1 | The alternating A/B test fails with `returning to older A identity replayed thirty coins`; retained reentry and a later legitimate forest retry also fail. |
| Same old clearing getter plus forest guard changed from `rewardBanked` to latest-ID lookup | 31 pass / 3 fail, exit 1 | Reproduces the same failures. This combined mutation does not independently establish a separate forest-guard failure. |

The v1 profile stores only the latest reward ID. Region-local accepted transaction
latches retain each still-living attempt's receipt after another region advances
that profile. This changes neither the v1 schema nor its 30-coin arithmetic; true
retries create fresh reward identities. The fixture verifies actual runtime calls,
not a reimplementation of those calls.

## Entry-frame ownership red/green

The freshly added regression originally produced **34 pass / 1 fail**: after F
entered the forest, `ClearingRuntime.Update` continued into clearing presentation
and repainted the shared hero using retained clearing immunity. The failure text
was `clearing RefreshViews overwrote the newly entered forest actor with an old
clearing hurt tint`. An immediate return once the forest becomes active fixes that
ownership boundary. The same full fixture is now **35 pass / 0 fail**.

The source SHA-256 identities for that final independent integration run are:

| File | SHA-256 |
| --- | --- |
| `Assets/Scripts/Gameplay/ClearingRuntime.cs` | `654454f612dc2a9a1cb4b8cf293774bae5a60347e0351adef6bd8a6d7e539f37` |
| `Assets/Scripts/Gameplay/ThornwoodRuntime.cs` | `75944e9410b64cb1f55be6e09552e0964948eb62371d83d5d894b026afc0c1f1` |
| `tests/thornwood/ThornwoodIntegrationChecks.cs` | `80a1608d3093b9eaa378288452a170ce164541c042504fe479ffb37667396c9c` |

## Evidence limits and skills

Domain execution is actual offline logic; integration execution uses explicitly
recording engine/input/physics/render/audio boundaries. Grid connectivity is
conservative deterministic geometry. These establish none of native collision,
Animator playback, visual readability, sound output, player difficulty or feel.
Those experiences are **尚未进行 Unity 原生验收**. No native Editor, licensing,
MCP or player-build success is claimed, and no unchanged connection probe ran.

Applied `verification-before-completion` to fresh exit codes and complete output,
`systematic-debugging` to isolated source-mutation evidence and the entry-frame
failure, and `game-design` to the complete entry/exploration/cache/return/growth
contract. Read the current AGENTS, development/issue/next/skill records and design
baseline; preserved existing Guardian 108 HP, damage/timing, blonde hero and v1
save compatibility. The only domain-fixture edits assert the correct beacon
return and correct inaccurate “serialized collider” wording to “runtime collider”.
