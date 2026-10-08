#!/usr/bin/env python3
"""Compile and check actual engine-independent clearing rules with Mono.

These are deterministic offline C# tests, without Unity or boundary doubles.
They do not validate scene collisions, range/facing checks, actual Input System,
animation rendering, UI, audio, player-resource bridge integration or Play Mode.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import subprocess
import sys
import tempfile

from run_player_checks import discover_tools


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", "--source-root", dest="project_root", type=Path,
                        default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.project_root.resolve()
    source = root / "Assets/Scripts/Gameplay/ClearingRules.cs"
    fixture = Path(__file__).resolve().parents[1] / "tests/gameplay/ClearingRulesChecks.cs"
    if not source.is_file() or not fixture.is_file():
        print("Clearing rules source or behavior fixture missing", file=sys.stderr)
        return 2
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 2
    with tempfile.TemporaryDirectory(prefix="rpg-clearing-checks-") as directory:
        output = Path(directory) / "checks.exe"
        print("Compiling actual clearing rules and deterministic tests without Unity doubles", flush=True)
        result = subprocess.run([*compiler, "-nologo", "-out:" + str(output), str(source), str(fixture)],
                                check=False, timeout=120)
        if result.returncode:
            return result.returncode
        try:
            return subprocess.run([mono, str(output)], check=False, timeout=120).returncode
        except subprocess.TimeoutExpired:
            print("Clearing rules checks timed out after 120 seconds", file=sys.stderr)
            return 124


if __name__ == "__main__":
    raise SystemExit(main())
