using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Rpg.Gameplay;

// Execute production Color[] generation directly. These are raster contracts,
// not visual quality acceptance or Unity texture/rendering tests.
public static class RasterBehaviorChecks
{
    static readonly string[] Names = { "ground", "wall", "path", "sentinel", "plinth", "crystal", "rune" };
    static ClearingRaster[] Build()
    {
        return new[] { ClearingPixelArt.RasterGround(96, 64), ClearingPixelArt.RasterWall(90, 18),
            ClearingPixelArt.RasterPath(28, 66), ClearingPixelArt.RasterSentinel(),
            ClearingPixelArt.RasterPlinth(), ClearingPixelArt.RasterCrystal(), ClearingPixelArt.RasterRune() };
    }
    static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    static bool Same(Color a, Color b) { return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a; }
    static int Byte(float value) { return (int)Math.Round(value * 255f); }
    static int Rgb(Color value) { return (Byte(value.r) << 16) | (Byte(value.g) << 8) | Byte(value.b); }
    static List<Color> Palette()
    {
        var result = new List<Color>(); var uniqueRgb = new HashSet<int>();
        foreach (var field in typeof(ClearingPalette).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(Color)) continue;
            Color color = (Color)field.GetValue(null);
            Expect(Finite(color.r) && Finite(color.g) && Finite(color.b) && color.r >= 0 && color.r <= 1
                && color.g >= 0 && color.g <= 1 && color.b >= 0 && color.b <= 1 && color.a == 1,
                "Invalid shared palette entry " + field.Name);
            Expect(uniqueRgb.Add(Rgb(color)), "Duplicate palette entry " + field.Name);
            result.Add(color);
        }
        Expect(result.Count >= 12 && result.Count <= 20, "Limited palette is missing or unexpectedly broad");
        return result;
    }
    static void Reject(Action operation)
    {
        try { operation(); } catch (ArgumentOutOfRangeException) { return; }
        throw new Exception("Invalid dimension was not explicitly rejected");
    }
    static int Visible(ClearingRaster art)
    { int count = 0; foreach (Color pixel in art.Pixels) if (pixel.a > 0) count++; return count; }
    static int ConnectedPixels(ClearingRaster art)
    {
        var visited = new HashSet<int>(); var queue = new Queue<int>();
        for (int i = 0; i < art.Pixels.Length; i++) if (art.Pixels[i].a > 0) { visited.Add(i); queue.Enqueue(i); break; }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue(), x = index % art.Width, y = index / art.Width;
            foreach (int neighbour in new[] { x > 0 ? index - 1 : -1, x < art.Width - 1 ? index + 1 : -1,
                y > 0 ? index - art.Width : -1, y < art.Height - 1 ? index + art.Width : -1 })
                if (neighbour >= 0 && art.Pixels[neighbour].a > 0 && visited.Add(neighbour)) queue.Enqueue(neighbour);
        }
        return visited.Count;
    }
    static int Main(string[] args)
    {
        int passed = 0, failed = 0;
        Action<string, Action> test = (name, body) => {
            try { body(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
        };
        test("limited shared palette has bounded unique opaque colors", () => Palette());
        test("every raster uses the shared palette and binary finite alpha", () => {
            var palette = Palette();
            foreach (ClearingRaster art in Build()) {
                Expect(art.Width > 0 && art.Height > 0 && art.Pixels.Length == art.Width * art.Height, "Invalid raster layout");
                foreach (Color pixel in art.Pixels) {
                    Expect(Finite(pixel.a) && (pixel.a == 0 || pixel.a == 1), "Raster contains blended/nonfinite alpha");
                    Expect(Finite(pixel.r) && Finite(pixel.g) && Finite(pixel.b), "Raster contains nonfinite RGB");
                    if (pixel.a > 0) Expect(palette.Exists(color => Same(pixel, color)), "Raster introduced an untracked RGB color");
                }
            }
        });
        test("environment ground and walls are fully opaque at actual scene dimensions", () => {
            foreach (ClearingRaster art in new[] { ClearingPixelArt.RasterGround(438, 258),
                ClearingPixelArt.RasterWall(15, 273), ClearingPixelArt.RasterWall(447, 15) })
                Expect(Visible(art) == art.Pixels.Length, "Environment has transparent holes");
        });
        test("all raster builders are deterministic and allocate independent buffers", () => {
            var first = Build(); var second = Build();
            for (int i = 0; i < first.Length; i++) {
                Expect(!Object.ReferenceEquals(first[i].Pixels, second[i].Pixels), "Shared mutable pixel buffer");
                Expect(first[i].Pixels.Length == second[i].Pixels.Length, "Unstable raster size");
                for (int j = 0; j < first[i].Pixels.Length; j++) Expect(Same(first[i].Pixels[j], second[i].Pixels[j]), "Non-deterministic pixels");
            }
        });
        test("sentinel silhouette is connected with two grounded boots and side clearance", () => {
            var art = ClearingPixelArt.RasterSentinel(); int visible = Visible(art), bootSegments = 0; bool previous = false;
            Expect(art.Width == 34 && art.Height == 42, "Sentinel changed the production canvas contract");
            Expect(visible > art.Pixels.Length * .35 && visible < art.Pixels.Length * .85, "Silhouette is empty or rectangular");
            Expect(ConnectedPixels(art) == visible, "Detached opaque pixels or disconnected limbs");
            for (int x = 0; x < art.Width; x++) { bool opaque = art.Pixels[x].a > 0; if (opaque && !previous) bootSegments++; previous = opaque; }
            Expect(bootSegments == 2, "Foot baseline needs two separated grounded boots");
            for (int y = 0; y < art.Height; y++) Expect(art.Pixels[y * art.Width].a == 0
                && art.Pixels[y * art.Width + art.Width - 1].a == 0, "Actor silhouette clips a side edge");
        });
        test("beacon and rune art have transparent backgrounds and connected silhouettes", () => {
            foreach (ClearingRaster art in new[] { ClearingPixelArt.RasterPlinth(), ClearingPixelArt.RasterCrystal(), ClearingPixelArt.RasterRune() }) {
                int visible = Visible(art); Expect(visible > 0 && visible < art.Pixels.Length, "Blank art or opaque backing rectangle");
                Expect(ConnectedPixels(art) == visible, "Stray floating pixels in objective art");
            }
        });
        test("ground texture stays quiet around combat silhouettes", () => {
            var art = ClearingPixelArt.RasterGround(438, 258); int quiet = 0;
            foreach (Color pixel in art.Pixels) if (Same(pixel, ClearingPalette.Ground) || Same(pixel, ClearingPalette.Forest)) quiet++;
            Expect(quiet > art.Pixels.Length * .8, "Decorative texture dominates the traversable ground");
        });
        test("raster writes clip and cannot wrap into another row", () => {
            var art = new ClearingRaster(3, 3); art.Rect(-1, -1, 3, 3, ClearingPalette.Amber);
            Expect(Visible(art) == 4, "Clipped rectangle has incorrect coverage");
            art.Pixel(3, 0, ClearingPalette.Cyan); art.Pixel(-1, 1, ClearingPalette.Cyan); art.Rect(1, 1, -2, 3, ClearingPalette.Cyan);
            Expect(Visible(art) == 4 && Same(art.Pixels[3], ClearingPalette.Amber), "Out-of-range pixel wrapped or negative rectangle drew");
        });
        test("unsafe dimensions reject before raster allocation", () => {
            foreach (var size in new[] { new[] { 0, 1 }, new[] { -1, 1 }, new[] { 1, 0 }, new[] { 1, -1 }, new[] { 2049, 1 }, new[] { 1, 2049 } }) {
                int width = size[0], height = size[1];
                Reject(() => ClearingPixelArt.RasterGround(width, height));
                Reject(() => ClearingPixelArt.RasterWall(width, height));
                Reject(() => ClearingPixelArt.RasterPath(width, height));
            }
            foreach (var size in new[] { new[] { 1, 1 }, new[] { 2048, 1 }, new[] { 1, 2048 } })
                foreach (var art in new[] { ClearingPixelArt.RasterGround(size[0], size[1]),
                    ClearingPixelArt.RasterWall(size[0], size[1]), ClearingPixelArt.RasterPath(size[0], size[1]) })
                    Expect(art.Width == size[0] && art.Height == size[1], "Valid dimension boundary was rejected or changed");
        });
        if (args.Length > 0) Export(args[0]);
        Console.WriteLine("ART_CHECK_RESULT passed=" + passed + " failed=" + failed + "; actual managed Color[] source, not Unity rendering or art acceptance.");
        return failed == 0 ? 0 : 1;
    }
    static void Export(string directory)
    {
        Directory.CreateDirectory(directory); var lines = new List<string>(); var art = Build();
        for (int i = 0; i < art.Length; i++) {
            var rgba = new byte[art[i].Pixels.Length * 4];
            for (int j = 0; j < art[i].Pixels.Length; j++) {
                Color c = art[i].Pixels[j]; rgba[j * 4] = (byte)Byte(c.r); rgba[j * 4 + 1] = (byte)Byte(c.g);
                rgba[j * 4 + 2] = (byte)Byte(c.b); rgba[j * 4 + 3] = (byte)Byte(c.a);
            }
            File.WriteAllBytes(Path.Combine(directory, Names[i] + ".rgba"), rgba);
            lines.Add(Names[i] + "\t" + art[i].Width + "\t" + art[i].Height);
        }
        File.WriteAllLines(Path.Combine(directory, "rasters.tsv"), lines.ToArray());
    }
}
