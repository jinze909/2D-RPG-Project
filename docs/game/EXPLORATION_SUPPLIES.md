# Optional exploration supplies — eighth-round implementation contract

Player value: leave the central battle route, discover optional reserves, restore
resources with E and return to existing tactical combat. Mature progression,
enemy roles, accepted damage feedback, costs/cooldowns and hero remain unchanged.

Two nonblocking ground points use feet coordinates, never hero body center:
herbs(-5.6,-2.35) replenish at most4HP; rune(5.6,1.15) at most6MP. Both are
reachable inside the closed seal on the current .18/.14 half-footprint geometry.
These are offline design checks, not native physics. No saved coins/ranks or
completion requirement changes; this is not inventory or permanent world saving.

Discover within2.4 and clear wall LOS; remember discovery for the current attempt.
Use E within0.8 plus LOS, alive/unpaused/incomplete, discovered and unclaimed.
Full/invalid/no-effective-gain values retain the reserve and play no success cue.
Accepted amount is actual clamped after-minus-before, never nominal recharge.
Each point has one independent charge per run. Disable/resume preserves state;
only actual retry/new-run Reset restocks. Pause freezes world presentation.

E priority: near beacon uses existing objective logic; otherwise nearby supply;
otherwise existing objective guidance. Completed-run E save retry remains intact.
Actual gain uses actor-owned PlayerStats, with no duplicate HP/MP authority.
Success reuses existing Reward cue; discovery/rejection is silent. Near beacon
then nearby usable/full/spent supply then transient message choose the existing
edge help slot. No new persistent combat HUD panel.

Planned cached PPU30/Point/shared-palette original herbs/cross and rune/diamond
props distinguish kind by silhouette. Unseen subdued, discovered available bright,
claimed empty; no blocking collider. Optional short accepted-claim pulse follows
run.Time and clears interruption snapshots without resetting charge.

Current checkpoint contains only pure admission/discovery/claim/reset rules and
actual C# tests. Runtime, prop animation/art, input, resource bridge and HUD are
explicitly unfinished; no playable exploration or native rendering is claimed.
