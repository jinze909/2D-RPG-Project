# Historical regression boundary — iteration 7

Fresh restored-source validator passed 290 groups, including input/animation9,
scene14, movement14, presentation69 and raster/hero10. Hero images, input assets,
Animator clips/controller, scenes, package locks and settings have no diff from
sixth-round master b8d55f0. New numbers are independent world objects and do not
replace the hero animation state. Existing tests cover walking input continuity
during combat, facing/strike geometry, real animation sprite references and
immediate death/revive routes. Actual native gait/foot drift/deformation/state
blending remains unrun; offline evidence cannot prove those visual complaints gone.

The existing HUD edge/reference-layout contract passes; no new Canvas panel was
added. Numbers occupy small world labels with fixed slots. Actual rendered HUD,
near-actor overlap and number readability remain unrun.

CombatClearing's seal/outer-boundary/unlocked-footprint route checks and completion
admission/reward tests pass. These verify the actual current clearing geometry and
fight-three -> E beacon -> saved reward loop. The historical thorn-forest scene,
archery cycle and full quest/map-unlock system do not exist in this checkout;
no equivalent scene or regression result is invented for them.

Native Editor tests/play/build and audible cues remain unrun and optional under
the current user instruction. Saved growth, tactics and old 59 presentation groups
were retained; this iteration adds actual-loss feedback rather than rewriting them.
