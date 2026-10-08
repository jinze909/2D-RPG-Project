#!/usr/bin/env python3
"""Run actual presentation C# with recording Unity boundaries, not native Unity.

No real physics, Canvas layout, rendering, Animator playback or audible device
output is simulated. The source root can be a retained baseline for red checks.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import os
import subprocess
import sys
import tempfile

from run_player_checks import discover_tools


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    trusted = Path(__file__).resolve().parents[1]
    sources = sorted((args.project_root.resolve() / "Assets").rglob("*.cs"))
    if not sources:
        print("No production C# sources found", file=sys.stderr)
        return 2
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 2
    with tempfile.TemporaryDirectory(prefix="rpg-presentation-checks-") as directory:
        output = Path(directory) / "checks.exe"
        command = [*compiler, "-nologo", "-nowarn:0649,0067", "-out:" + str(output),
                   *map(str, sorted((trusted / "tests/offline").glob("*BoundaryStubs.cs"))),
                   str(trusted / "tests/presentation/PresentationBehaviorChecks.cs"), *map(str, sources)]
        print(f"Compiling {len(sources)} actual project C# files for recording-boundary presentation checks", flush=True)
        compiled = subprocess.run(command, check=False)
        if compiled.returncode:
            return compiled.returncode
        os.chmod(output, 0o644)
        try:
            return subprocess.run([mono, str(output)], check=False, timeout=120).returncode
        except subprocess.TimeoutExpired:
            print("Presentation checks exceeded 120 seconds", file=sys.stderr)
            return 124


if __name__ == "__main__":
    raise SystemExit(main())
