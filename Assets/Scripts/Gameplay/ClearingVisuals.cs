using UnityEngine;

namespace Rpg.Gameplay
{
    // Code-native blockout art at the existing hero's 30-pixel unit scale.
    // These shapes are gameplay-readable placeholders, not final character art.
    internal sealed class ClearingVisuals
    {
        private readonly Transform root;
        private readonly Sprite pixel;
        private readonly Texture2D texture;
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
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
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

        private void Wall(string name, Vector2 position, Vector2 size)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = position;
            item.AddComponent<BoxCollider2D>().size = size;
            Block("Stone", item.transform, Vector2.zero, size, new Color(.22f, .29f, .29f));
            Block("Lit edge", item.transform, new Vector2(0, size.y / 2f - .06f),
                new Vector2(size.x, .12f), new Color(.4f, .46f, .4f), -9);
        }

        internal void BuildWorld()
        {
            Block("Moss ground", root, Vector2.zero, new Vector2(14.6f, 8.6f), new Color(.12f, .21f, .18f), -30);
            Block("South path", root, new Vector2(0, -2.75f), new Vector2(1.6f, 2.2f), new Color(.29f, .3f, .22f), -29);
            Block("Beacon path", root, new Vector2(0, 2.95f), new Vector2(1.6f, 2.3f), new Color(.29f, .3f, .22f), -29);
            // Deterministic sparse ground detail, leaving the central combat silhouettes clear.
            for (int i = 0; i < 64; i++)
            {
                float x = ((i * 37) % 390 - 195) / 30f;
                float y = ((i * 53) % 210 - 105) / 30f;
                if (Mathf.Abs(x) < 1f || y > 2f) continue;
                Block("Grass tuft " + i, root, new Vector2(x, y), new Vector2(.13f, .067f),
                    new Color(.19f, .29f, .22f), -28);
            }
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
                Block("Seal bar " + i, Gate.transform, new Vector2(-1.4f + i * .35f, 0f),
                    new Vector2(.12f, .45f), new Color(.77f, .51f, .28f), -8);
            Block("Seal rail", Gate.transform, Vector2.zero, new Vector2(3.1f, .09f),
                new Color(.9f, .69f, .34f), -7);
            Block("Beacon plinth", root, BeaconPosition + new Vector2(0, -.14f), new Vector2(.8f, .35f),
                new Color(.4f, .48f, .4f), -6);
            Beacon = Block("Beacon crystal", root, BeaconPosition + new Vector2(0, .2f), new Vector2(.36f, .36f),
                new Color(.45f, .47f, .4f), -5);
            Beacon.transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        internal GameObject CreateSentinel(int id, Vector2 position, out SpriteRenderer eye,
            out SpriteRenderer healthFill, out Transform warning)
        {
            GameObject enemy = new GameObject("Moss sentinel " + (id + 1));
            enemy.transform.SetParent(root, false);
            enemy.transform.position = position;
            enemy.layer = 2; // Physical collisions remain enabled; wall raycasts use Default only.
            Color stone = new Color(.41f, .48f, .43f);
            Color moss = new Color(.28f, .41f, .3f);
            Block("Feet", enemy.transform, new Vector2(0, .067f), new Vector2(.7f, .13f), stone, 1, true);
            Block("Body", enemy.transform, new Vector2(0, .45f), new Vector2(.6f, .8f), stone, 2, true);
            Block("Shoulders", enemy.transform, new Vector2(0, .75f), new Vector2(1f, .25f), moss, 3, true);
            Block("Head", enemy.transform, new Vector2(0, 1.04f), new Vector2(.5f, .5f), moss, 4, true);
            eye = Block("Warning eye", enemy.transform, new Vector2(0, 1.08f), new Vector2(.267f, .067f),
                new Color(.98f, .79f, .39f), 5, true);
            Block("Health background", enemy.transform, new Vector2(0, 1.5f), new Vector2(.72f, .067f),
                new Color(.14f, .13f, .13f), 6, true);
            healthFill = Block("Health", enemy.transform, new Vector2(0, 1.5f), new Vector2(.72f, .067f),
                new Color(.76f, .48f, .32f), 7, true);
            GameObject mark = new GameObject("Attack warning footprint");
            mark.transform.SetParent(enemy.transform, false);
            warning = mark.transform;
            Color warningColor = new Color(1f, .49f, .23f, .8f);
            Block("North warning", warning, new Vector2(0, .95f), new Vector2(1.9f, .067f), warningColor, -2);
            Block("South warning", warning, new Vector2(0, -.95f), new Vector2(1.9f, .067f), warningColor, -2);
            Block("West warning", warning, new Vector2(-.95f, 0), new Vector2(.067f, 1.9f), warningColor, -2);
            Block("East warning", warning, new Vector2(.95f, 0), new Vector2(.067f, 1.9f), warningColor, -2);
            mark.SetActive(false);
            return enemy;
        }

        internal void Dispose()
        {
            Object.Destroy(pixel);
            Object.Destroy(texture);
        }
    }
}
