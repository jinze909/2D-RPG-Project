# Next iteration

For a new Codex window, first read
[new-window handoff](docs/iterations/2026-10-08-iteration-003/new-window-handoff.md).
It records exact delivered source/ZIP identities, the user's license-login choice,
and the now-readable Unity plugin skills. Recheck the actual CLI, license and MCP
connection; skill visibility alone does not establish native Editor readiness.

1. Fetch latest master and all unmerged `rpg/iteration-*` work. Read iterations 2/3,
   PR #1 and its stacked polish PR, exact hosted CI and verified ZIP receipts.
   PR #1 merge remains the user's decision. Do not recreate combat, HUD, raster
   art or the beacon loop merely because master predates those candidates.
2. Perform `docs/game/COMBAT_CLEARING.md` native acceptance with licensed
   international Unity 2022.3.53f1: import/compile, EditMode/PlayMode, collisions,
   gate, input/attack order, death/retry/pause, HUD screenshots and all six cues.
   Check FullRect sprite creation, pixel scale, sentinel feet/health bar layering,
   warning-fill/X readability, hit/kill durations, multi-target sound deduplication,
   stale sounds on retry and edge-slot messages in narrow/portrait windows.
   Keep high-risk work isolated until native evidence justifies merging.
3. Resolve real visual defects: walking root/foot alignment, gait at partial
   speed, coherent hero hurt/death/attack beats and native enemy-art readability.
   The new stone/moss raster art is already implemented; improve only confirmed
   defects rather than replacing it just to increase modification count. Preserve
   blonde identity, PPU30 and accepted frames; rejected ranger arrows stay rejected.
4. After accepting this loop, add one coherent extension: exploration/interaction
   with checkpoint or meaningful persistent rewards. Define save/version/reset
   contracts before inventory/equipment/classes. Balance using actual play.
5. Verify candidate CI/ZIP provenance and maintain eight skills and all five root
   records. Every new task rediscovers tools, reference code and unmerged results.

Earlier movement/numeric guards remain regression covered.
Pursue new confirmed problems and complete playable loops, preserving user history
and newer accepted work. This list does not override more serious new issues.
