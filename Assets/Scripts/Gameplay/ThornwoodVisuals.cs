using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rpg.Gameplay
{
    // Cached presentation for the first forest. All blocking geometry comes from
    // ThornwoodLayout; decoration never invents a second collision map. The hero
    // and clearing keep their existing art, animation and shared pixel ownership.
    internal sealed class ThornwoodVisuals
    {
        private const float Pixel = 1f / 30f;
        private readonly ClearingVisuals shared;
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly SpriteRenderer[][] seedGlyphs = new SpriteRenderer[3][];
        private readonly SpriteRenderer[] seedEmpty = new SpriteRenderer[3];
        private readonly SpriteRenderer[] cacheLid = new SpriteRenderer[3];
        private readonly SpriteRenderer cacheSeal;
        private readonly SpriteRenderer cacheOpen;
        private readonly Sprite[] poses;
        private bool disposed;
        private bool worldBuilt;

        internal GameObject Root { get; private set; }
        internal ThornwoodEnemyView[] Enemies { get; private set; }

        internal ThornwoodVisuals(Transform parent, ClearingVisuals art)
        {
            if (art == null) throw new ArgumentNullException("art");
            shared = art;
            Root = new GameObject("Thornwood - dark root forest");
            Root.transform.SetParent(parent, false);
            poses = new Sprite[6];
            for (int pose = 0; pose < poses.Length; pose++)
                poses[pose] = Upload("Briar Stalker pose " + pose, StalkerRaster(pose), new Vector2(.5f, 0f));
            BuildWorld();
            Enemies = new ThornwoodEnemyView[3];
            for (int id = 0; id < Enemies.Length; id++)
                Enemies[id] = new ThornwoodEnemyView(id, Root.transform, shared, poses);
            Transform cache = Point("Thornwood root cache", ThornwoodLayout.Cache);
            Part("Cache rooted base", cache, 0, 3, 32, 12, ClearingPalette.Ink, -18);
            Part("Cache bark face", cache, 0, 4, 28, 10, ClearingPalette.StoneShadow, -17);
            Part("Cache moss face", cache, -4, 6, 20, 6, ClearingPalette.Moss, -16);
            for (int i = 0; i < cacheLid.Length; i++)
                cacheLid[i] = Part("Cache closed lid " + i, cache, 0, 12 + i * 2, 30 - i * 4, 2,
                    i == 2 ? ClearingPalette.MossLight : ClearingPalette.Stone, -16 + i);
            cacheSeal = Part("Cache seed lock", cache, 0, 9, 4, 6, ClearingPalette.Cyan, -12);
            cacheOpen = Part("Cache opened hollow", cache, 0, 13, 22, 6, ClearingPalette.Ink, -11);
            BuildSeeds();
            BuildReturnSign();
            Reset();
            Root.SetActive(false);
        }

        internal void BuildWorld()
        {
            if (disposed || worldBuilt) return;
            worldBuilt = true;
            ThornwoodPoint center = new ThornwoodPoint(ThornwoodLayout.CenterX, ThornwoodLayout.CenterY);
            Renderer("Thornwood forest floor", Root.transform, ToVector(center),
                Upload("Thornwood forest floor", GroundRaster(), new Vector2(.5f, .5f)), -35);
            // Quiet stepping stones guide the loop; they have no collider and sit
            // below all actors, seeds and roots.
            for (int y = -3; y <= 3; y++)
            {
                Part("Root trail center " + y, Root.transform, 660, y * 30, 25, 9,
                    y % 2 == 0 ? ClearingPalette.StoneShadow : ClearingPalette.Ground, -32);
                Part("Root trail worn edge " + y, Root.transform, 663, y * 30 + 3, 13, 1,
                    ClearingPalette.Stone, -31);
            }
            for (int side = -1; side <= 1; side += 2)
                for (int step = 1; step <= 4; step++)
                    Part("Root trail branch " + side + " " + step, Root.transform,
                        660 + side * step * 30, side * 5, 16, 7, ClearingPalette.Ground, -32);
            // The 65-pixel canvas needs an integer-pixel root pivot; .05 would
            // place it at 3.25 pixels and shift otherwise aligned tree pixels.
            Sprite tree = Upload("Thornwood thorn tree", TreeRaster(), new Vector2(.5f, 3f / 65f));
            for (int i = 0; i < ThornwoodLayout.BoundaryCount; i++)
                Boundary(i, ThornwoodLayout.GetBoundary(i));
            for (int i = 0; i < ThornwoodLayout.ObstacleCount; i++)
            {
                ThornwoodRect obstacle = ThornwoodLayout.GetObstacle(i);
                GameObject trunk = new GameObject("Thornwood blocking roots " + i);
                trunk.transform.SetParent(Root.transform, false);
                trunk.transform.position = new Vector3(obstacle.X, obstacle.Y, 0f);
                trunk.layer = 0; // Existing Default-only wall LOS includes these roots.
                trunk.AddComponent<BoxCollider2D>().size = new Vector2(obstacle.Width, obstacle.Height);
                // Tree tops remain on Default, beneath Player, so they cannot
                // hide an actor's feet or the shared attack warning footprint.
                int treeCount = Math.Max(1, Mathf.CeilToInt(obstacle.Height));
                for (int treeIndex = 0; treeIndex < treeCount; treeIndex++)
                    Renderer("Thornwood thorn tree " + i + " " + treeIndex, trunk.transform,
                        new Vector2(treeIndex % 2 == 0 ? -.13f : .13f,
                            -obstacle.Height * .5f + .05f + treeIndex * .9f), tree, -12);
                Part("Thornwood root collider silhouette " + i, trunk.transform, 0, 0,
                    obstacle.Width * 30f, obstacle.Height * 30f, ClearingPalette.Ink, -13);
            }
        }

        private void Boundary(int id, ThornwoodRect rectangle)
        {
            GameObject wall = new GameObject("Thornwood continuous boundary " + id);
            wall.transform.SetParent(Root.transform, false);
            wall.transform.position = new Vector3(rectangle.X, rectangle.Y, 0f);
            wall.layer = 0;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(rectangle.Width, rectangle.Height);
            Part("Thorn hedge shadow " + id, wall.transform, 0, 0,
                rectangle.Width * 30f, rectangle.Height * 30f, ClearingPalette.Ink, -17);
            int count = Mathf.CeilToInt(Mathf.Max(rectangle.Width, rectangle.Height) * 3f);
            bool horizontal = rectangle.Width >= rectangle.Height;
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * .5f) * 10f;
                Part("Thorn hedge branch " + id + " " + i, wall.transform,
                    horizontal ? offset : 0f, horizontal ? 0f : offset,
                    horizontal ? 9f : 5f, horizontal ? 5f : 9f, ClearingPalette.Forest, -16);
                Part("Thorn hedge point " + id + " " + i, wall.transform,
                    horizontal ? offset + 2f : -3f, horizontal ? 3f : offset + 2f,
                    2, 2, ClearingPalette.Moss, -15);
            }
        }

        private void BuildSeeds()
        {
            for (int id = 0; id < seedGlyphs.Length; id++)
            {
                Transform point = Point("Thornwood seed pod " + id, ThornwoodLayout.SeedPosition(id));
                Part("Seed moss nest " + id, point, 0, 2, 18, 6, ClearingPalette.GroundLight, -20);
                Part("Seed nest roots " + id, point, 0, 1, 22, 2, ClearingPalette.Ink, -19);
                List<SpriteRenderer> glyph = new List<SpriteRenderer>();
                glyph.Add(Part("Seed stalk " + id, point, 0, 6, 2, 10, ClearingPalette.MossLight, -18));
                glyph.Add(Part("Seed pod heart " + id, point, 0, 11, 6, 8, ClearingPalette.Cyan, -17));
                glyph.Add(Part("Seed pod upper " + id, point, 0, 15, 2, 2, ClearingPalette.CyanBright, -16));
                glyph.Add(Part("Seed pod glint " + id, point, -1, 12, 2, 4, ClearingPalette.Cream, -16));
                // Three distinct leaf silhouettes are recognizable without hue.
                for (int leaf = 0; leaf <= id; leaf++)
                    glyph.Add(Part("Seed identifying leaf " + id + " " + leaf, point,
                        (leaf - id * .5f) * 5f, 5, 4, 2, ClearingPalette.MossLight, -17));
                seedGlyphs[id] = glyph.ToArray();
                seedEmpty[id] = Part("Collected seed notch " + id, point, 0, 3, 6, 2, ClearingPalette.Stone, -18);
            }
        }

        private void BuildReturnSign()
        {
            Transform point = Point("Thornwood return trail sign", ThornwoodLayout.Entry);
            Part("Return trail slab", point, 0, 2, 36, 16, ClearingPalette.StoneShadow, -23);
            Part("Return trail edge", point, 0, -5, 36, 2, ClearingPalette.Ink, -22);
            Part("Return arrow shaft", point, 0, 4, 2, 10, ClearingPalette.Cream, -21);
            Part("Return arrow left", point, -3, 0, 4, 2, ClearingPalette.Cream, -21);
            Part("Return arrow right", point, 3, 0, 4, 2, ClearingPalette.Cream, -21);
            Part("Return arrow tip", point, 0, -2, 4, 2, ClearingPalette.Cream, -21);
        }

        private Transform Point(string name, ThornwoodPoint point)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(Root.transform, false);
            item.transform.position = new Vector3(point.X, point.Y, 0f);
            return item.transform;
        }

        private SpriteRenderer Part(string name, Transform parent, float x, float y,
            float width, float height, Color color, int order)
        {
            return shared.Block(name, parent, new Vector2(x * Pixel, y * Pixel),
                new Vector2(width * Pixel, height * Pixel), color, order);
        }

        private static SpriteRenderer Renderer(string name, Transform parent, Vector2 point, Sprite sprite, int order)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            // Static decoration uses the same 30-pixel world grid as its raster.
            // Attack footprints keep their exact domain geometry separately.
            item.transform.localPosition = new Vector2(Mathf.RoundToInt(point.x * 30f) * Pixel,
                Mathf.RoundToInt(point.y * 30f) * Pixel);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Sprite Upload(string name, ClearingRaster raster, Vector2 pivot)
        {
            Texture2D texture = new Texture2D(raster.Width, raster.Height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(raster.Pixels);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, raster.Width, raster.Height), pivot,
                30f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            texture.Apply(false, true);
            textures.Add(texture);
            sprites.Add(sprite);
            return sprite;
        }

        internal void SetActive(bool active)
        {
            if (disposed) return;
            Root.SetActive(active);
            for (int id = 0; id < Enemies.Length; id++)
                Enemies[id].Body.simulated = active && Enemies[id].Actor.activeSelf;
        }

        internal void SetSeedCollected(int index, bool collected = true)
        {
            if (disposed || index < 0 || index >= seedGlyphs.Length) return;
            for (int i = 0; i < seedGlyphs[index].Length; i++) seedGlyphs[index][i].gameObject.SetActive(!collected);
            seedEmpty[index].gameObject.SetActive(collected);
        }

        internal void SetCacheOpened(bool opened)
        {
            if (disposed) return;
            for (int i = 0; i < cacheLid.Length; i++) cacheLid[i].gameObject.SetActive(!opened);
            cacheSeal.gameObject.SetActive(!opened);
            cacheOpen.gameObject.SetActive(opened);
        }

        internal void RefreshEnemy(int id, SentinelState state, Vector2 facing, double simulationTime,
            SentinelFootprint footprint = null)
        {
            if (disposed || id < 0 || id >= Enemies.Length || state == null) return;
            Enemies[id].Refresh(state, facing, simulationTime, footprint, Root.activeSelf);
        }

        internal void ShowImpact(int id, bool kill, bool burst, double simulationTime)
        {
            if (disposed || id < 0 || id >= Enemies.Length) return;
            Enemies[id].ShowImpact(kill, burst, simulationTime);
        }

        internal void Refresh(double simulationTime)
        {
            if (disposed) return;
            for (int id = 0; id < Enemies.Length; id++) Enemies[id].RefreshImpact(simulationTime, Root.activeSelf);
        }

        internal void Clear()
        {
            if (disposed) return;
            for (int id = 0; id < Enemies.Length; id++) Enemies[id].Clear();
        }

        internal void Reset()
        {
            if (disposed) return;
            Clear();
            for (int id = 0; id < Enemies.Length; id++)
            {
                Enemies[id].Reset(ThornwoodLayout.EnemySpawn(id));
                SetSeedCollected(id, false);
            }
            SetCacheOpened(false);
        }

        internal void Dispose()
        {
            if (disposed) return;
            Clear();
            UnityEngine.Object.Destroy(Root);
            for (int i = 0; i < sprites.Count; i++) UnityEngine.Object.Destroy(sprites[i]);
            for (int i = 0; i < textures.Count; i++) UnityEngine.Object.Destroy(textures[i]);
            sprites.Clear();
            textures.Clear();
            disposed = true; // Borrowed ClearingVisuals square is not ours to destroy.
        }

        private static Vector2 ToVector(ThornwoodPoint point) { return new Vector2(point.X, point.Y); }

        private static ClearingRaster GroundRaster()
        {
            ClearingRaster raster = new ClearingRaster(480, 300);
            raster.Rect(0, 0, 480, 300, ClearingPalette.Forest);
            for (int y = 3; y < 296; y += 7)
                for (int x = 3; x < 476; x += 11)
                {
                    int pattern = (x * 17 + y * 31) % 29;
                    if (pattern < 2) raster.Rect(x, y, 4, 1, ClearingPalette.Ground);
                    else if (pattern == 7)
                    {
                        raster.Pixel(x, y, ClearingPalette.Moss);
                        raster.Pixel(x + 1, y + 1, ClearingPalette.GroundLight);
                    }
                }
            return raster;
        }

        private static ClearingRaster TreeRaster()
        {
            ClearingRaster raster = new ClearingRaster(48, 65);
            raster.Rect(12, 0, 25, 5, ClearingPalette.Ink);
            raster.Rect(20, 3, 9, 33, ClearingPalette.Ink);
            raster.Rect(22, 5, 3, 29, ClearingPalette.StoneShadow);
            raster.Rect(25, 6, 2, 26, ClearingPalette.Forest);
            for (int tier = 0; tier < 4; tier++)
            {
                int y = 20 + tier * 11;
                int width = 44 - tier * 8;
                int left = (48 - width) / 2;
                raster.Rect(left, y, width, 5, ClearingPalette.Ink);
                raster.Rect(left + 3, y + 4, width - 6, 7, ClearingPalette.Forest);
                raster.Rect(left + 6, y + 10, width - 12, 3, ClearingPalette.Ground);
                raster.Rect(left + 3, y + 5, 6, 2, ClearingPalette.Moss);
                raster.Pixel(left + width - 2, y + 4, ClearingPalette.MossLight);
            }
            raster.Rect(23, 58, 3, 7, ClearingPalette.GroundLight);
            raster.Rect(11, 1, 9, 2, ClearingPalette.StoneShadow);
            raster.Rect(28, 2, 12, 2, ClearingPalette.StoneShadow);
            return raster;
        }

        // Four-legged root hound: fixed 46x36 canvas and bottom-center anchor.
        // Walk contact/passing frames, crouched windup and extended pounce are
        // discrete cached poses, never actor scaling or a moving attack outline.
        private static ClearingRaster StalkerRaster(int pose)
        {
            ClearingRaster raster = new ClearingRaster(42, 36);
            int crouch = pose == 3 ? -3 : pose == 4 ? 1 : 0;
            int legA = pose == 1 ? 3 : pose == 2 ? -2 : 0;
            int legB = pose == 2 ? 3 : pose == 1 ? -2 : 0;
            for (int leg = 0; leg < 4; leg++)
            {
                int x = 7 + leg * 7 + (leg % 2 == 0 ? legA : legB);
                int y = pose == 4 ? 6 : 0;
                raster.Rect(x, y, 4, 12 + crouch, ClearingPalette.Ink);
                raster.Rect(x + 1, y + 2, 2, 8 + crouch, ClearingPalette.StoneShadow);
                raster.Rect(x - 1, y, 6, 2, ClearingPalette.Moss);
            }
            raster.Rect(5, 11 + crouch, 28, 14, ClearingPalette.Ink);
            raster.Rect(7, 13 + crouch, 24, 10, ClearingPalette.StoneShadow);
            raster.Rect(8, 19 + crouch, 20, 3, ClearingPalette.Moss);
            // Stepped shoulder and rump corners keep the wood creature from
            // reading as a single rectangle while retaining connected legs.
            raster.Rect(5, 11 + crouch, 2, 2, new Color(0f, 0f, 0f, 0f));
            raster.Rect(31, 11 + crouch, 2, 2, new Color(0f, 0f, 0f, 0f));
            raster.Rect(10, 14 + crouch, 3, 7, ClearingPalette.GroundLight);
            raster.Rect(18, 14 + crouch, 2, 6, ClearingPalette.Ink);
            raster.Rect(23, 16 + crouch, 7, 2, ClearingPalette.Ink);
            raster.Rect(29, 15 + crouch, 9, 15, ClearingPalette.Ink);
            raster.Rect(31, 17 + crouch, 5, 11, ClearingPalette.Moss);
            raster.Rect(35, 17 + crouch, 7, 6, ClearingPalette.Ink);
            raster.Rect(36, 19 + crouch, 5, 3, ClearingPalette.StoneShadow);
            raster.Rect(32, 24 + crouch, 3, 2, ClearingPalette.CyanBright);
            raster.Rect(37, 15 + crouch, 2, 3, ClearingPalette.Cream);
            raster.Rect(2, 19 + crouch, 6, 3, ClearingPalette.Ink);
            raster.Rect(0, 21 + crouch, 4, 3, ClearingPalette.Moss);
            for (int spine = 0; spine < 4; spine++)
            {
                int x = 7 + spine * 6;
                raster.Rect(x, 24 + crouch, 5, 3, ClearingPalette.Ink);
                raster.Rect(x + 1, 27 + crouch, 3, 3 + spine % 2, ClearingPalette.Moss);
                raster.Pixel(x + 2, 30 + crouch + spine % 2, ClearingPalette.MossLight);
            }
            // The cached hurt silhouette preserves opacity and the anchor; a
            // renderer tint alone would multiply dark bark instead of flashing.
            if (pose == 5)
                for (int pixel = 0; pixel < raster.Pixels.Length; pixel++)
                    if (raster.Pixels[pixel].a > 0f) raster.Pixels[pixel] = ClearingPalette.Cream;
            ClearingRaster padded = new ClearingRaster(46, 36);
            for (int y = 0; y < raster.Height; y++)
                for (int x = 0; x < raster.Width; x++) padded.Pixel(x + 2, y, raster.Pixels[y * raster.Width + x]);
            return padded;
        }
    }

    internal sealed class ThornwoodEnemyView
    {
        private const float Pixel = 1f / 30f;
        internal GameObject Actor { get; private set; }
        internal Rigidbody2D Body { get; private set; }
        internal Vector2 Position { get { return Body.position; } }
        private readonly BoxCollider2D collider;
        private readonly SpriteRenderer shape;
        private readonly SpriteRenderer health;
        private readonly SpriteRenderer healthBackground;
        private readonly Sprite[] poses;
        private readonly Transform warning;
        private readonly SpriteRenderer[] warningBorder = new SpriteRenderer[4];
        private readonly SpriteRenderer warningFill;
        private readonly SpriteRenderer[] warningTip = new SpriteRenderer[3];
        private readonly Transform impact;
        private readonly SpriteRenderer[] impactPieces = new SpriteRenderer[4];
        private double impactStart = double.NaN;
        private double impactUntil = double.NaN;
        private double hurtUntil = double.NaN;
        private Color impactColor;
        private bool impactKill;
        private Vector2 lastPosePosition;
        private double lastMovementAt = double.NaN;

        internal ThornwoodEnemyView(int id, Transform parent, ClearingVisuals art, Sprite[] poses)
        {
            this.poses = poses;
            Actor = new GameObject("Briar Stalker " + (id + 1));
            Actor.transform.SetParent(parent, false);
            Actor.layer = 2;
            Body = Actor.AddComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            collider = Actor.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(.7f, .4f);
            art.Block("Briar grounded shadow", Actor.transform, new Vector2(0f, .017f),
                new Vector2(1.05f, .12f), ClearingPalette.WithAlpha(ClearingPalette.Ink, .75f), -4);
            GameObject pose = new GameObject("Briar root hound cached pose");
            pose.transform.SetParent(Actor.transform, false);
            shape = pose.AddComponent<SpriteRenderer>();
            shape.sprite = poses[0];
            shape.sortingLayerName = "Player";
            healthBackground = art.Block("Briar health background", Actor.transform, new Vector2(0f, 1.3f),
                new Vector2(.8f, 2f * Pixel), ClearingPalette.Ink, 6, true);
            health = art.Block("Briar health", Actor.transform, new Vector2(0f, 1.3f),
                new Vector2(.8f, 2f * Pixel), ClearingPalette.MossLight, 7, true);
            GameObject warningObject = new GameObject("Briar locked pounce footprint " + id);
            warningObject.transform.SetParent(parent, false);
            warning = warningObject.transform;
            for (int side = 0; side < warningBorder.Length; side++)
                warningBorder[side] = art.Block("Briar pounce border " + side, warning, Vector2.zero,
                    new Vector2(Pixel, Pixel), ClearingPalette.AmberBright, -2);
            warningFill = art.Block("Briar pounce charge", warning, Vector2.zero,
                new Vector2(Pixel, Pixel), ClearingPalette.Amber, -3);
            for (int i = 0; i < warningTip.Length; i++)
                warningTip[i] = art.Block("Briar pounce direction " + i, warning, Vector2.zero,
                    new Vector2(2f * Pixel, 2f * Pixel), ClearingPalette.Cream, -1);
            GameObject impactObject = new GameObject("Briar independent impact " + id);
            impactObject.transform.SetParent(parent, false);
            impact = impactObject.transform;
            for (int side = 0; side < impactPieces.Length; side++)
                impactPieces[side] = art.Block("Briar hit stroke " + side, impact, Vector2.zero,
                    new Vector2(Pixel, Pixel), ClearingPalette.Cream, 30003, true);
            Reset(ThornwoodLayout.EnemySpawn(id));
        }

        internal void Refresh(SentinelState state, Vector2 facing, double time, SentinelFootprint footprint, bool worldActive)
        {
            bool live = !state.IsDead;
            Actor.SetActive(live);
            Body.simulated = worldActive && live;
            collider.enabled = live;
            if (!live) warning.gameObject.SetActive(false);
            else
            {
                // MovePosition is the authoritative motion path; it need not
                // leave a nonzero velocity in a recording boundary. Remember a
                // short simulation-time step so pause retains the last pose.
                Vector2 position = Position;
                if ((position - lastPosePosition).sqrMagnitude > .000001f) lastMovementAt = time;
                lastPosePosition = position;
                double movementAge = time - lastMovementAt;
                bool walking = movementAge >= 0d && movementAge < .12d;
                float facingX = footprint != null && state.AttackPhase != SentinelAttackPhase.Ready ?
                    footprint.CenterX - footprint.OriginX : facing.x;
                if (Mathf.Abs(facingX) > .01f) shape.flipX = facingX < 0f;
                int frame = state.AttackPhase == SentinelAttackPhase.Telegraph ? 3 :
                    state.AttackPhase == SentinelAttackPhase.Active ? 4 :
                    state.AttackPhase == SentinelAttackPhase.Ready && walking ?
                    1 + (int)(Math.Floor(time * 8d) % 2d) : 0;
                shape.sprite = poses[time < hurtUntil ? 5 : frame];
                shape.color = Color.white;
                int depth = Mathf.RoundToInt(-Position.y * 30f) * 16;
                shape.sortingOrder = depth + 3;
                healthBackground.sortingOrder = depth + 6;
                health.sortingOrder = depth + 7;
                float ratio = Mathf.Clamp01(state.Health / ClearingRun.SentinelMaxHealth);
                health.transform.localScale = new Vector3(.8f * ratio, 2f * Pixel, 1f);
                health.transform.localPosition = new Vector3((ratio - 1f) * .4f, 1.3f, 0f);
                health.color = time < hurtUntil ? ClearingPalette.Cream : ClearingPalette.MossLight;
                RefreshWarning(state, footprint);
            }
            RefreshImpact(time, worldActive);
        }

        private void RefreshWarning(SentinelState state, SentinelFootprint footprint)
        {
            bool active = state.AttackPhase == SentinelAttackPhase.Active;
            bool visible = footprint != null && (active || state.AttackPhase == SentinelAttackPhase.Telegraph);
            warning.gameObject.SetActive(visible);
            if (!visible) return;
            warning.position = new Vector3(footprint.CenterX, footprint.CenterY, 0f);
            warning.rotation = Quaternion.Euler(0f, 0f, footprint.AngleDegrees);
            float width = footprint.Width;
            float height = footprint.Height;
            Color color = active ? ClearingPalette.Danger : ClearingPalette.AmberBright;
            for (int side = 0; side < warningBorder.Length; side++)
            {
                bool horizontal = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                // Keep the full one-pixel stroke inside the accepted rectangle,
                // rather than advertising danger beyond the saved footprint.
                warningBorder[side].transform.localPosition = new Vector3(horizontal ? 0f : (width - Pixel) * .5f * sign,
                    horizontal ? (height - Pixel) * .5f * sign : 0f, 0f);
                warningBorder[side].transform.localScale = new Vector3(horizontal ? width : Pixel,
                    horizontal ? Pixel : height, 1f);
                warningBorder[side].color = color;
            }
            float progress = active ? 1f : Mathf.Clamp01(state.PhaseProgress);
            warningFill.transform.localScale = new Vector3(width * progress, height, 1f);
            warningFill.transform.localPosition = new Vector3((progress - 1f) * width * .5f, 0f, 0f);
            warningFill.color = ClearingPalette.WithAlpha(color, active ? .23f : .13f);
            for (int i = 0; i < warningTip.Length; i++)
                warningTip[i].transform.localPosition = new Vector3(width * .5f - (i == 1 ? 5f : 8f) * Pixel,
                    (i - 1) * 3f * Pixel, 0f);
        }

        internal void ShowImpact(bool kill, bool burst, double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0d) return;
            impactStart = time;
            impactUntil = time + (kill ? .38d : .16d);
            hurtUntil = kill ? double.NaN : time + .11d;
            impactKill = kill;
            impactColor = kill ? ClearingPalette.MossLight : burst ? ClearingPalette.CyanBright : ClearingPalette.Cream;
            impact.position = new Vector3(Position.x, Position.y + .55f, 0f);
            RefreshImpact(time, true);
        }

        internal void RefreshImpact(double time, bool visible)
        {
            double age = time - impactStart;
            bool showing = visible && age >= 0d && time < impactUntil;
            impact.gameObject.SetActive(showing);
            if (!showing) return;
            float progress = (float)(age / (impactUntil - impactStart));
            float radius = 9f + (float)Math.Floor(progress * (impactKill ? 12f : 5f));
            for (int side = 0; side < impactPieces.Length; side++)
            {
                bool horizontal = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                impactPieces[side].transform.localPosition = new Vector3(horizontal ? 0f : radius * sign * Pixel,
                    horizontal ? radius * sign * Pixel : 0f, 0f);
                impactPieces[side].transform.localScale = new Vector3((horizontal ? 8f : 2f) * Pixel,
                    (horizontal ? 2f : 8f) * Pixel, 1f);
                impactPieces[side].color = ClearingPalette.WithAlpha(impactColor, 1f - progress);
            }
        }

        internal void Clear()
        {
            impactStart = impactUntil = hurtUntil = double.NaN;
            impact.gameObject.SetActive(false);
            warning.gameObject.SetActive(false);
            Body.velocity = Vector2.zero;
            shape.color = Color.white;
            lastMovementAt = double.NaN;
            lastPosePosition = Position;
        }

        internal void Reset(ThornwoodPoint point)
        {
            Clear();
            Body.position = new Vector2(point.X, point.Y);
            lastPosePosition = Position;
            Actor.SetActive(true);
            Body.simulated = false;
            collider.enabled = true;
            shape.sprite = poses[0];
            shape.flipX = false;
        }
    }
}
