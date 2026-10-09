#!/usr/bin/env python3
"""Compile production pixel builders and check their Color[] plus unchanged hero art.

The optional preview converts the exact managed RGBA output into a contact sheet.
It is an offline art preview, never a Unity screenshot or visual acceptance result.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

from run_player_checks import discover_tools


def preview(raw_directory: Path, target: Path) -> None:
    from PIL import Image, ImageDraw
    sheet = Image.new("RGB", (790, 350), (27, 48, 42))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 10), "OFFLINE ART: exact production C# pixels; no Unity rendering", fill=(240, 231, 193))
    positions = [(12, 50), (410, 50), (12, 205), (530, 115), (660, 205), (700, 50), (400, 240)]
    for row, (x, y) in zip((raw_directory / "rasters.tsv").read_text().splitlines(), positions):
        name, width, height = row.split("\t")
        size = (int(width), int(height))
        # Unity arrays use a bottom-left origin. This is a lossless row-order conversion.
        art = Image.frombytes("RGBA", size, (raw_directory / (name + ".rgba")).read_bytes())
        art = art.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        scale = 2 if name in ("ground", "wall", "path") else 3
        art = art.resize((size[0] * scale, size[1] * scale), Image.Resampling.NEAREST)
        draw.text((x, y - 16), f"{name} ({width}x{height})", fill=(240, 231, 193))
        sheet.paste(art, (x, y), art)
    target.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(target)
    print("Offline managed-raster contact sheet: " + str(target))


def main() -> int:
    trusted = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=trusted)
    parser.add_argument("--preview", type=Path)
    args = parser.parse_args()
    root = args.project_root.resolve()
    baseline = json.loads((trusted / "tests/art/hero-baseline.json").read_text())
    failures = []
    for name, expected in baseline["files_sha256"].items():
        path = root / name
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            failures.append(name)
    print(("FAIL" if failures else "PASS") + " original hero PNG/import hashes unchanged from " + baseline["commit"])
    for name in failures:
        print("Hero baseline mismatch: " + name)
    try:
        mono, compiler = discover_tools()
    except RuntimeError as error:
        print(str(error))
        return 2
    tests = trusted / "tests/art"
    production = root / "Assets/Scripts/Gameplay"
    with tempfile.TemporaryDirectory(prefix="rpg-raster-checks-") as directory:
        temporary = Path(directory)
        assembly = temporary / "raster-checks.exe"
        sources = [tests / "UnityColorBoundary.cs", tests / "RasterBehaviorChecks.cs",
                   production / "ClearingPalette.cs", production / "ClearingPixelArt.cs"]
        print("Compiling actual managed raster sources with a Color value boundary", flush=True)
        compiled = subprocess.run([*compiler, "-nologo", "-out:" + str(assembly), *map(str, sources)], check=False)
        if compiled.returncode:
            return compiled.returncode
        command = [mono, str(assembly)]
        if args.preview:
            command.append(str(temporary / "rgba"))
        result = subprocess.run(command, check=False, timeout=120)
        if args.preview:
            preview(temporary / "rgba", args.preview)
        return result.returncode or bool(failures)


if __name__ == "__main__":
    raise SystemExit(main())
