#!/usr/bin/env python3
"""Execute actual boss rules and locked attack geometry without Unity or doubles.

These pure C# checks validate domain admission, timing, contacts and completion.
Native movement, physics, input, animation, audio and playability are not covered.
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
    sources = [root / "Assets/Scripts/Gameplay" / name for name in (
        "ClearingRules.cs", "ClearingBoss.cs", "SentinelTactics.cs")]
    fixture = Path(__file__).resolve().parents[1] / "tests/boss/ClearingBossChecks.cs"
    missing = [str(path) for path in [*sources, fixture] if not path.is_file()]
    if missing:
        print("Boss source or fixture missing: " + ", ".join(missing), file=sys.stderr)
        return 2
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 2
    with tempfile.TemporaryDirectory(prefix="rpg-boss-checks-") as directory:
        output = Path(directory) / "checks.exe"
        print("Compiling actual boss and clearing rules; no Unity doubles", flush=True)
        result = subprocess.run([*compiler, "-nologo", "-out:" + str(output),
                                 *map(str, sources), str(fixture)], check=False, timeout=120)
        if result.returncode:
            return result.returncode
        try:
            return subprocess.run([mono, str(output)], check=False, timeout=120).returncode
        except subprocess.TimeoutExpired:
            print("Boss checks timed out after 120 seconds", file=sys.stderr)
            return 124


if __name__ == "__main__":
    raise SystemExit(main())
