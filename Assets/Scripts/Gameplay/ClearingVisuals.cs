using System.Collections.Generic;
using UnityEngine;

namespace Rpg.Gameplay
{
    // Original code-native raster art at the established 30-pixel world unit scale.
    // Pixel structure is deterministic; native playback and art acceptance remain separate.
    internal sealed class ClearingVisuals
    {
        private readonly Transform root;
        private readonly Sprite pixel;
        private readonly Texture2D texture;
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private readonly List<Texture2D> textures = new List<Texture2D>();
        internal GameObject Gate { get; private set; }
        internal SpriteRenderer Beacon { get; private set; }
        internal static readonly Vector2 BeaconPosition = new Vector2(0f, 3.35f);
        internal static readonly Vector2 PlayerSpawn = new Vector2(0f, -2.65f);
        // Existing centered 35x68/30 PPU Viola frames put the boots about 1.1 units below the body.
        internal static readonly Vector2 PlayerFootOffset = new Vector2(0f, -1.1f);

        internal ClearingVisuals(Transform root)
        {
            this.root = root;
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Clearing shared point pixel";
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f, 0, SpriteMeshType.FullRect);
            pixel.name = "Clearing shared square";
        }

        internal SpriteRenderer Block(string name, Transform parent, Vector2 position,
            Vector2 size, Color color, int order = -10, bool actorLayer = false)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(position.x, position.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = color;
            renderer.sortingLayerName = actorLayer ? "Player" : "Default";
            renderer.sortingOrder = order;
            return renderer;
        }

        private SpriteRenderer Art(string name, Transform parent, Vector2 position, string key,
            System.Func<ClearingRaster> build, Vector2 pivot, int order, bool actorLayer = false)
        {
            Sprite sprite;
            if (!sprites.TryGetValue(key, out sprite))
            {
                ClearingRaster raster = build();
                Texture2D image = new Texture2D(raster.Width, raster.Height, TextureFormat.RGBA32, false);
                image.name = "Clearing raster " + key;
                image.filterMode = FilterMode.Point;
                image.wrapMode = TextureWrapMode.Clamp;
                image.SetPixels(raster.Pixels);
                // FullRect avoids implicit tight-mesh/outline work; actor colliders are separate.
                // Create while pixels remain readable, then upload and release the CPU copy.
                sprite = Sprite.Create(image, new Rect(0, 0, raster.Width, raster.Height), pivot, 30f, 0, SpriteMeshType.FullRect);
                image.Apply(false, true);
                sprite.name = "Clearing sprite " + key;
                textures.Add(image);
                sprites.Add(key, sprite);
            }
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(position.x, position.y, 0f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = actorLayer ? "Player" : "Default";
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Wall(string name, Vector2 position, Vector2 size)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = position;
            item.AddComponent<BoxCollider2D>().size = size;
            int width = Mathf.RoundToInt(size.x * 30f);
            int height = Mathf.RoundToInt(size.y * 30f);
            Art("Chipped stone courses", item.transform, Vector2.zero, "wall-" + width + "x" + height,
                () => ClearingPixelArt.RasterWall(width, height), new Vector2(.5f, .5f), -10);
        }

        internal void BuildWorld()
        {
            Art("Moss ground", root, Vector2.zero, "ground", () => ClearingPixelArt.RasterGround(438, 258), new Vector2(.5f, .5f), -30);
            Art("South stepping stones", root, new Vector2(0, -2.75f), "south-path", () => ClearingPixelArt.RasterPath(48, 66), new Vector2(.5f, .5f), -29);
            Art("Beacon stepping stones", root, new Vector2(0, 2.95f), "north-path", () => ClearingPixelArt.RasterPath(48, 69), new Vector2(.5f, .5f), -29);
            // Corners overlap: there is no route around the gate partition or outer bounds.
            Wall("West boundary", new Vector2(-7.2f, 0), new Vector2(.5f, 9.1f));
            Wall("East boundary", new Vector2(7.2f, 0), new Vector2(.5f, 9.1f));
            Wall("South boundary", new Vector2(0, -4.3f), new Vector2(14.9f, .5f));
            Wall("North boundary", new Vector2(0, 4.3f), new Vector2(14.9f, .5f));
            Wall("Beacon partition west", new Vector2(-4.5f, 2.3f), new Vector2(6.1f, .4f));
            Wall("Beacon partition east", new Vector2(4.5f, 2.3f), new Vector2(6.1f, .4f));
            Gate = new GameObject("Objective gate - opens after all three sentinels");
            Gate.transform.SetParent(root, false);
            Gate.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            Gate.AddComponent<BoxCollider2D>().size = new Vector2(3.1f, .4f);
            for (int i = 0; i < 9; i++)
                Art("Seal rune " + i, Gate.transform, new Vector2(-1.4f + i * .35f, 0f), "rune",
                    ClearingPixelArt.RasterRune, new Vector2(.5f, .5f), -8);
            Block("Seal rail", Gate.transform, Vector2.zero, new Vector2(3.1f, .09f),
                ClearingPalette.WithAlpha(ClearingPalette.AmberBright, .7f), -7);
            Art("Beacon carved plinth", root, BeaconPosition + new Vector2(0, -.14f), "plinth",
                ClearingPixelArt.RasterPlinth, new Vector2(.5f, .5f), -6);
            Beacon = Art("Beacon faceted crystal", root, BeaconPosition + new Vector2(0, .2f), "crystal",
                ClearingPixelArt.RasterCrystal, new Vector2(.5f, 0f), -5);
        }

        internal GameObject CreateSentinel(int id, Vector2 position, out SpriteRenderer eye,
            out SpriteRenderer healthFill, out Transform warning)
        {
            SentinelAttackKind kind = SentinelTactics.RoleForIndex(id);
            GameObject enemy = new GameObject("Moss sentinel " + (id + 1));
            enemy.transform.SetParent(root, false);
            enemy.transform.position = position;
            enemy.layer = 2; // Physical collisions remain enabled; wall raycasts use Default only.
            Block("Sentinel grounded shadow", enemy.transform, new Vector2(0, .017f), new Vector2(.9f, .133f),
                ClearingPalette.WithAlpha(ClearingPalette.Ink, .75f), -4);
            Art("Chipped moss sentinel", enemy.transform, Vector2.zero, "sentinel",
                ClearingPixelArt.RasterSentinel, new Vector2(.5f, 0f), 2, true);
            CreateRoleMarker(enemy.transform, kind);
            Color readyColor = kind == SentinelAttackKind.Sigil ? ClearingPalette.CyanBright : ClearingPalette.AmberBright;
            eye = Block("Warning eye", enemy.transform, new Vector2(0, 1.08f), new Vector2(.267f, .067f),
                readyColor, 5, true);
            Block("Health background", enemy.transform, new Vector2(0, 1.5f), new Vector2(.72f, .067f),
                ClearingPalette.Ink, 6, true);
            healthFill = Block("Health", enemy.transform, new Vector2(0, 1.5f), new Vector2(.72f, .067f),
                ClearingPalette.Amber, 7, true);
            GameObject mark = new GameObject("Attack warning footprint");
            mark.transform.SetParent(enemy.transform, false);
            warning = mark.transform;
            // Runtime positions/rotates this unit-scale root using the same locked
            // footprint as contact checks. Stroke thickness stays constant for every role.
            SentinelFootprint footprint = SentinelTactics.CreateFootprint(kind, 0f, 0f, 1f, 0f);
            Color warningColor = ClearingPalette.WithAlpha(readyColor, .85f);
            Block("North warning", warning, new Vector2(0, footprint.Height * .5f), new Vector2(footprint.Width, .067f), warningColor, -2);
            Block("South warning", warning, new Vector2(0, -footprint.Height * .5f), new Vector2(footprint.Width, .067f), warningColor, -2);
            Block("West warning", warning, new Vector2(-footprint.Width * .5f, 0), new Vector2(.067f, footprint.Height), warningColor, -2);
            Block("East warning", warning, new Vector2(footprint.Width * .5f, 0), new Vector2(.067f, footprint.Height), warningColor, -2);
            mark.SetActive(false);
            return enemy;
        }

        private void CreateRoleMarker(Transform actor, SentinelAttackKind kind)
        {
            // Original shared raster stays intact. Cached, pixel-sized silhouettes
            // identify tactics without relying on hue and stay below the health bar.
            const float unit = 1f / 30f;
            if (kind == SentinelAttackKind.Sweep)
            {
                Block("Warden crest", actor, new Vector2(-18f * unit, 23f * unit), new Vector2(10f * unit, 13f * unit),
                    ClearingPalette.Ink, 4, true);
                Block("Warden shield face", actor, new Vector2(-18f * unit, 23f * unit), new Vector2(6f * unit, 9f * unit),
                    ClearingPalette.StoneLight, 5, true);
                Block("Warden crest upright", actor, new Vector2(-18f * unit, 23f * unit), new Vector2(2f * unit, 6f * unit),
                    ClearingPalette.AmberBright, 6, true);
                Block("Warden crest crossbar", actor, new Vector2(-18f * unit, 23f * unit), new Vector2(6f * unit, 2f * unit),
                    ClearingPalette.AmberBright, 6, true);
            }
            else if (kind == SentinelAttackKind.Lance)
            {
                Block("Lancer side spear", actor, new Vector2(20f * unit, 20f * unit), new Vector2(2f * unit, 34f * unit),
                    ClearingPalette.Ink, 4, true);
                Block("Lancer spear shaft", actor, new Vector2(20f * unit, 20f * unit), new Vector2(unit, 32f * unit),
                    ClearingPalette.StoneLight, 5, true);
                Block("Lancer spear shoulder", actor, new Vector2(20f * unit, 37f * unit), new Vector2(6f * unit, 2f * unit),
                    ClearingPalette.AmberBright, 5, true);
                Block("Lancer spear point", actor, new Vector2(20f * unit, 39f * unit), new Vector2(2f * unit, 4f * unit),
                    ClearingPalette.Cream, 5, true);
            }
            else
            {
                Block("Seer rune", actor, new Vector2(-20f * unit, 35f * unit), new Vector2(2f * unit, 2f * unit),
                    ClearingPalette.CyanBright, 5, true);
                Block("Seer rune base", actor, new Vector2(-20f * unit, 23f * unit), new Vector2(2f * unit, 2f * unit),
                    ClearingPalette.CyanBright, 5, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    Block("Seer rune upper " + side, actor, new Vector2((-20f + side * 2f) * unit, 33f * unit),
                        new Vector2(2f * unit, 2f * unit), ClearingPalette.CyanBright, 5, true);
                    Block("Seer rune lower " + side, actor, new Vector2((-20f + side * 2f) * unit, 25f * unit),
                        new Vector2(2f * unit, 2f * unit), ClearingPalette.CyanBright, 5, true);
                    Block("Seer rune side " + side, actor, new Vector2((-20f + side * 4f) * unit, 29f * unit),
                        new Vector2(2f * unit, 6f * unit), ClearingPalette.CyanBright, 5, true);
                }
            }
        }

        internal void Dispose()
        {
            foreach (Sprite sprite in sprites.Values) Object.Destroy(sprite);
            foreach (Texture2D image in textures) Object.Destroy(image);
            sprites.Clear();
            textures.Clear();
            Object.Destroy(pixel);
            Object.Destroy(texture);
        }
    }
}
