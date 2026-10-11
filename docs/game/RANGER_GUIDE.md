# Mira's optional Thornwood survey

Mira is the first NPC in the existing CombatClearing region. Her short survey
connects the saved clearing beacon, the implemented Thornwood expedition and the
existing health/mana training. She does not block access to Thornwood or add a
second currency/reward path. The original blonde hero, Guardian108HP/timing and
v1 progression format remain unchanged.

## Player loop

1. Complete the clearing and successfully save its beacon reward. At the beacon,
   T talks to Mira. E accepts her optional survey; T or Esc closes any page.
2. F at the beacon enters Thornwood by its existing admission rules. Collect
   three seeds, defeat three Briar Stalkers, then open the north cache with E.
   The existing cache transaction saves30coins. Mira's dialogue observes that
   actual expedition; it never fills objectives or writes rewards.
3. E at the south trail returns whenever alive and unpaused. You can return early
   to ask Mira about current counts. Reentry retains the same existing attempt;
   neither dialogue nor travel refills resources.
4. T reopens Mira's field report. E reports a complete, successfully banked
   expedition once. A completed cache whose save failed is not reportable; the
   dialogue explains reentry and E save retry. No extra coins are awarded for
   acceptance or reporting.
5. The training page uses the existing1/2 health/mana upgrade purchases with their
   normal rank caps, prices and save-before-mutate writer. Only banked coins can
   be spent; bonuses apply to the next genuine new run, not to travel or dialogue.
   E/T/Esc closes it. Training is available even before finishing the survey.

The dialogue has three pages: introduction/acceptance, live field report and
training. Opening never accepts or reports automatically; each E edge performs
one step. Holding a key must not auto-advance. While a page is open, the runtime
owns input exclusively: E/F/R/attacks do not also operate the world, and T/Esc
opening or closing must not fall through to another action or pause that frame.

## Authority and lifecycle

`RangerExpeditionSnapshot` is an immutable read view of current expedition ID,
seed count, defeated stalker count, cache completion and the runtime's accepted
reward latch. An absent identity cannot manufacture a completed survey. Snapshot
refresh on E is authoritative; a previously displayed completed cache does not
permit reporting after the current save authority says unbanked.

`RangerDialogue` contains no Unity APIs or progression store. `Open` chooses the
introduction until accepted, otherwise the field report. `Advance` accepts,
reports/advances to training, or closes; it has no monetary return value. Reporting
requires acceptance, a nonempty current attempt ID, three seeds, three defeated
stalkers, completed cache and accepted bank receipt. Close/reopen and early
return/reentry preserve acceptance and a current-attempt report. A genuine forest
retry changes expedition ID and clears the old report while retaining this
clearing's accepted survey. A genuine clearing retry resets all dialogue state.

This is session-local NPC interaction, not a persistent quest journal, broad
quest framework, equipment system or mid-expedition world save. Quitting/loading
still restores only the existing profile, not the unfinished forest or survey.

## Evidence boundaries

The engine-independent fixture executes the actual production dialogue source
and covers meaningful acceptance/page/report/save-failure/new-attempt/close/reset
paths. Runtime integration additionally must verify actual input ownership,
proximity/alive/banked admission, pause/disable cleanup, existing purchases and no
currency changes from dialogue. Neither fixture demonstrates native keyboard,
Animator, collision, Canvas font fitting, audio output or player readability.

Run the pure fixture with:

```bash
python3 tools/run_ranger_checks.py --project-root .
```

Native input/presentation and player experience are **尚未进行 Unity 原生验收**
unless a later record supplies real Unity Editor/player evidence. API-reference
compilation, recorded boundaries and code raster inspection remain separate.
