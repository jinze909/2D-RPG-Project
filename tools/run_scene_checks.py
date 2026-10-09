#!/usr/bin/env python3
"""Check serialized scene integration and independent blockout reachability.

This is a static/offline check; native Unity physics, camera rendering, Canvas
layout at actual resolutions, audio and gameplay acceptance remain unverified.
"""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path
import sys
import unittest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    test_path = Path(__file__).resolve().parents[1] / "tests/clearing_scene/test_scene_contracts.py"
    spec = importlib.util.spec_from_file_location("rpg_clearing_scene_contracts", test_path)
    if spec is None or spec.loader is None:
        print("Scene contract test module unavailable", file=sys.stderr)
        return 2
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.PROJECT_ROOT = args.project_root.resolve()
    suite = unittest.defaultTestLoader.loadTestsFromModule(module)
    if suite.countTestCases() == 0:
        print("No scene contract tests discovered", file=sys.stderr)
        return 2
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    print("Static scene/level contracts only: no native Unity physics, rendering, UI layout or gameplay was run.")
    return 0 if result.wasSuccessful() and result.testsRun > 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
