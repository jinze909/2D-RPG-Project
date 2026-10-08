#!/usr/bin/env python3
"""Compile actual project C# and run behavior checks without a Unity license.

Requires Mono with mcs/csc; Unity's bundled Mono/Roslyn is also supported.
All assemblies are temporary. This is not Unity import, native physics, or a
Play Mode test. Use RPG_MONO and RPG_CSC/RPG_MCS to select explicit tools.
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


def discover_tools() -> tuple[str, list[str]]:
    mono = os.environ.get("RPG_MONO") or shutil.which("mono")
    compiler = os.environ.get("RPG_CSC") or os.environ.get("RPG_MCS")
    if mono and compiler:
        return mono, [mono, compiler] if compiler.lower().endswith(".exe") else [compiler]
    if mono:
        compiler = shutil.which("mcs") or shutil.which("csc")
        if compiler:
            return mono, [compiler]

    roots = []
    if os.environ.get("UNITY_EDITOR_PATH"):
        editor = Path(os.environ["UNITY_EDITOR_PATH"])
        roots.append(editor.parent if editor.name == "Unity" else editor)
    for installation in (Path("/workspace/.cloud-tools/unity-onboarding"), Path("/opt/unity/Editor")):
        if installation.is_dir():
            roots.extend([installation, *sorted(installation.glob("*/Editor"))])
    for editor in roots:
        bundled_mono = editor / "Data/MonoBleedingEdge/bin/mono"
        bundled_csc = editor / "Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
        if bundled_mono.is_file() and bundled_csc.is_file():
            selected_mono = mono or str(bundled_mono)
            selected_compiler = compiler or str(bundled_csc)
            return selected_mono, ([selected_mono, selected_compiler]
                if selected_compiler.lower().endswith(".exe") else [selected_compiler])
    raise RuntimeError("Mono/compiler unavailable. Install mono-devel, or set RPG_MONO and RPG_CSC/RPG_MCS.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", "--source-root", dest="project_root", type=Path,
                        default=Path(__file__).resolve().parents[1],
                        help="Checkout containing the production Assets scripts")
    args = parser.parse_args()
    root = args.project_root.resolve()
    tests = Path(__file__).resolve().parents[1] / "tests/offline"
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 2

    sources = sorted((root / "Assets").rglob("*.cs"))
    if not sources:
        print("No production C# scripts found", file=sys.stderr)
        return 2
    with tempfile.TemporaryDirectory(prefix="rpg-player-checks-") as directory:
        # A separate unprivileged verification UID must be able to read the
        # assembly without being able to replace it or trusted reports.
        os.chmod(directory, 0o755)
        output = Path(directory) / "checks.exe"
        command = [*compiler, "-nologo", "-nowarn:0649,0067", "-out:" + str(output),
                   *map(str, sorted(tests.glob("*BoundaryStubs.cs"))), str(tests / "MovementBehaviorChecks.cs"),
                   *map(str, sources)]
        print(f"Compiling {len(sources)} actual project C# files with boundary doubles", flush=True)
        compiled = subprocess.run(command, check=False)
        if compiled.returncode:
            return compiled.returncode
        os.chmod(output, 0o644)
        print("Compilation passed; native Unity import/physics/rendering remain unverified", flush=True)
        try:
            return subprocess.run([mono, str(output)], check=False, timeout=120).returncode
        except subprocess.TimeoutExpired:
            print("Offline behavior checks timed out after 120 seconds", file=sys.stderr)
            return 124


if __name__ == "__main__":
    raise SystemExit(main())
