"""Source/asset contracts; no native Input System, Animator or hardware is run."""
from __future__ import annotations

import json
from pathlib import Path
import re
import unittest


PROJECT_ROOT = Path(__file__).resolve().parents[2]


def embedded_actions(source: str) -> dict:
    match = re.search(r'InputActionAsset\.FromJson\(@"(.*?)"\);', source, re.DOTALL)
    if match is None:
        raise ValueError("Generated input wrapper has no serialized action asset")
    return json.loads(match.group(1).replace('""', '"'))


def sprite_keys(source: str) -> list[tuple[float, int, str]]:
    return [(float(time), int(local_id), guid) for time, local_id, guid in re.findall(
        r'- time: ([\d.]+)\n\s+value: \{fileID: (-?\d+), guid: (\w+), type: 3\}', source)]


class InputAnimationContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.actions = json.loads((PROJECT_ROOT / "Assets/Actions/PlayerActions.inputactions").read_text())
        cls.movement = next(item for item in cls.actions["maps"] if item["name"] == "Movement")
        cls.bindings = cls.movement["bindings"]
        cls.dead = (PROJECT_ROOT / "Assets/Animations/Player/Dead.anim").read_text()

    def keyboard_composite(self, expected_paths: dict[str, str]):
        for index, binding in enumerate(self.bindings):
            if not binding["isComposite"] or binding["action"] != "Move":
                continue
            children = []
            for child in self.bindings[index + 1:]:
                if not child["isPartOfComposite"]:
                    break
                children.append(child)
            if {child["name"]: child["path"] for child in children} == expected_paths:
                self.assertIn(binding["path"].lower(), ("2dvector", "2dvector(mode=0)"))
                return
        self.fail("Missing a complete normalized keyboard composite: " + repr(expected_paths))

    def test_generated_wrapper_matches_input_asset(self):
        wrapper = (PROJECT_ROOT / "Assets/Actions/PlayerActions.cs").read_text()
        self.assertEqual(self.actions, embedded_actions(wrapper),
                         "Runtime wrapper and editable input asset disagree")

    def test_wasd_preserved_as_a_single_composite(self):
        self.keyboard_composite(dict(up="<Keyboard>/w", down="<Keyboard>/s",
                                     left="<Keyboard>/a", right="<Keyboard>/d"))

    def test_all_arrow_keys_are_a_normalized_composite(self):
        self.keyboard_composite(dict(up="<Keyboard>/upArrow", down="<Keyboard>/downArrow",
                                     left="<Keyboard>/leftArrow", right="<Keyboard>/rightArrow"))

    def test_stick_is_bound_as_an_analog_vector(self):
        stick = [item for item in self.bindings if item["path"] == "<Gamepad>/leftStick"]
        self.assertEqual(1, len(stick), "Left stick must reach Move as a Vector2")
        self.assertFalse(stick[0]["isComposite"])
        self.assertFalse(stick[0]["isPartOfComposite"])
        self.assertEqual("Move", stick[0]["action"])
        self.assertEqual("", stick[0]["processors"],
                         "Use the control's built-in deadzone; avoid double deadzone/normalization")
        self.assertFalse(any(item["path"].startswith("<Gamepad>/leftStick/")
                             for item in self.bindings), "Stick direction buttons discard analog strength")

    def test_input_identifiers_are_unique_and_move_is_vector(self):
        ids = [self.movement["id"]]
        ids.extend(item["id"] for item in self.movement["actions"])
        ids.extend(item["id"] for item in self.bindings)
        self.assertEqual(len(ids), len(set(ids)))
        move = next(item for item in self.movement["actions"] if item["name"] == "Move")
        self.assertEqual("Value", move["type"])
        self.assertEqual("Vector2", move["expectedControlType"])
        self.assertTrue(move["initialStateCheck"])
        self.assertEqual("", move["processors"])

    def test_defeat_does_not_loop(self):
        self.assertRegex(self.dead, r"m_LoopTime: 0\b", "Death must not replay indefinitely")

    def test_defeat_holds_one_existing_pose(self):
        keys = sprite_keys(self.dead)
        self.assertTrue(keys)
        self.assertEqual(1, len({(local_id, guid) for _, local_id, guid in keys}),
                         "Existing Dead clip cycles through four standing directions")
        idle = sprite_keys((PROJECT_ROOT / "Assets/Animations/Player/Idle_Down.anim").read_text())
        self.assertEqual(idle[0][1:], keys[0][1:],
                         "Use the established front idle sprite until proper defeat art exists")

    def test_all_animation_keys_resolve_to_real_sprite_subassets(self):
        tables = {}
        for meta in (PROJECT_ROOT / "Assets").rglob("*.png.meta"):
            source = meta.read_text()
            guid = re.search(r"^guid: (\w+)$", source, re.MULTILINE).group(1)
            tables[guid] = {int(item) for item in re.findall(r"^      internalID: (-?\d+)$", source, re.MULTILINE)}
        clips = list((PROJECT_ROOT / "Assets/Animations/Player").glob("*.anim"))
        self.assertGreaterEqual(len(clips), 9)
        for clip in clips:
            keys = sprite_keys(clip.read_text())
            self.assertTrue(keys, str(clip))
            for time, local_id, guid in keys:
                self.assertIn(guid, tables, str(clip))
                self.assertIn(local_id, tables[guid], f"{clip.name}: sprite {local_id} missing at {time}s")
            self.assertEqual(sorted(set(time for time, _, _ in keys)), [time for time, _, _ in keys])

    def test_controller_retains_immediate_death_and_revive_routes(self):
        source = (PROJECT_ROOT / "Assets/Animations/Player/PlayerViola.controller").read_text()
        blocks = re.split(r"^--- !u!", source, flags=re.MULTILINE)[1:]
        states = {}
        for block in blocks:
            if not block.startswith("1102 "):
                continue
            state_id = re.search(r"^1102 &(-?\d+)", block).group(1)
            states[re.search(r"  m_Name: (.+)", block).group(1)] = state_id
        self.assertIn("Dead", states)
        self.assertIn("Idle Tree", states)
        for trigger, target in (("Dead", "Dead"), ("Revive", "Idle Tree")):
            transitions = [block for block in blocks if block.startswith("1101 ")
                           and f"m_ConditionEvent: {trigger}\n" in block]
            self.assertEqual(1, len(transitions), trigger)
            self.assertIn(f"m_DstState: {{fileID: {states[target]}}}", transitions[0])
            self.assertIn("m_HasExitTime: 0", transitions[0], "Transition must not wait for loop completion")


if __name__ == "__main__":
    unittest.main()
