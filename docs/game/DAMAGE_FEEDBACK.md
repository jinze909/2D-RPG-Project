# Accepted-damage feedback — iteration 7

Player value: show what light18, burst30 and enemy2 contacts actually removed,
including clamped lethal6/24. Retain existing hit/kill/hurt flashes and cues, inputs,
MP cost/cooldowns, tactical enemies, gate/rewards and savedgrowth. This is a bounded
feedback slice; no globaltimescale hitstop, camera overhaul or new ability needed.

One cached independent worldlabel for each of3enemies plus1player; each7glyph
renderers. Cache14original3x5 glyphs (digits/minus/decimal/less-than/plus) with
1pixelInkoutline atPPU30, Point/Clamp/noMip/FullRect, establishedpalette. No HUD
panel/font/package/hero replacement. Each actualaccepted contact reports HPbefore
minus HPafter, not nominalattackdamage. Rejected/repeated/blocked/expired contacts
never emit. LightCream, burstCyan, hurtDanger; minus sign preserves meaning.

Capture impactposition; rise .35worldunits and fade over .6simulationseconds.
Newhit replaces sameactor's fixedslot; multi-target action uses separate slots.
Killlabel survives enemybody hiding. Pause freezes run.Time; defeat/completion/
disable/retry clear text/amount/deadline/roots, preventing frozen or resumedlabels.
Formatting happens only on newhit; no GameObject/Texture/component creation after
startup and no newmanaged allocation in ordinaryRefresh. Show may allocate its
bounded formattedstring. Disposable caches follow Runtime owner lifecycle.

Currentinteger losses display exactly. Fractional futurelosses round to2decimals,
less-than.01 displays-<0.01; greater999 displays-999+. Allstrings fit7glyphs;
invalidzero/negative/nonfinite values produce nofeedback. This is presentation,
not a new HP authority or mutable combat/save model.

Verification: pureformat/glyph/lifetime contracts plus actualRuntime/renderer
recording-boundary checks for deltas/cappedkills/rejections/multi-target/lock/pause/
cleanup/cache/replacement; existing combat/resource/progression/scene/input/hero
checks remain. Native visualreadability, actualphysics/input/audio/play/build are
unrun, optional for merging. Readable guides do not establish liveEditor/MCP.
