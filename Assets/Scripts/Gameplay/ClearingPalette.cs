using UnityEngine;

namespace Rpg.Gameplay
{
    /// <summary>Shared limited palette: quiet forest, readable stone, warm danger and cool magic.</summary>
    public static class ClearingPalette
    {
        public static readonly Color Ink = Rgb(20, 31, 32);
        public static readonly Color Forest = Rgb(27, 48, 42);
        public static readonly Color Ground = Rgb(40, 67, 49);
        public static readonly Color GroundLight = Rgb(48, 77, 55);
        public static readonly Color Moss = Rgb(67, 94, 55);
        public static readonly Color MossLight = Rgb(106, 133, 73);
        public static readonly Color StoneShadow = Rgb(58, 75, 74);
        public static readonly Color Stone = Rgb(93, 111, 101);
        public static readonly Color StoneLight = Rgb(147, 160, 134);
        public static readonly Color Cream = Rgb(240, 231, 193);
        public static readonly Color Amber = Rgb(211, 139, 62);
        public static readonly Color AmberBright = Rgb(255, 204, 104);
        public static readonly Color Cyan = Rgb(74, 162, 164);
        public static readonly Color CyanBright = Rgb(155, 233, 214);
        public static readonly Color Danger = Rgb(226, 92, 59);

        private static Color Rgb(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
        public static Color WithAlpha(Color color, float alpha) { color.a = Mathf.Clamp01(alpha); return color; }
    }
}
