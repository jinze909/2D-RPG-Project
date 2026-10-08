Run `python3 tools/run_player_checks.py`. Install Mono's compiler/runtime
(`mono-devel` on Ubuntu), or set `RPG_MONO` and `RPG_CSC`/`RPG_MCS` to executable
paths. Unity's bundled Mono/Roslyn is discovered when available; set
`UNITY_EDITOR_PATH` for another installation.

The runner compiles every production `.cs` under `Assets`, including the real
generated input wrapper and the editor inspector. Behavior checks call the
actual `PlayerMovement` lifecycle methods. External Unity APIs use narrow
boundary doubles, so these checks prove input sampling, movement request
ownership, death/disable handling, cleanup, and facing parameter behavior.
They do not prove Unity package import, native physics/collisions, Animator
transitions, frame rendering, or gamepad hardware behavior. Native Unity tests
and playtesting remain required for those claims.

`--project-root PATH` loads candidate production scripts. The runner and
fixtures always come from their own directory, allowing automation to copy
reviewed baseline checks outside a candidate checkout before an AI edit.
New scripts are included in compilation; that alone does not establish
behavior coverage for a new feature.

The partial action value check preserves a value already returned by an
input action. The current gamepad binding uses the Input System's default
digital `2DVector` composite, so it does not yet expose analog stick strength.
Changing that binding and validating real input devices is a separate task.
See the official version 1.11.2
[Vector2Composite source](https://raw.githubusercontent.com/Unity-Technologies/InputSystem/1.11.2/Packages/com.unity.inputsystem/InputSystem/Actions/Composites/Vector2Composite.cs).
