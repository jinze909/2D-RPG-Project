using System;
using System.Globalization;
using Rpg.Gameplay;

internal static class DamageReadoutChecks
{
    private static int passed, failed;
    private static void Require(bool value) { if (!value) throw new Exception("contract failed"); }
    private static void Test(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    private static int Main()
    {
        Test("current combat losses include exact capped lethal damage", () => {
            foreach (int value in new[] { 1, 2, 6, 18, 24, 30, 54, 999 }) Require(DamageReadout.FormatLoss(value) == "-" + value);
        });
        Test("zero negative and nonfinite losses do not produce false feedback", () => {
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Require(DamageReadout.FormatLoss(value) == null);
        });
        Test("fractional and very small losses have bounded truthful decimal markers", () => {
            Require(DamageReadout.FormatLoss(.5f) == "-0.5"); Require(DamageReadout.FormatLoss(1.25f) == "-1.25");
            Require(DamageReadout.FormatLoss(.333f) == "-0.33"); Require(DamageReadout.FormatLoss(.001f) == "-<0.01");
            Require(DamageReadout.FormatLoss(float.Epsilon) == "-<0.01");
        });
        Test("large losses are bounded rather than overflowing a pooled label", () => {
            Require(DamageReadout.FormatLoss(1000f) == "-999+"); Require(DamageReadout.FormatLoss(float.MaxValue) == "-999+");
        });
        Test("decimal display remains invariant under comma cultures", () => {
            var before = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); Require(DamageReadout.FormatLoss(1.25f) == "-1.25"); }
            finally { CultureInfo.CurrentCulture = before; }
        });
        Test("every admitted loss uses supported glyphs within the fixed slot capacity", () => {
            for (int i = 1; i <= 10000; i++) {
                string text = DamageReadout.FormatLoss(i * .12345f); Require(text.Length <= DamageReadout.MaximumGlyphs && text[0] == '-');
                foreach (char c in text) Require(DamageReadout.Glyph(c) != 0);
            }
        });
        Test("glyph silhouettes are distinct and fit three by five foreground pixels", () => {
            var masks = new System.Collections.Generic.HashSet<ushort>();
            foreach (char c in DamageReadout.Symbols) { ushort m = DamageReadout.Glyph(c); Require(m > 0 && m < (1 << 15) && masks.Add(m)); }
            Require(DamageReadout.Glyph('.') == 2); Require(DamageReadout.Glyph('-') == (7 << 6));
            try { DamageReadout.Glyph('X'); throw new Exception("invalid accepted"); } catch (ArgumentOutOfRangeException) { }
        });
        Test("simulation lifetime clamps before start and expires exactly at deadline", () => {
            Require(DamageReadout.Progress(9d, 10d) == 0d); Require(DamageReadout.Progress(10d, 10d) == 0d);
            Require(Math.Abs(DamageReadout.Progress(10.3d, 10d) - .5d) < .000001d);
            Require(DamageReadout.Progress(10.6d, 10d) > .999999d); Require(DamageReadout.Progress(100d, 10d) == 1d);
        });
        Test("invalid presentation clocks expire rather than retain stale effects", () => {
            Require(DamageReadout.Progress(double.NaN, 0d) == 1d); Require(DamageReadout.Progress(1d, double.PositiveInfinity) == 1d);
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; actual pure presentation rules, no Unity engine.");
        return failed == 0 ? 0 : 1;
    }
}
