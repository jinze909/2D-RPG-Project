#!/usr/bin/env python3
"""Check serialized input/animation contracts, not native Unity behavior."""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path
import unittest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    test_path = Path(__file__).resolve().parents[1] / "tests/input_animation/test_input_animation.py"
    spec = importlib.util.spec_from_file_location("rpg_input_animation_contracts", test_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.PROJECT_ROOT = args.project_root.resolve()
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromModule(module))
    print("Static input/animation contracts only: native device input, Animator playback and Game View remain unverified.")
    return 0 if result.wasSuccessful() else 1


if __name__ == "__main__":
    raise SystemExit(main())
