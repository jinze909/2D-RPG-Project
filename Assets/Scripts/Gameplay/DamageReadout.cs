using System;
using System.Globalization;

namespace Rpg.Gameplay
{
    // Presentation only: the caller supplies accepted HP loss, never nominal damage.
    internal static class DamageReadout
    {
        internal const string Symbols = "0123456789-.<+";
        internal const int MaximumGlyphs = 7;
        internal const double Duration = .6d;
        internal const float Rise = .35f;

        internal static string FormatLoss(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return null;
            if (amount < .01f) return "-<0.01";
            if (amount > 999f) return "-999+";
            // Integer combat values remain exact; other losses round to two decimals.
            return "-" + amount.ToString("0.##", CultureInfo.InvariantCulture);
        }

        internal static double Progress(double now, double startedAt)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || double.IsNaN(startedAt) || double.IsInfinity(startedAt)) return 1d;
            return Math.Max(0d, Math.Min(1d, (now - startedAt) / Duration));
        }

        // Three columns, five rows; bit zero is the bottom-left foreground pixel.
        internal static ushort Glyph(char symbol)
        {
            switch (symbol)
            {
                case '0': return 31599;
                case '1': return 9879;
                case '2': return 31183;
                case '3': return 31207;
                case '4': return 23524;
                case '5': return 29671;
                case '6': return 29679;
                case '7': return 30866;
                case '8': return 31727;
                case '9': return 31719;
                case '-': return 448;
                case '.': return 2;
                case '<': return 17492;
                case '+': return 1488;
                default: throw new ArgumentOutOfRangeException("symbol");
            }
        }
    }
}
