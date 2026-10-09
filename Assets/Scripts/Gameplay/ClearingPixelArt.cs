using UnityEngine;

namespace Rpg.Gameplay
{
    // Pure managed raster builders. The production pixels can be checked without
    // native rendering; upload/filtering and visual acceptance remain Unity checks.
    internal sealed class ClearingRaster
    {
        internal const int MaximumDimension = 2048;
        internal int Width { get; private set; }
        internal int Height { get; private set; }
        internal Color[] Pixels { get; private set; }
        internal ClearingRaster(int width, int height)
        {
            if (width <= 0 || width > MaximumDimension) throw new System.ArgumentOutOfRangeException("width");
            if (height <= 0 || height > MaximumDimension) throw new System.ArgumentOutOfRangeException("height");
            Width = width;
            Height = height;
            Pixels = new Color[checked(width * height)];
        }
        internal void Pixel(int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height) Pixels[y * Width + x] = color;
        }
        internal void Rect(int x, int y, int width, int height, Color color)
        {
            for (int yy = y; yy < y + height; yy++)
                for (int xx = x; xx < x + width; xx++) Pixel(xx, yy, color);
        }
    }

    internal static class ClearingPixelArt
    {
        private static uint Hash(int x, int y)
        {
            unchecked
            {
                uint value = (uint)(x * 374761393 + y * 668265263);
                value = (value ^ (value >> 13)) * 1274126177u;
                return value ^ (value >> 16);
            }
        }

        internal static ClearingRaster RasterGround(int width, int height)
        {
            var art = new ClearingRaster(width, height);
            art.Rect(0, 0, width, height, ClearingPalette.Ground);
            for (int y = 2; y < height - 2; y++)
                for (int x = 2; x < width - 3; x++)
                {
                    uint hash = Hash(x, y);
                    if (hash % 173 == 0)
                    {
                        art.Rect(x, y, 3, 1, ClearingPalette.GroundLight);
                        art.Pixel(x + 1, y + 1, ClearingPalette.Moss);
                    }
                    else if (hash % 227 == 0) art.Rect(x, y, 2, 1, ClearingPalette.Forest);
                }
            return art;
        }

        internal static ClearingRaster RasterPath(int width, int height)
        {
            var art = new ClearingRaster(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int edge = (int)(Hash(0, y / 5) % 3);
                    if (x < edge || x >= width - edge) continue;
                    bool seam = (y + 2) % 12 == 0 || (x + ((y / 12) % 2) * 12) % 24 == 0;
                    Color color = seam ? ClearingPalette.Forest : ClearingPalette.StoneShadow;
                    if (!seam && Hash(x / 3, y / 3) % 7 == 0) color = ClearingPalette.Stone;
                    if (x == edge || x == width - edge - 1) color = ClearingPalette.Moss;
                    art.Pixel(x, y, color);
                }
            return art;
        }

        internal static ClearingRaster RasterWall(int width, int height)
        {
            var art = new ClearingRaster(width, height);
            art.Rect(0, 0, width, height, ClearingPalette.Ink);
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    int row = y / 7;
                    int brickX = (x + (row % 2) * 7) % 14;
                    if (y % 7 == 0 || brickX == 0) continue;
                    Color color = ClearingPalette.StoneShadow;
                    if (y % 7 >= 4) color = ClearingPalette.Stone;
                    if (y % 7 == 6 || brickX == 1) color = ClearingPalette.StoneLight;
                    if (Hash(x / 14, row) % 5 == 0 && y % 7 > 4) color = ClearingPalette.Moss;
                    art.Pixel(x, y, color);
                }
            art.Rect(1, height - 2, width - 2, 1, ClearingPalette.StoneLight);
            return art;
        }

        internal static ClearingRaster RasterSentinel()
        {
            var art = new ClearingRaster(34, 42);
            // Separated boots and stepped fists keep the silhouette legible at 1x.
            art.Rect(7, 0, 8, 5, ClearingPalette.Ink);
            art.Rect(20, 0, 8, 5, ClearingPalette.Ink);
            art.Rect(8, 1, 6, 3, ClearingPalette.Stone);
            art.Rect(21, 1, 6, 3, ClearingPalette.StoneShadow);
            art.Rect(9, 4, 5, 5, ClearingPalette.StoneLight);
            art.Rect(21, 4, 5, 5, ClearingPalette.Stone);
            art.Rect(7, 8, 21, 20, ClearingPalette.Ink);
            art.Rect(9, 9, 17, 18, ClearingPalette.Stone);
            art.Rect(22, 9, 4, 18, ClearingPalette.StoneShadow);
            art.Rect(9, 22, 12, 4, ClearingPalette.StoneLight);
            art.Rect(10, 12, 2, 9, ClearingPalette.StoneLight);
            art.Rect(16, 9, 1, 10, ClearingPalette.StoneShadow);
            art.Rect(17, 18, 6, 1, ClearingPalette.Ink);
            art.Pixel(23, 17, ClearingPalette.Ink);
            art.Rect(2, 13, 7, 15, ClearingPalette.Ink);
            art.Rect(3, 14, 5, 12, ClearingPalette.Stone);
            art.Rect(3, 24, 5, 2, ClearingPalette.StoneLight);
            art.Rect(27, 11, 5, 16, ClearingPalette.Ink);
            art.Rect(28, 12, 3, 13, ClearingPalette.StoneShadow);
            art.Rect(1, 26, 11, 5, ClearingPalette.Ink);
            art.Rect(2, 27, 9, 3, ClearingPalette.Moss);
            art.Rect(24, 25, 9, 5, ClearingPalette.Ink);
            art.Rect(25, 26, 7, 3, ClearingPalette.Moss);
            art.Rect(10, 28, 15, 12, ClearingPalette.Ink);
            art.Rect(11, 29, 13, 10, ClearingPalette.Stone);
            art.Rect(11, 35, 12, 3, ClearingPalette.StoneLight);
            art.Rect(21, 30, 3, 5, ClearingPalette.StoneShadow);
            art.Rect(11, 31, 13, 3, ClearingPalette.Ink);
            art.Rect(12, 32, 11, 1, ClearingPalette.Amber);
            art.Rect(11, 38, 13, 3, ClearingPalette.Moss);
            art.Rect(13, 40, 4, 2, ClearingPalette.MossLight);
            art.Rect(20, 39, 3, 2, ClearingPalette.MossLight);
            art.Rect(3, 28, 3, 3, ClearingPalette.MossLight);
            art.Rect(25, 27, 4, 3, ClearingPalette.MossLight);
            // The chest carries a modest amber rune rather than background noise.
            art.Rect(15, 19, 5, 3, ClearingPalette.StoneShadow);
            art.Rect(17, 19, 1, 3, ClearingPalette.Amber);
            art.Pixel(16, 20, ClearingPalette.Amber);
            art.Pixel(18, 20, ClearingPalette.Amber);
            return art;
        }

        internal static ClearingRaster RasterPlinth()
        {
            var art = new ClearingRaster(34, 14);
            art.Rect(1, 1, 32, 6, ClearingPalette.Ink);
            art.Rect(3, 2, 28, 5, ClearingPalette.StoneShadow);
            art.Rect(3, 7, 28, 5, ClearingPalette.Stone);
            art.Rect(5, 11, 24, 2, ClearingPalette.StoneLight);
            art.Rect(5, 5, 24, 1, ClearingPalette.Ink);
            art.Rect(10, 8, 14, 1, ClearingPalette.Cyan);
            art.Pixel(9, 9, ClearingPalette.Cyan);
            art.Pixel(24, 9, ClearingPalette.Cyan);
            return art;
        }

        internal static ClearingRaster RasterCrystal()
        {
            var art = new ClearingRaster(16, 26);
            for (int y = 1; y < 25; y++)
            {
                int half = y < 12 ? 1 + y / 2 : 1 + (24 - y) / 2;
                int left = 7 - half;
                int right = 8 + half;
                for (int x = left; x <= right; x++)
                {
                    Color color = x == left || x == right ? ClearingPalette.Ink :
                        x < 8 ? ClearingPalette.CyanBright : ClearingPalette.Cyan;
                    if (x == 7 && y > 8) color = ClearingPalette.Cream;
                    art.Pixel(x, y, color);
                }
            }
            return art;
        }

        internal static ClearingRaster RasterRune()
        {
            var art = new ClearingRaster(11, 13);
            art.Rect(1, 1, 9, 11, ClearingPalette.Ink);
            art.Rect(2, 2, 7, 9, ClearingPalette.StoneShadow);
            art.Rect(5, 3, 1, 7, ClearingPalette.AmberBright);
            art.Rect(3, 5, 5, 1, ClearingPalette.Amber);
            art.Rect(3, 8, 5, 1, ClearingPalette.Amber);
            art.Rect(2, 11, 7, 1, ClearingPalette.StoneLight);
            return art;
        }
    }
}
