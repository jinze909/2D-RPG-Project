#!/usr/bin/env python3
"""Run actual health/mana/animation C# with recording Unity boundary doubles.

This compiles and executes production methods but is not a native Unity import,
Animator transition, PlayMode, rendering, or input-device test. Fixtures remain
in this trusted runner checkout even when --project-root selects another source.
Requires Mono/compiler or Unity's bundled Mono; no Unity license is used.
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
    native_mono = shutil.which("mono")
    mono = os.environ.get("RPG_MONO") or native_mono
    compiler = os.environ.get("RPG_CSC") or os.environ.get("RPG_MCS")
    if mono and not compiler:
        compiler = shutil.which("mcs") or shutil.which("csc")
    if mono and compiler:
        return mono, [native_mono or mono, compiler] if compiler.lower().endswith(".exe") else [compiler]

    editors: list[Path] = []
    if os.environ.get("UNITY_EDITOR_PATH"):
        editor = Path(os.environ["UNITY_EDITOR_PATH"])
        editors.append(editor.parent if editor.name == "Unity" else editor)
    installation = Path("/workspace/.cloud-tools/unity-onboarding")
    if installation.is_dir():
        editors.extend(sorted(installation.glob("*/Editor")))
    editors.append(Path("/opt/unity/Editor"))
    for editor in editors:
        bundled_mono = editor / "Data/MonoBleedingEdge/bin/mono"
        bundled_csc = editor / "Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
        if bundled_mono.is_file() and bundled_csc.is_file():
            selected_mono = mono or str(bundled_mono)
            selected_compiler = compiler or str(bundled_csc)
            return selected_mono, ([str(bundled_mono), selected_compiler]
                if selected_compiler.lower().endswith(".exe") else [selected_compiler])
    raise RuntimeError("Mono/compiler unavailable; install mono-devel or set RPG_MONO and RPG_CSC/RPG_MCS.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", "--source-root", type=Path,
                        default=Path(__file__).resolve().parents[1],
                        help="Checkout whose production Assets scripts are compiled")
    args = parser.parse_args()
    root = args.project_root.resolve()
    fixtures = Path(__file__).resolve().parents[1] / "tests/resources"
    production = [root / "Assets/Scripts" / path for path in (
        "Extra/IDamageable.cs", "Player/PlayerStats.cs", "Player/PlayerAnimations.cs",
        "Player/PlayerHealth.cs", "Player/PlayerMana.cs")]
    missing = [str(path.relative_to(root)) for path in production if not path.is_file()]
    if missing:
        print("Missing production C# files: " + ", ".join(missing), file=sys.stderr)
        return 2
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 2

    with tempfile.TemporaryDirectory(prefix="rpg-resource-checks-") as directory:
        # Trusted compiler owns the directory. The verification-only user can
        # traverse/read it, but cannot alter the assembly or validation reports.
        Path(directory).chmod(0o755)
        executable = Path(directory) / "checks.exe"
        command = [*compiler, "-nologo", "-nowarn:0649", "-out:" + str(executable),
                   str(fixtures / "UnityResourceBoundaryStubs.cs"),
                   str(fixtures / "ResourceBehaviorChecks.cs"), *map(str, production)]
        print("Compiling 5 actual production C# files with resource boundary doubles", flush=True)
        compiled = subprocess.run(command, check=False)
        if compiled.returncode:
            return compiled.returncode
        executable.chmod(0o644)
        print("Offline C# compilation passed; native Unity/Animator tests remain unverified", flush=True)
        try:
            return subprocess.run([mono, str(executable)], check=False, timeout=120).returncode
        except subprocess.TimeoutExpired:
            print("Resource behavior checks exceeded the 120-second limit", file=sys.stderr)
            return 124


if __name__ == "__main__":
    raise SystemExit(main())
