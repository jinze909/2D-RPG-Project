using System;
using UnityEngine;

namespace Rpg.Gameplay
{
    // Two fixed, nonblocking props reuse the clearing's Point-filtered square.
    // Pixel-sized silhouettes identify health and mana without relying on hue.
    internal sealed class ClearingSupplyVisuals
    {
        internal const double PulseDuration = .45;
        private const float Pixel = 1f / 30f;
        private readonly GameObject[] points = new GameObject[ClearingSupplies.Count];
        private readonly SpriteRenderer[] bases = new SpriteRenderer[ClearingSupplies.Count];
        private readonly SpriteRenderer[][] glyphs = new SpriteRenderer[ClearingSupplies.Count][];
        private readonly SpriteRenderer[] emptyMarks = new SpriteRenderer[ClearingSupplies.Count];
        private readonly SpriteRenderer[][] pulsePieces = new SpriteRenderer[ClearingSupplies.Count][];
        private readonly double[] pulseStart = new double[ClearingSupplies.Count];
        private bool disposed;

        internal ClearingSupplyVisuals(Transform parent, ClearingVisuals visuals)
        {
            for (int index = 0; index < ClearingSupplies.Count; index++)
            {
                points[index] = new GameObject(index == 0 ? "Hidden healing herbs" : "Hidden mana rune");
                points[index].transform.SetParent(parent, false);
                points[index].transform.localPosition = new Vector3(
                    ClearingSupplies.PositionX(index), ClearingSupplies.PositionY(index), 0f);
                Transform point = points[index].transform;
                bases[index] = Part(visuals, "Supply stone tray", point, 0, 5, 18, 14,
                    ClearingPalette.StoneShadow, -21);
                Part(visuals, "Supply tray rim", point, 0, -1, 18, 2, ClearingPalette.Ink, -20);
                glyphs[index] = index == 0 ? Herbs(visuals, point) : Rune(visuals, point);
                emptyMarks[index] = Part(visuals, "Empty supply notch", point, 0, 5, 8, 2,
                    ClearingPalette.Stone, -19);
                emptyMarks[index].gameObject.SetActive(false);
                pulsePieces[index] = new SpriteRenderer[4];
                for (int side = 0; side < pulsePieces[index].Length; side++)
                {
                    pulsePieces[index][side] = Part(visuals, "Supply claim border " + side, point,
                        0, 5, 2, 2, ClearingPalette.Cream, -18);
                    pulsePieces[index][side].gameObject.SetActive(false);
                }
                pulseStart[index] = double.NaN;
                SetState(index, false, false);
            }
        }

        private static SpriteRenderer Part(ClearingVisuals visuals, string name, Transform parent,
            float x, float y, float width, float height, Color color, int order)
        {
            return visuals.Block(name, parent, new Vector2(x * Pixel, y * Pixel),
                new Vector2(width * Pixel, height * Pixel), color, order);
        }

        private static SpriteRenderer[] Herbs(ClearingVisuals visuals, Transform parent)
        {
            return new[]
            {
                Part(visuals, "Herb upright", parent, 0, 5, 2, 10, ClearingPalette.Moss, -19),
                Part(visuals, "Herb cross leaves", parent, 0, 5, 10, 2, ClearingPalette.Moss, -19),
                Part(visuals, "Herb left bud", parent, -4, 9, 2, 2, ClearingPalette.Moss, -19),
                Part(visuals, "Herb right bud", parent, 4, 9, 2, 2, ClearingPalette.Moss, -19),
                Part(visuals, "Herb heart", parent, 0, 5, 2, 2, ClearingPalette.Moss, -18)
            };
        }

        private static SpriteRenderer[] Rune(ClearingVisuals visuals, Transform parent)
        {
            SpriteRenderer[] rune = new SpriteRenderer[8];
            int[,] pixels = { { 0, 11 }, { -2, 9 }, { 2, 9 }, { -4, 6 },
                { 4, 6 }, { -2, 3 }, { 2, 3 }, { 0, 1 } };
            for (int part = 0; part < rune.Length; part++)
                rune[part] = Part(visuals, "Mana diamond " + part, parent,
                    pixels[part, 0], pixels[part, 1], 2,
                    part == 3 || part == 4 ? 4 : 2, ClearingPalette.Stone, -19);
            return rune;
        }

        internal void Refresh(ClearingSupplies supplies, double simulationTime)
        {
            if (disposed || supplies == null) return;
            for (int index = 0; index < ClearingSupplies.Count; index++)
            {
                bool spent = supplies.IsClaimed(index);
                SetState(index, supplies.IsDiscovered(index), spent);
                double age = simulationTime - pulseStart[index];
                bool pulsing = spent && Finite(simulationTime) && Finite(age) &&
                    age >= 0.0 && age < PulseDuration;
                if (!pulsing) pulseStart[index] = double.NaN;
                // Integer-pixel growth preserves the established world-pixel scale.
                float radius = 11f + (pulsing ? (float)Math.Floor(age / PulseDuration * 4.0) : 0f);
                SpriteRenderer[] border = pulsePieces[index];
                for (int side = 0; side < border.Length; side++)
                {
                    border[side].gameObject.SetActive(pulsing);
                    if (!pulsing) continue;
                    bool horizontal = side < 2;
                    float direction = side % 2 == 0 ? 1f : -1f;
                    border[side].transform.localPosition = new Vector3(
                        horizontal ? 0f : radius * direction * Pixel,
                        (horizontal ? 5f + radius * direction : 5f) * Pixel, 0f);
                    border[side].transform.localScale = new Vector3(
                        (horizontal ? radius * 2f : 1f) * Pixel,
                        (horizontal ? 1f : radius * 2f) * Pixel, 1f);
                    border[side].color = age < PulseDuration * .5 ? ClearingPalette.Cream :
                        index == 0 ? ClearingPalette.MossLight : ClearingPalette.CyanBright;
                }
            }
        }

        private void SetState(int index, bool discovered, bool spent)
        {
            bases[index].color = discovered && !spent ? ClearingPalette.Stone : ClearingPalette.StoneShadow;
            emptyMarks[index].gameObject.SetActive(spent);
            Color color = !discovered ? index == 0 ? ClearingPalette.Moss : ClearingPalette.Stone :
                index == 0 ? ClearingPalette.MossLight : ClearingPalette.CyanBright;
            for (int part = 0; part < glyphs[index].Length; part++)
            {
                glyphs[index][part].gameObject.SetActive(!spent);
                glyphs[index][part].color = color;
            }
            if (index == 0 && discovered && !spent)
                glyphs[index][glyphs[index].Length - 1].color = ClearingPalette.Cream;
        }

        internal void ShowClaim(int index, double simulationTime)
        {
            if (disposed || index < 0 || index >= ClearingSupplies.Count ||
                !Finite(simulationTime) || simulationTime < 0.0) return;
            pulseStart[index] = simulationTime;
        }

        internal void Clear()
        {
            if (disposed) return;
            for (int index = 0; index < ClearingSupplies.Count; index++)
            {
                pulseStart[index] = double.NaN;
                for (int side = 0; side < pulsePieces[index].Length; side++)
                    pulsePieces[index][side].gameObject.SetActive(false);
            }
        }

        internal void Dispose()
        {
            if (disposed) return;
            Clear();
            for (int index = 0; index < points.Length; index++) UnityEngine.Object.Destroy(points[index]);
            // Shared sprites and texture belong to ClearingVisuals.
            disposed = true;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
