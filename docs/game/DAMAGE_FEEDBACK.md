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

## Runtime integration and compatible APIs

ClearingRuntime captures HP before each accepted contact and supplies the delta
only after damage resolves. Renderer roots are owned by Runtime, independently
of enemy body visibility. Sorting order 30004 in the existing Player layer sits
above actor depth and impact effects. Clear runs before run-clock reset and on
all terminal/interruption paths; Dispose requests root/sprite/texture destruction.
A fatal player's number clears immediately with defeat feedback. Existing J/Space
and K input, K's six MP cost and established cooldowns remain authoritative.

Checked official Unity 2022.3 references:
- https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Sprite.Create.html
  supplies the FullRect overload with texture, rect, pivot and PPU.
- https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Texture2D.Apply.html
  confirms Apply(false, true) uploads without mip rebuilding and releases the CPU
  copy after cached glyph construction. No per-contact texture upload occurs.

Native display fitting, close-actor overlap and fractional-pixel animation remain
unrun. The small original glyphs and Point filtering do not prove native pixel
perfectness. API-reference compilation and recording-boundary behavior are
explicitly separate from Unity import, actual physics, rendering or audition.
