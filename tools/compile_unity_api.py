#!/usr/bin/env python3
"""Compile production C# against installed Unity 2022.3 engine/Editor DLLs.

This checks real Unity API signatures without starting an editor or using a
license. It is not Unity import, a native test, a build, or a playtest. When a
compiled Input System package is unavailable, only its API is replaced by the
reviewed offline input boundary; the generated PlayerActions source still
compiles. The existing behavior runners remain separate and unchanged.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile


def find_editor(explicit: Path | None) -> Path:
    candidates = []
    if explicit:
        candidates.append(explicit)
    elif os.environ.get("UNITY_EDITOR_PATH"):
        candidates.append(Path(os.environ["UNITY_EDITOR_PATH"]))
    else:
        installation = Path("/workspace/.cloud-tools/unity-onboarding")
        candidates.extend(sorted(installation.glob("*/Editor")))
        candidates.append(Path("/opt/unity/Editor"))
    for path in candidates:
        directory = path.parent if path.name == "Unity" else path
        if (directory / "Unity").is_file() and (directory / "Data/Managed/UnityEngine").is_dir():
            return directory.resolve()
    raise RuntimeError("Installed Unity engine reference assemblies unavailable; use --editor-path.")


def input_boundary_namespaces(content: str) -> str:
    """Keep only the reviewed Input namespaces, never later Unity doubles."""
    namespaces = []
    # Ignore comments/literals while finding the matching namespace brace.
    tokens = re.compile(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:[^"\\]|\\.)*"|\'(?:[^\'\\]|\\.)*\'|[{}]', re.S)
    for name in ("UnityEngine.InputSystem.Utilities", "UnityEngine.InputSystem"):
        match = re.search(r"namespace\s+" + re.escape(name) + r"\s*\{", content)
        if not match:
            raise RuntimeError("Reviewed Input boundary namespace is missing: " + name)
        depth = 1
        for token in tokens.finditer(content, match.end()):
            if token.group() == "{":
                depth += 1
            elif token.group() == "}":
                depth -= 1
                if depth == 0:
                    namespaces.append(content[match.start():token.end()])
                    break
        else:
            raise RuntimeError("Reviewed Input boundary namespace is incomplete: " + name)
    return "\n".join(namespaces)


def compile_sources(root: Path, editor: Path, input_system_dll: Path | None) -> dict:
    binary = editor / "Unity"
    version = subprocess.check_output([str(binary), "-version"], text=True, timeout=15).strip()
    if version != "2022.3.53f1":
        raise RuntimeError("Expected international Unity 2022.3.53f1; detected " + version)
    mono = editor / "Data/MonoBleedingEdge/bin/mono"
    compiler = editor / "Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
    if not mono.is_file() or not compiler.is_file():
        raise RuntimeError("Installed editor's Mono/C# compiler is unavailable.")

    sources = sorted((root / "Assets").rglob("*.cs"))
    if not sources:
        raise RuntimeError("No production Assets C# files found.")
    modules = editor / "Data/Managed/UnityEngine"
    references = sorted(modules.glob("UnityEngine*.dll"))
    references.extend(sorted(modules.glob("UnityEditor*.dll")))
    editor_reference = modules / "UnityEditor.CoreModule.dll"
    if not editor_reference.is_file():
        raise RuntimeError("Real UnityEditor reference assembly is unavailable.")
    # The root Managed/UnityEditor.dll is a monolithic compatibility assembly;
    # mixing it with split modules causes duplicate Editor definitions.
    standard = editor / "Data/NetStandard/ref/2.1.0/netstandard.dll"
    if not standard.is_file():
        raise RuntimeError("Installed editor's .NET Standard 2.1 reference is unavailable.")
    references.append(standard)
    ui_candidates = [root / "Library/ScriptAssemblies/UnityEngine.UI.dll"]
    template_cache = editor / "Data/Resources/PackageManager/ProjectTemplates/libcache"
    ui_candidates.extend(sorted(template_cache.glob("*/ScriptAssemblies/UnityEngine.UI.dll")))
    native_ui = next((path for path in ui_candidates if path.is_file()), None)
    if native_ui:
        references.append(native_ui.resolve())
    native_input = input_system_dll or root / "Library/ScriptAssemblies/Unity.InputSystem.dll"
    input_boundary = not native_input.is_file()
    if not input_boundary:
        references.append(native_input.resolve())
    elif input_system_dll:
        raise RuntimeError("Requested Input System reference does not exist: " + str(input_system_dll))

    with tempfile.TemporaryDirectory(prefix="rpg-unity-api-") as directory:
        temporary = Path(directory)
        extra_sources = []
        if input_boundary:
            # Compile with the same reviewed Input API boundary used by actual-
            # source behavior checks. Never replace UnityEngine itself here.
            fixture = Path(__file__).resolve().parents[1] / "tests/offline/UnityBoundaryStubs.cs"
            content = fixture.read_text()
            boundary = temporary / "InputSystemBoundary.cs"
            boundary.write_text("using System;\nusing System.Collections;\nusing System.Collections.Generic;\n"
                                + input_boundary_namespaces(content))
            extra_sources.append(boundary)
        # The current project imports this package namespace without using any
        # of its types. A marker is not a UI component or an implementation.
        if not native_ui:
            namespace_marker = temporary / "UnusedEventSystemsNamespace.cs"
            namespace_marker.write_text("namespace UnityEngine.EventSystems {}\n")
            extra_sources.append(namespace_marker)
        command = [str(mono), str(compiler), "-nologo", "-target:library", "-langversion:9.0", "-nostdlib",
                   "-nowarn:0649,0067", "-define:UNITY_EDITOR,UNITY_2022_3",
                   "-out:" + str(temporary / "ProjectApiCheck.dll"),
                   *("-r:" + str(path) for path in references), *map(str, extra_sources), *map(str, sources)]
        result = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                timeout=120, check=False)
    return {"check": "Unity API reference compilation", "unity": version,
            "source_count": len(sources), "engine_reference_count": len(references),
            "input_system": "reviewed boundary double" if input_boundary else "compiled package assembly",
            "ugui": "real installed package assembly" if native_ui else "unused EventSystems namespace marker only",
            "unity_engine": "real installed reference assemblies", "unity_editor": "real installed reference assembly",
            "exit_code": result.returncode, "compiler_output": result.stdout,
            "native_import": False, "native_tests": False, "gameplay_executed": False}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", "--project-root", type=Path,
                        default=Path(__file__).resolve().parents[1])
    parser.add_argument("--editor-path", type=Path)
    parser.add_argument("--input-system-dll", type=Path)
    parser.add_argument("--output", type=Path, help="Optional JSON evidence file")
    args = parser.parse_args()
    try:
        report = compile_sources(args.root.resolve(), find_editor(args.editor_path), args.input_system_dll)
    except (RuntimeError, OSError, subprocess.SubprocessError) as error:
        print(str(error), file=sys.stderr)
        return 2
    print(json.dumps(report, indent=2))
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + "\n")
    return report["exit_code"]


if __name__ == "__main__":
    raise SystemExit(main())
