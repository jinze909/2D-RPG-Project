using System;
using UnityEngine;

namespace Rpg.Gameplay
{
    /// <summary>Bounded world-space damage feedback; gameplay remains its caller's responsibility.</summary>
    internal sealed class ClearingDamageNumbers : IDisposable
    {
        private sealed class Slot
        {
            internal Transform Root;
            internal SpriteRenderer[] Renderers;
            internal string Text;
            internal float Amount;
            internal double StartedAt;
            internal Vector2 Origin;
            internal Color Color;
        }

        private readonly Slot[] slots = new Slot[4];
        private readonly Sprite[] glyphSprites = new Sprite[DamageReadout.Symbols.Length];
        private readonly Texture2D[] glyphTextures = new Texture2D[DamageReadout.Symbols.Length];
        private bool disposed;

        internal ClearingDamageNumbers(Transform root)
        {
            if (root == null) throw new ArgumentNullException("root");
            for (int i = 0; i < glyphSprites.Length; i++) CreateGlyph(i);
            for (int i = 0; i < slots.Length; i++)
            {
                var item = new GameObject("Damage number " + i);
                item.transform.SetParent(root, false);
                var slot = new Slot { Root = item.transform, Renderers = new SpriteRenderer[DamageReadout.MaximumGlyphs] };
                slots[i] = slot;
                for (int j = 0; j < slot.Renderers.Length; j++)
                {
                    var glyph = new GameObject("Damage glyph " + j);
                    glyph.transform.SetParent(item.transform, false);
                    var renderer = glyph.AddComponent<SpriteRenderer>();
                    renderer.sortingLayerName = "Player";
                    renderer.sortingOrder = 30004;
                    slot.Renderers[j] = renderer;
                }
                item.SetActive(false);
            }
        }

        private void CreateGlyph(int index)
        {
            ushort mask = DamageReadout.Glyph(DamageReadout.Symbols[index]);
            var pixels = new Color[5 * 7];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 3; x++)
                    if ((mask & (1 << (y * 3 + x))) != 0)
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                                pixels[(y + 1 + dy) * 5 + x + 1 + dx] = ClearingPalette.Ink;
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 3; x++)
                    if ((mask & (1 << (y * 3 + x))) != 0)
                        pixels[(y + 1) * 5 + x + 1] = ClearingPalette.Cream;
            var texture = new Texture2D(5, 7, TextureFormat.RGBA32, false);
            texture.name = "Damage glyph " + DamageReadout.Symbols[index];
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            glyphTextures[index] = texture;
            glyphSprites[index] = Sprite.Create(texture, new Rect(0, 0, 5, 7),
                new Vector2(.5f, .5f), 30f, 0, SpriteMeshType.FullRect);
            texture.Apply(false, true);
        }

        internal void Show(int slotIndex, float actualLoss, Vector2 point, Color color, double now)
        {
            if (disposed || slotIndex < 0 || slotIndex >= slots.Length || !Finite(now)
                || !Finite(point.x) || !Finite(point.y) || !Finite(color.r) || !Finite(color.g)
                || !Finite(color.b) || !Finite(color.a)) return;
            string text = DamageReadout.FormatLoss(actualLoss);
            if (text == null) return;
            Slot slot = slots[slotIndex];
            slot.Text = text;
            slot.Amount = actualLoss;
            slot.StartedAt = now;
            slot.Origin = point;
            slot.Color = color;
            slot.Root.position = new Vector3(point.x, point.y, 0f);
            for (int i = 0; i < slot.Renderers.Length; i++)
            {
                SpriteRenderer renderer = slot.Renderers[i];
                renderer.gameObject.SetActive(i < text.Length);
                if (i >= text.Length) continue;
                renderer.sprite = glyphSprites[DamageReadout.Symbols.IndexOf(text[i])];
                renderer.color = color;
                renderer.transform.localPosition = new Vector3((i - (text.Length - 1) * .5f) * 4f / 30f, 0f, 0f);
            }
            slot.Root.gameObject.SetActive(true);
        }

        internal void Refresh(double now)
        {
            if (disposed) return;
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (slot.Text == null) continue;
                float progress = (float)DamageReadout.Progress(now, slot.StartedAt);
                if (now >= slot.StartedAt + DamageReadout.Duration || progress >= 1f)
                {
                    slot.Root.gameObject.SetActive(false);
                    slot.Text = null;
                    continue;
                }
                slot.Root.position = new Vector3(slot.Origin.x, slot.Origin.y + DamageReadout.Rise * progress, 0f);
                Color color = slot.Color;
                color.a *= 1f - progress * progress * (3f - 2f * progress);
                for (int j = 0; j < slot.Renderers.Length; j++) slot.Renderers[j].color = color;
            }
        }

        internal void Clear()
        {
            if (disposed) return;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Root.gameObject.SetActive(false);
                slots[i].Text = null;
                slots[i].Amount = 0f;
                slots[i].StartedAt = 0d;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            for (int i = 0; i < slots.Length; i++) UnityEngine.Object.Destroy(slots[i].Root.gameObject);
            for (int i = 0; i < glyphSprites.Length; i++)
            {
                UnityEngine.Object.Destroy(glyphSprites[i]);
                UnityEngine.Object.Destroy(glyphTextures[i]);
            }
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
