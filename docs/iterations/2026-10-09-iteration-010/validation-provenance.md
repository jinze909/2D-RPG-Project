# Iteration 10 validation provenance

The archived reports are exact copies of real executed output. project-validation
records base_sha ac3aa87bfa7910314e498e0512a77df2141ae928: tests executed the working
tree including the pause/dead HUD correction and expanded123fixture, later saved
in code checkpoint36567e8. That base identifies history, not an assertion that
uncommitted test edits were already in ac3aa87. No further production edit is
planned. Exact immutable CI after publication binds delivered source identities.

Actual commands:

```bash
python3 tools/validate_project.py --root . --output /tmp/rpg-iteration-010/final-project.json
python3 tools/compile_unity_api.py --project-root . --output /tmp/rpg-iteration-010/final-api.json
python3 -m unittest discover -s tests -p 'test_distribution.py' -v
git lfs fsck
git diff --check
```

All exited0 as inspected by root. Sum only passed_tests:379; add six distribution
checks =385. Five additional static envelope checks also pass; they are not counted
as five extra unit tests. Project report's native_unity_run and compiler native
import/tests/gameplay fields are false. Input System is a reviewed boundary double.
Distribution log's “not a git repository” comes from its temporary package fixture;
that test and allsix groups reportok/OK. The resulting fixtureZIP is not a source
delivery ZIP and its bytes/hash are not final game-archive evidence.

Independent reviewer actually reran123presentation/19pureBoss/API26/86 and reviewed
the final permanentfixture. No remaining production blocker found within scope.
None of this is native Editor/play/physics/Animator/render/audio/build or a real
player playtest. Bounded balance simulation has separately stated assumptions.
