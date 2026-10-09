# Clearing progression contract — version 1

The fifth-round candidate turns the clearing's run-local reward into a small
repeatable growth loop: defeat three sentinels, restore the beacon with E, bank
30 coins, buy a permanent upgrade on the result screen, then press R to begin
the next run with it. Killing enemies alone, dying or abandoning the run never
banks coins. This is a progression profile, not a full world/position save.

## Player choices

| Upgrade | Effect per rank | Maximum | Prices for ranks 1 / 2 / 3 |
| --- | --- | --- | --- |
| Vitality — 1 or keypad 1 | +2 maximum HP next run | 3 ranks (+6 HP) | 30 / 45 / 60 coins |
| Focus — 2 or keypad 2 | +2 maximum MP next run | 3 ranks (+6 MP) | 30 / 45 / 60 coins |

The original authoring maxima are 10 HP / 20 MP. Bonus maxima are always computed
from captured base values and saved ranks, then new-run resources fill those
maxima. Buying never heals or enlarges the current completed actor, never rewrites
PlayerStats.asset and never stacks the same bonus on repeated retries. The first
clear permits one rank immediately; later ranks ask the player to save or choose
another attribute. These are candidate balance values requiring actual play.

Purchases are admitted only on an unpaused completed run whose reward has been
banked. Rejected purchases spend no coins and show the actual reason. A successful
deposit/purchase updates the bank/rank labels and uses the existing reward cue;
repeated completion input does not duplicate money or audio. Combat controls,
attack costs/timings, warning footprints, sentinels, gate geometry and hero
locomotion remain the existing contract in COMBAT_CLEARING.md.

## Persistence, ownership and reset

ClearingProgress owns an immutable version-1 ledger: revision, bank coins,
completed-run count, Vitality/Focus ranks and latest deposited reward ID.
ClearingRun still owns encounter state and run-local coins; Player owns the
runtime stats clone. ClearingRuntime owns one fresh Guid N reward ID per attempt,
retains it across disable/resume, and changes it only on new-run reset. It never
submits archived completed encounters. The latest ID prevents repeated current
completion deposits across purchases/reloads; it is not a global historical
replay ledger or server anti-cheat system.

R resets encounter enemies, HP/MP, cooldowns, objective, effects and the attempt
ID. It retains the saved bank/ranks. Restarting the application starts a new
clearing with saved bonuses; enemies, positions and unfinished encounters are
not restored. No automatic profile wipe or destructive reset shortcut exists.

Desktop storage uses Application.persistentDataPath/ClearingProgress and stable
clearing-progress.dat / .bak / .tmp / .lock names. Company/product identity and
this directory/file contract must remain stable to retain existing profiles.
The compact UTF-8 text uses a magic header, explicit version, ordered numeric
fields, a hexadecimal reward ID and SHA-256 damage checksum. It contains no Unity
objects or asset references. No serializer/package/native JSON service is needed.
Canonical LF encoding and invariant numbers are required; arbitrary hand-edited
whitespace or invalid currency arithmetic is rejected. A checksum is not encryption.

Version 1 requires rank 0..3 and at most 1,000,000 completed runs. Revision equals
completed runs plus purchased ranks. Bank plus spent upgrade costs equals
30 times completed runs. Prices and that ledger are versioned data contracts;
future price/schema changes need explicit migration, not a silent v1 rewrite.
There was no older profile format to migrate in the baseline.

## Save failure and recovery

Transactions change live bank/ranks only after a successful write. The store
locks cooperating instances, compares the expected committed snapshot, writes
and flushes bounded staging bytes, then uses same-directory File.Move for a new
save or File.Replace for an existing one. It does not delete the destination as
a fallback. Storage is touched at initialization and explicit completed-run
transactions; no per-frame or quit-only saving occurs.

Missing save/backup starts a writable fresh profile. A damaged primary with a
verified backup loads earlier progress with a recovery notice; recovery can
roll back the last transaction. A subsequent save preserves that good backup
and keeps the damaged primary in a separate rejected file. An orphan staging
file never replaces committed progress. Future-version primary or backup data
blocks writes instead of downgrading it. Two damaged files, invalid encoding,
oversized data, lock/path/permission failures or unsupported storage are handled
without an automatic profile reset or overwrite.

After a failed completion deposit, E retries saving from the result screen.
R can continue, with an explicit notice that this unbanked reward is lost.
Purchases stay unavailable until that completion is banked. After a failed
purchase, stored coins/ranks remain intact and the same purchase can be retried.
Other-session conflicts may require closing the other session and reopening.
Empty/unsupported persistent paths permit the combat loop without save writes.
No browser/mobile/console persistence support is claimed by desktop Mono checks.

## Acceptance evidence

Pure C# tests exercise the actual model, strict codec and real temporary-file
store: completion/death/admission, repeated IDs, prices/caps, failed transactions,
reload, corrupt/future data, old backup preservation, real staging/preflight path
and lock faults, and stale-writer rejection. Preflight backup collisions do not
claim to simulate a failure inside the File.Replace system call.

Recording-boundary tests must additionally execute actual Runtime/Hud methods:
near/locked beacon, key selection, save retries, terminal/control gates,
next-run bonus application without stacking or template mutation, and honest HUD
cost/save feedback. These are not Unity physics/render/PlayMode tests.

Licensed Unity 2022.3.53f1 acceptance still needs real input, lifecycle, visuals,
multi-resolution result layout, persistence across actual player restarts, audio
audition and platform build. PR #1 stays draft and master unchanged until that
acceptance is complete. Save files, licenses and caches are excluded from source
ZIP delivery.

## Researched APIs

- [Unity 2022.3 persistentDataPath](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-persistentDataPath.html)
- [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=netstandard-2.1)
- [FileStream.Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=netstandard-2.1)

The real Unity API-reference compiler and Mono filesystem tests verify the APIs
available here; documentation alone does not prove target-player support.
