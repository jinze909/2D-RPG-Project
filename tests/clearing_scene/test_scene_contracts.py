"""Serialized entry points and geometric level contracts; no Unity simulation.

The reachability check reads the actual literal wall/gate rectangles used by
ClearingVisuals, then flood-fills an independent grid. It establishes blockout
connectivity, not native contacts, collision tunnelling, navigation or rendering.
"""
from __future__ import annotations

from collections import deque
import math
from pathlib import Path
import re
import unittest


PROJECT_ROOT = Path(__file__).resolve().parents[2]
NUMBER = r"(-?(?:\d+(?:\.\d*)?|\.\d+))[fF]?"
VECTOR = r"new Vector2\(\s*" + NUMBER + r"\s*,\s*" + NUMBER + r"\s*\)"
SCENE = "Assets/Scenes/CombatClearing.unity"


def guid(path: Path) -> str:
    match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.MULTILINE)
    if not match:
        raise ValueError("Missing GUID: " + str(path))
    return match[1]


def scene_objects(source: str) -> dict[int, tuple[int, str]]:
    markers = list(re.finditer(r"^--- !u!(\d+) &(-?\d+)\n", source, re.MULTILINE))
    result = {}
    for index, marker in enumerate(markers):
        local_id = int(marker[2])
        if local_id in result:
            raise ValueError("Duplicate scene fileID: " + str(local_id))
        end = markers[index + 1].start() if index + 1 < len(markers) else len(source)
        result[local_id] = (int(marker[1]), source[marker.end():end])
    return result


def vector_constant(source: str, name: str) -> tuple[float, float]:
    match = re.search(r"\b" + re.escape(name) + r"\s*=\s*" + VECTOR, source)
    if not match:
        raise ValueError("Cannot read literal Vector2: " + name)
    return float(match[1]), float(match[2])


def method_body(source: str, name: str) -> str:
    match = re.search(r"\b" + re.escape(name) + r"\([^)]*\)\s*\{", source)
    if not match:
        raise ValueError("Method unavailable: " + name)
    start = match.end()
    depth = 1
    for index in range(start, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[start:index]
    raise ValueError("Unterminated method: " + name)


def sprite_vertical_bounds(meta: Path, local_id: int) -> tuple[float, float, float]:
    """Measure real alpha pixels using the importer rect, pivot and unit scale."""
    from PIL import Image
    source = meta.read_text()
    ppu = float(re.search(r"spritePixelsToUnits: ([\d.]+)", source)[1])
    for block in re.split(r"^    - serializedVersion: 2\n", source, flags=re.MULTILINE)[1:]:
        identity = re.search(r"^      internalID: (-?\d+)$", block, re.MULTILINE)
        if not identity or int(identity[1]) != local_id:
            continue
        rect = re.search(r"      rect:\n\s+serializedVersion: 2\n\s+x: ([\d.]+)\n\s+y: ([\d.]+)"
                         r"\n\s+width: ([\d.]+)\n\s+height: ([\d.]+)", block)
        x, y, width, height = map(int, map(float, rect.groups()))
        alignment = int(re.search(r"^      alignment: (\d+)$", block, re.MULTILINE)[1])
        if alignment == 0:  # Unity SpriteAlignment.Center ignores the stored custom pivot.
            pivot_y = .5
        elif alignment == 9:
            pivot_y = float(re.search(r"pivot: \{x: [\d.-]+, y: ([\d.-]+)\}", block)[1])
        else:
            raise ValueError("Sprite alignment reader needs the actual non-center alignment: " + str(alignment))
        with Image.open(Path(str(meta)[:-5])) as image:
            frame = image.crop((x, image.height - y - height, x + width, image.height - y))
            bbox = frame.convert("RGBA").getchannel("A").getbbox()
        if bbox is None:
            raise ValueError("Referenced hero frame contains no visible pixels")
        return ((height - bbox[3] - pivot_y * height) / ppu,
                (height - bbox[1] - pivot_y * height) / ppu, 1 / ppu)
    raise ValueError("Referenced hero sprite absent from importer: " + str(local_id))


def world_rectangles(source: str) -> tuple[list[tuple[float, float, float, float]], tuple[float, float, float, float]]:
    walls = []
    for match in re.finditer(r'\bWall\("[^"]+",\s*' + VECTOR + r"\s*,\s*" + VECTOR + r"\s*\);", source):
        x, y, width, height = map(float, match.groups())
        if width <= 0 or height <= 0 or not all(math.isfinite(v) for v in (x, y, width, height)):
            raise ValueError("Wall dimensions must be finite and positive")
        walls.append((x, y, width / 2, height / 2))
    position = re.search(r"Gate\.transform\.localPosition\s*=\s*new Vector3\(\s*" + NUMBER
                         + r"\s*,\s*" + NUMBER + r"\s*,\s*" + NUMBER + r"\s*\)", source)
    size = re.search(r"Gate\.AddComponent<BoxCollider2D>\(\)\.size\s*=\s*" + VECTOR, source)
    if len(walls) < 4 or not position or not size:
        raise ValueError("Literal world walls/gate unavailable; update the geometry reader for the real scene")
    gate = (float(position[1]), float(position[2]), float(size[1]) / 2, float(size[2]) / 2)
    return walls, gate


def reachable(rectangles: list[tuple[float, float, float, float]], start: tuple[float, float],
              footprint: tuple[float, float] = (0, 0), step: float = .1) -> tuple[set[tuple[int, int]], bool]:
    """Independent 4-neighbour blockout traversal with optional actor clearance.

    Point traversal is the stricter closed-gate test: even a zero-width actor
    cannot bypass the connected seal. AABB clearance is conservative when testing
    the open route, since the native horizontal capsule fits inside this box.
    """
    limit_x = math.ceil(max(abs(x) + half_x for x, _, half_x, _ in rectangles) + 1)
    limit_y = math.ceil(max(abs(y) + half_y for _, y, _, half_y in rectangles) + 1)
    max_x, max_y = round(limit_x / step), round(limit_y / step)

    def blocked(cell):
        px, py = cell[0] * step, cell[1] * step
        return any(abs(px - x) <= half_x + footprint[0] + 1e-9
                   and abs(py - y) <= half_y + footprint[1] + 1e-9
                   for x, y, half_x, half_y in rectangles)

    origin = (round(start[0] / step), round(start[1] / step))
    if blocked(origin):
        raise ValueError("Player spawn intersects the blockout")
    visited = {origin}
    frontier = deque([origin])
    escaped = False
    while frontier:
        x, y = frontier.popleft()
        if abs(x) == max_x or abs(y) == max_y:
            escaped = True
        for neighbour in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if (abs(neighbour[0]) <= max_x and abs(neighbour[1]) <= max_y
                    and neighbour not in visited and not blocked(neighbour)):
                visited.add(neighbour)
                frontier.append(neighbour)
    return visited, escaped


class ClearingSceneContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = (PROJECT_ROOT / SCENE).read_text()
        cls.objects = scene_objects(cls.scene)
        cls.visuals = (PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingVisuals.cs").read_text()
        cls.runtime = (PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingRuntime.cs").read_text()
        cls.hud = (PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingHud.cs").read_text()
        cls.walls, cls.gate = world_rectangles(cls.visuals)
        cls.spawn = vector_constant(cls.visuals, "PlayerSpawn")
        cls.beacon = vector_constant(cls.visuals, "BeaconPosition")

    def test_scene_is_enabled_in_build_with_its_actual_guid(self):
        settings = (PROJECT_ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
        entries = re.findall(r"- enabled: 1\s+path: ([^\n]+)\s+guid: ([0-9a-f]{32})", settings)
        self.assertIn((SCENE, guid(PROJECT_ROOT / (SCENE + ".meta"))), entries)

    def test_bootstrap_is_active_and_resolves_real_player_and_camera(self):
        script_guid = guid(PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingRuntime.cs.meta")
        matches = [(local_id, body) for local_id, (kind, body) in self.objects.items()
                   if kind == 114 and f"guid: {script_guid}," in body]
        self.assertEqual(1, len(matches), "One real ClearingRuntime component must own the scene")
        bootstrap_id, body = matches[0]
        self.assertRegex(body, r"m_Enabled: 1\b")
        owner = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", body)[1])
        self.assertEqual(1, self.objects[owner][0])
        self.assertRegex(self.objects[owner][1], r"m_IsActive: 1\b")
        self.assertIn(f"component: {{fileID: {bootstrap_id}}}", self.objects[owner][1])
        player_id = int(re.search(r"\n  player: \{fileID: (\d+)\}", body)[1])
        camera_id = int(re.search(r"\n  sceneCamera: \{fileID: (\d+)\}", body)[1])
        self.assertEqual(114, self.objects[player_id][0])
        self.assertIn("guid: " + guid(PROJECT_ROOT / "Assets/Scripts/Player/Player.cs.meta") + ",",
                      self.objects[player_id][1])
        self.assertEqual(20, self.objects[camera_id][0])
        self.assertRegex(self.objects[camera_id][1], r"m_Enabled: 1\b")

    def test_original_hero_has_animation_body_and_audio_listener(self):
        bootstrap_guid = guid(PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingRuntime.cs.meta")
        bootstrap = next(body for kind, body in self.objects.values()
                         if kind == 114 and f"guid: {bootstrap_guid}," in body)
        player_id = int(re.search(r"\n  player: \{fileID: (\d+)\}", bootstrap)[1])
        owner = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", self.objects[player_id][1])[1])
        owned = [body for _, body in self.objects.values() if f"m_GameObject: {{fileID: {owner}}}\n" in body]
        self.assertTrue(any("Animator:\n" in body and "m_Controller: {fileID: 0}" not in body for body in owned))
        self.assertTrue(any("Rigidbody2D:\n" in body for body in owned))
        self.assertTrue(any("SpriteRenderer:\n" in body for body in owned))
        self.assertTrue(any(kind == 81 and "m_Enabled: 1" in body for kind, body in self.objects.values()),
                        "Synthesized feedback needs a real enabled scene AudioListener")

    def test_world_root_preserves_the_geometry_coordinate_space(self):
        script_guid = guid(PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingRuntime.cs.meta")
        bootstrap = next(body for kind, body in self.objects.values()
                         if kind == 114 and f"guid: {script_guid}," in body)
        owner = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", bootstrap)[1])
        roots = [body for kind, body in self.objects.values()
                 if kind == 4 and f"m_GameObject: {{fileID: {owner}}}\n" in body]
        self.assertEqual(1, len(roots))
        for field, expected in (("m_LocalPosition", (0, 0, 0)), ("m_LocalScale", (1, 1, 1))):
            value = re.search(field + r": \{x: ([\d.-]+), y: ([\d.-]+), z: ([\d.-]+)\}", roots[0])
            self.assertIsNotNone(value)
            self.assertEqual(expected, tuple(map(float, value.groups())))
        self.assertIn("m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}", roots[0])
        self.assertIn("m_Father: {fileID: 0}", roots[0])

    def test_actor_layers_collide_with_physical_walls_and_wall_queries(self):
        physics = (PROJECT_ROOT / "ProjectSettings/Physics2DSettings.asset").read_text()
        matrix = re.search(r"m_LayerCollisionMatrix: ([0-9a-f]+)", physics)
        self.assertIsNotNone(matrix)
        blob = bytes.fromhex(matrix[1])
        self.assertEqual(32 * 4, len(blob))
        rows = [int.from_bytes(blob[i * 4:(i + 1) * 4], "little") for i in range(32)]
        self.assertTrue(rows[2] & 1 and rows[0] & (1 << 2),
                        "Ignore Raycast actors must still collide with Default walls")
        self.assertIn("player.gameObject.layer = 2;", self.runtime)
        self.assertIn("enemy.layer = 2;", self.visuals)
        self.assertIn("Physics2D.Linecast(from, to, 1 << 0)", self.runtime)
        wall = method_body(self.visuals, "Wall")
        self.assertIn("item.AddComponent<BoxCollider2D>().size = size;", wall)
        self.assertIn("item.transform.localPosition = position;", wall)
        self.assertNotIn("isTrigger = true", wall)

    def test_seal_prevents_any_point_route_to_beacon(self):
        visited, _ = reachable([*self.walls, self.gate], self.spawn)
        target = (round(self.beacon[0] / .1), round(self.beacon[1] / .1))
        self.assertNotIn(target, visited, "The locked objective is reachable around the partition/gate")

    def test_outer_boundary_cannot_be_escaped_before_or_after_unlock(self):
        for rectangles in ([*self.walls, self.gate], self.walls):
            with self.subTest(gate_closed=len(rectangles) > len(self.walls)):
                _, escaped = reachable(rectangles, self.spawn)
                self.assertFalse(escaped, "Player can leave the entire play area around a wall end")

    def test_unlocked_route_has_clearance_for_actual_player_footprint(self):
        size = re.search(r"collider\.size\s*=\s*" + VECTOR, self.runtime)
        self.assertIsNotNone(size, "Read the actual player capsule size before claiming route clearance")
        offset = vector_constant(self.visuals, "PlayerFootOffset")
        self.assertIn("collider.offset = ClearingVisuals.PlayerFootOffset;", self.runtime)
        self.assertIn("playerBody.position + ClearingVisuals.PlayerFootOffset", self.runtime,
                      "Combat/interaction points must share the physical foot collider center")
        reset = method_body(self.runtime, "ResetRun")
        self.assertIn("playerBody.position = ClearingVisuals.PlayerSpawn - ClearingVisuals.PlayerFootOffset;", reset,
                      "PlayerSpawn is a foot position; using it as the sprite center shifts actual collision")
        footprint = (float(size[1]) / 2, float(size[2]) / 2)
        body_spawn = (self.spawn[0] - offset[0], self.spawn[1] - offset[1])
        center = (body_spawn[0] + offset[0], body_spawn[1] + offset[1])
        visited, escaped = reachable(self.walls, center, footprint)
        self.assertFalse(escaped)
        target = (round(self.beacon[0] / .1), round(self.beacon[1] / .1))
        self.assertIn(target, visited, "The gate opens but the player's footprint still cannot reach the beacon")

    def test_optional_supplies_are_reachable_before_unlock_without_cross_seal_interaction(self):
        supplies = (PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingSupplies.cs").read_text()
        count = re.search(r"const int Count\s*=\s*(\d+)\s*;", supplies)
        radius = re.search(r"const float UseRadius\s*=\s*" + NUMBER + r"\s*;", supplies)
        size = re.search(r"collider\.size\s*=\s*" + VECTOR, self.runtime)
        self.assertIsNotNone(count, "Read the actual number of authored supply points")
        self.assertIsNotNone(radius, "Read the actual supply interaction radius")
        self.assertIsNotNone(size, "Read the actual player foot collider before checking access")
        count = int(count[1])
        radius = float(radius[1])
        self.assertGreater(count, 0)
        self.assertTrue(math.isfinite(radius) and radius > 0)
        axes = []
        for axis in ("X", "Y"):
            body = method_body(supplies, "Position" + axis)
            positions = {int(match[1]): float(match[2]) for match in re.finditer(
                r"if\s*\(index\s*==\s*(\d+)\)\s*return\s*" + NUMBER + r"\s*;", body)}
            self.assertEqual(set(range(count)), set(positions),
                             "Every supply must have an actual authored " + axis + " coordinate")
            axes.append(positions)
        footprint = (float(size[1]) / 2, float(size[2]) / 2)
        closed = [*self.walls, self.gate]
        visited, escaped = reachable(closed, self.spawn, footprint)
        self.assertFalse(escaped)
        for index in range(count):
            point = (axes[0][index], axes[1][index])
            with self.subTest(supply=index):
                self.assertTrue(all(math.isfinite(value) for value in point))
                self.assertIn((round(point[0] / .1), round(point[1] / .1)), visited,
                              "Optional supply is inaccessible until the objective gate opens")
                # The entire use disk, including conservative actor clearance,
                # stays inside the same sealed region as its reachable center.
                # Thus it cannot offer an interaction from across any wall/gate.
                for x, y, half_x, half_y in closed:
                    dx = max(abs(point[0] - x) - half_x - footprint[0], 0)
                    dy = max(abs(point[1] - y) - half_y - footprint[1], 0)
                    self.assertGreater(math.hypot(dx, dy), radius,
                                       "Supply interaction disk reaches a sealed wall or gate")

    def test_real_scene_runtime_boots_the_boss_required_encounter(self):
        # The serialized bootstrap is checked above. Inspect its actual Start,
        # rather than treating a helper's default opt-out as live scene admission.
        start = method_body(self.runtime, "Start")
        self.assertIn("run = new ClearingRun(true);", start,
                      "The shipped scene still instantiates the three-guard-only helper path")
        self.assertIn("bossVisuals = new ClearingBossVisuals(transform, visuals);", start,
                      "The boss-required run has no owned actor/altar/locked warning presentation")
        reset = method_body(self.runtime, "ResetRun")
        self.assertIn("bossVisuals.Reset();", reset,
                      "A genuine retry retains the previous guardian's physical pose or feedback")
        update = method_body(self.runtime, "Update")
        self.assertLess(update.index("NearBeacon"), update.index("TryAwakenBoss();"),
                        "Guardian entry steals existing beacon interaction/save priority")

    def test_guardian_altar_and_spawn_are_accessible_inside_the_closed_encounter(self):
        boss = (PROJECT_ROOT / "Assets/Scripts/Gameplay/ClearingBossVisuals.cs").read_text()
        altar = vector_constant(boss, "AltarPosition")
        spawn = vector_constant(boss, "SpawnPosition")
        size = re.search(r"collider\.size\s*=\s*" + VECTOR, self.runtime)
        self.assertIsNotNone(size)
        footprint = (float(size[1]) / 2, float(size[2]) / 2)
        closed = [*self.walls, self.gate]
        visited, escaped = reachable(closed, self.spawn, footprint)
        self.assertFalse(escaped)
        for name, point in (("altar", altar), ("guardian foot spawn", spawn)):
            with self.subTest(point=name):
                self.assertIn((round(point[0] / .1), round(point[1] / .1)), visited,
                              "The required encounter point sits across its own closed gate")
        admission = method_body(self.runtime, "NearBossAltar") if re.search(
            r"\bNearBossAltar\s*\([^)]*\)", self.runtime) else self.runtime
        radius = re.search(r"Vector2\.Distance\(PlayerPoint,\s*ClearingBossVisuals\.AltarPosition\)\s*<=\s*" + NUMBER,
                           admission)
        self.assertIsNotNone(radius, "Read the actual actor-foot altar use radius")
        for x, y, half_x, half_y in closed:
            dx = max(abs(altar[0] - x) - half_x - footprint[0], 0)
            dy = max(abs(altar[1] - y) - half_y - footprint[1], 0)
            self.assertGreater(math.hypot(dx, dy), float(radius[1]),
                               "Guardian altar use disk reaches a sealed wall or beacon partition")

    def test_hero_raster_feet_and_north_camera_envelope_match_play_space(self):
        # Use actual scene/animation sprite references, not an assumed frame size.
        references = set(re.findall(r"(?:m_Sprite:|value:) \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 3\}", self.scene))
        for clip in (PROJECT_ROOT / "Assets/Animations/Player").glob("*.anim"):
            references.update(re.findall(r"value: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 3\}", clip.read_text()))
        self.assertGreater(len(references), 20, "Measure the existing idle/walk raster poses")
        metas = {guid(meta): meta for meta in (PROJECT_ROOT / "Assets").rglob("*.png.meta")}
        bounds = [sprite_vertical_bounds(metas[identity], int(local_id)) for local_id, identity in references]
        player_guid = guid(PROJECT_ROOT / "Assets/Scripts/Player/Player.cs.meta")
        player = next(body for kind, body in self.objects.values()
                      if kind == 114 and f"guid: {player_guid}," in body)
        owner = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", player)[1])
        transform = next(body for kind, body in self.objects.values()
                         if kind == 4 and f"m_GameObject: {{fileID: {owner}}}\n" in body)
        scale_y = float(re.search(r"m_LocalScale: \{x: [\d.-]+, y: ([\d.-]+), z: [\d.-]+\}", transform)[1])
        foot_y = vector_constant(self.visuals, "PlayerFootOffset")[1]
        for bottom, _, pixel in bounds:
            self.assertLessEqual(abs(foot_y - bottom * scale_y), 2 * pixel * scale_y,
                                 "The collider/combat point sits above the actual raster feet")
        size = re.search(r"collider\.size\s*=\s*" + VECTOR, self.runtime)
        half_foot_height = float(size[2]) / 2
        horizontal_walls = [wall for wall in self.walls if wall[2] > wall[3]]
        north = max(horizontal_walls, key=lambda wall: wall[1])
        south = min(horizontal_walls, key=lambda wall: wall[1])
        north_foot = north[1] - north[3] - half_foot_height
        south_foot = south[1] + south[3] + half_foot_height
        highest_head = north_foot - foot_y + max(top for _, top, _ in bounds) * scale_y
        lowest_foot = south_foot - foot_y + min(bottom for bottom, _, _ in bounds) * scale_y
        camera = method_body(self.runtime, "LateUpdate")
        minimum = float(re.search(r"orthographicSize\s*=\s*Mathf.Max\(\s*" + NUMBER, camera)[1])
        camera_y = float(re.search(r"sceneCamera\.transform\.position\s*=\s*new Vector3\(\s*" + NUMBER
                                  + r"\s*,\s*" + NUMBER, camera)[2])
        self.assertLessEqual(highest_head, camera_y + minimum,
                             "At the north walk boundary the existing hero's head is outside the camera")
        self.assertGreaterEqual(lowest_foot, camera_y - minimum,
                                "The lower reachable foot boundary falls outside the camera")

    def test_hud_preserves_central_combat_space_at_reference_resolution(self):
        reference = re.search(r"scaler\.referenceResolution\s*=\s*" + VECTOR, self.hud)
        self.assertIsNotNone(reference)
        width, height = map(float, reference.groups())
        labels = list(re.finditer(r'\bLabel\("([^"]+)",\s*' + VECTOR + r"\s*,\s*" + VECTOR
                                  + r"\s*,\s*" + VECTOR, self.hud))
        self.assertGreaterEqual(len(labels), 5, "No live HUD labels were measured")
        for label in labels:
            name = label[1]
            if name == "Run result":
                continue  # Center messages belong only to paused/terminal runs.
            ax, ay, ox, oy, sx, sy = map(float, label.groups()[1:])
            left, bottom = ax * width + ox - ax * sx, ay * height + oy - ay * sy
            right, top = left + sx, bottom + sy
            with self.subTest(label=name):
                self.assertGreaterEqual(left, 0)
                self.assertGreaterEqual(bottom, 0)
                self.assertLessEqual(right, width)
                self.assertLessEqual(top, height)
                overlaps_center = right > width * .25 and left < width * .75 and top > height * .2 and bottom < height * .8
                self.assertFalse(overlaps_center, "A persistent label covers the central combat view")

    def test_hud_has_resolution_scaling_and_no_input_capture(self):
        self.assertIn("CanvasScaler.ScaleMode.ScaleWithScreenSize", self.hud)
        self.assertIn("label.raycastTarget = false;", self.hud)
        self.assertNotIn("GraphicRaycaster", self.hud)
        self.assertNotIn("typeof(Image)", self.hud, "Opaque panels need a separate occlusion review")
        self.assertRegex(self.hud, r'(?s)terminal\.text\s*=.*?:\s*"";',
                         "Center result text must be empty during an active run")

    def test_combat_bridge_observes_latest_movement_input_and_facing(self):
        order = re.search(r"\[DefaultExecutionOrder\((\d+)\)\]", self.runtime)
        self.assertIsNotNone(order, "Combat must run after the default-order movement input snapshot")
        self.assertGreater(int(order[1]), 0)
        attack = method_body(self.runtime, "StartAttack")
        self.assertIn("strikeDirection = movement.FacingDirection", attack,
                      "Animator normalized diagonals can disagree with the player's current facing")

    def test_death_is_observed_before_attack_and_objective_input(self):
        update = method_body(self.runtime, "Update")
        synchronize = update.index("SynchronizeDeath();")
        self.assertLess(synchronize, update.index("KeyCode.J"))
        self.assertLess(synchronize, update.index("KeyCode.E)"))
        death = method_body(self.runtime, "SynchronizeDeath")
        self.assertIn("float.IsNaN", death)
        self.assertIn("float.IsInfinity", death)
        self.assertIn("run.NotifyPlayerDeath();", death)
        self.assertIn("StopActors();", death)
        self.assertIn("health.DebugDamageEnabled = false;", self.runtime,
                      "The legacy debug key must not silently kill the player inside the shipped encounter")

    def test_strike_visible_rectangle_matches_directional_contact(self):
        contact = method_body(self.runtime, "InStrike")
        branches = re.findall(r"forward\s*>=\s*" + NUMBER + r"\s*&&\s*forward\s*<=\s*" + NUMBER
                              + r"\s*&&\s*side\s*<=\s*" + NUMBER, contact)
        self.assertEqual(2, len(branches), "Both player attacks need readable directional contact rectangles")
        views = method_body(self.runtime, "RefreshViews")
        centers = re.search(r"float center\s*=\s*strikeKind == PlayerAttackKind.Light\s*\?\s*" + NUMBER
                            + r"\s*:\s*" + NUMBER, views)
        scales = re.search(r"strike\.transform\.localScale\s*=\s*strikeKind == PlayerAttackKind.Light\s*\?\s*new Vector3\("
                           + NUMBER + r"\s*,\s*" + NUMBER + r"\s*,\s*1\)\s*:\s*new Vector3\("
                           + NUMBER + r"\s*,\s*" + NUMBER + r"\s*,\s*1\)", views)
        self.assertIsNotNone(centers)
        self.assertIsNotNone(scales)
        for index, branch in enumerate(branches):
            near, far, side = map(float, branch)
            with self.subTest(kind=("light", "burst")[index]):
                center = float(centers[index + 1])
                length, width = float(scales[index * 2 + 1]), float(scales[index * 2 + 2])
                self.assertAlmostEqual(near, center - length / 2)
                self.assertAlmostEqual(far, center + length / 2)
                self.assertAlmostEqual(side, width / 2)


if __name__ == "__main__":
    unittest.main()
