using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rpg.Gameplay
{
    // Original crowned stone/moss silhouette, built once from the shared Point square.
    // Actor art uses integer 1/30-unit pixels; locked warning geometry is authoritative.
    internal sealed class ClearingBossVisuals
    {
        internal static readonly Vector2 AltarPosition = new Vector2(0f, -.7f);
        internal static readonly Vector2 SpawnPosition = new Vector2(0f, .6f);
        private const float Pixel = 1f / 30f;
        private readonly GameObject actor;
        private readonly Transform art;
        private readonly BoxCollider2D collider;
        private readonly SpriteRenderer[] bodyPieces;
        private readonly Color[] bodyColors;
        private readonly int[] bodyOrders;
        private readonly SpriteRenderer eye;
        private readonly SpriteRenderer[] phaseMarks;
        private readonly SpriteRenderer health;
        private readonly SpriteRenderer healthBackground;
        private readonly GameObject altar;
        private readonly SpriteRenderer altarBase;
        private readonly SpriteRenderer[] altarGlyph;
        private readonly SpriteRenderer altarSpent;
        private readonly Transform warning;
        private readonly SpriteRenderer[] warningBorder;
        private readonly SpriteRenderer warningFill;
        private readonly SpriteRenderer warningCrossA;
        private readonly SpriteRenderer warningCrossB;
        private readonly Transform impact;
        private readonly SpriteRenderer[] impactPieces;
        private double impactStartedAt = double.NaN;
        private double impactUntil = double.NaN;
        private double hurtUntil = double.NaN;
        private bool impactWasKill;
        private Color impactColor;
        private bool disposed;

        internal Rigidbody2D Body { get; private set; }
        internal Vector2 Position { get { return Body.position; } }

        internal ClearingBossVisuals(Transform parent, ClearingVisuals visuals)
        {
            actor = new GameObject("Moss Guardian - crowned stone Boss");
            actor.transform.SetParent(parent, false);
            actor.transform.position = SpawnPosition;
            actor.layer = 2; // Physical contact remains; Default-only wall LOS ignores actors.
            Body = actor.AddComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.mass = 20f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            collider = actor.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.2f, .6f);
            Part(visuals, "Guardian grounded shadow", actor.transform, 0, 1, 52, 6,
                ClearingPalette.WithAlpha(ClearingPalette.Ink, .75f), -4, false);
            GameObject artObject = new GameObject("Guardian pixel pose");
            artObject.transform.SetParent(actor.transform, false);
            art = artObject.transform;
            List<SpriteRenderer> pieces = new List<SpriteRenderer>();
            AddPiece(pieces, visuals, "Guardian broad silhouette", 0, 27, 40, 44, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian left shoulder", -23, 32, 12, 24, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian right shoulder", 23, 32, 12, 24, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian left fist", -25, 15, 12, 14, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian right fist", 25, 15, 12, 14, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian left boot", -11, 4, 16, 8, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian right boot", 11, 4, 16, 8, ClearingPalette.Ink, 2);
            AddPiece(pieces, visuals, "Guardian chest stone", 0, 27, 36, 40, ClearingPalette.Stone, 3);
            AddPiece(pieces, visuals, "Guardian chest lit face", -5, 30, 24, 32, ClearingPalette.StoneLight, 4);
            AddPiece(pieces, visuals, "Guardian chest shadow", 14, 25, 6, 34, ClearingPalette.StoneShadow, 4);
            AddPiece(pieces, visuals, "Guardian left arm stone", -23, 32, 8, 20, ClearingPalette.StoneLight, 3);
            AddPiece(pieces, visuals, "Guardian right arm stone", 23, 32, 8, 20, ClearingPalette.Stone, 3);
            AddPiece(pieces, visuals, "Guardian left fist face", -25, 15, 8, 10, ClearingPalette.Stone, 3);
            AddPiece(pieces, visuals, "Guardian right fist face", 25, 15, 8, 10, ClearingPalette.StoneShadow, 3);
            AddPiece(pieces, visuals, "Guardian left grounded boot", -11, 4, 12, 4, ClearingPalette.StoneShadow, 3);
            AddPiece(pieces, visuals, "Guardian right grounded boot", 11, 4, 12, 4, ClearingPalette.StoneShadow, 3);
            AddPiece(pieces, visuals, "Guardian crown shadow", 0, 51, 38, 10, ClearingPalette.Ink, 4);
            AddPiece(pieces, visuals, "Guardian crown stone", 0, 51, 34, 6, ClearingPalette.StoneLight, 5);
            AddPiece(pieces, visuals, "Guardian crown west prong", -14, 56, 6, 12, ClearingPalette.Ink, 4);
            AddPiece(pieces, visuals, "Guardian crown heart prong", 0, 58, 6, 16, ClearingPalette.Ink, 4);
            AddPiece(pieces, visuals, "Guardian crown east prong", 14, 56, 6, 12, ClearingPalette.Ink, 4);
            AddPiece(pieces, visuals, "Guardian crown west tip", -14, 56, 2, 8, ClearingPalette.MossLight, 5);
            AddPiece(pieces, visuals, "Guardian crown heart tip", 0, 59, 2, 10, ClearingPalette.Cream, 5);
            AddPiece(pieces, visuals, "Guardian crown east tip", 14, 56, 2, 8, ClearingPalette.MossLight, 5);
            AddPiece(pieces, visuals, "Guardian brow", 0, 40, 26, 8, ClearingPalette.Ink, 5);
            AddPiece(pieces, visuals, "Guardian left moss mantle", -15, 45, 12, 8, ClearingPalette.Moss, 6);
            AddPiece(pieces, visuals, "Guardian moss lit patch", -18, 47, 6, 4, ClearingPalette.MossLight, 7);
            AddPiece(pieces, visuals, "Guardian right moss mantle", 18, 33, 6, 16, ClearingPalette.Moss, 6);
            AddPiece(pieces, visuals, "Guardian chest seam", -2, 24, 2, 12, ClearingPalette.StoneShadow, 5);
            AddPiece(pieces, visuals, "Guardian chest crack", 3, 19, 8, 2, ClearingPalette.StoneShadow, 5);
            AddPiece(pieces, visuals, "Guardian core socket", 0, 28, 10, 10, ClearingPalette.Ink, 5);
            AddPiece(pieces, visuals, "Guardian core", 0, 28, 6, 6, ClearingPalette.Cyan, 6);
            AddPiece(pieces, visuals, "Guardian core glint", -1, 29, 2, 2, ClearingPalette.CyanBright, 7);
            bodyPieces = pieces.ToArray();
            bodyColors = new Color[bodyPieces.Length];
            bodyOrders = new int[bodyPieces.Length];
            for (int i = 0; i < bodyPieces.Length; i++)
            {
                bodyColors[i] = bodyPieces[i].color;
                bodyOrders[i] = bodyPieces[i].sortingOrder;
            }
            eye = Part(visuals, "Guardian warning eye", art, 0, 40, 18, 2, ClearingPalette.AmberBright, 7, true);
            phaseMarks = new[]
            {
                Part(visuals, "Guardian enraged left notch", art, -9, 43, 2, 4, ClearingPalette.Cream, 7, true),
                Part(visuals, "Guardian enraged right notch", art, 9, 43, 2, 4, ClearingPalette.Cream, 7, true)
            };
            healthBackground = Part(visuals, "Guardian health background", actor.transform, 0, 72, 50, 2,
                ClearingPalette.Ink, 8, true);
            health = Part(visuals, "Guardian health", actor.transform, 0, 72, 50, 2, ClearingPalette.Amber, 9, true);

            altar = new GameObject("Central Guardian altar - nonblocking");
            altar.transform.SetParent(parent, false);
            altar.transform.position = AltarPosition;
            altarBase = Part(visuals, "Guardian altar stone", altar.transform, 0, 4, 30, 14,
                ClearingPalette.StoneShadow, -21, false);
            Part(visuals, "Guardian altar rim", altar.transform, 0, -2, 30, 2, ClearingPalette.Ink, -20, false);
            altarGlyph = new[]
            {
                Part(visuals, "Altar diamond north", altar.transform, 0, 10, 2, 2, ClearingPalette.Stone, -19, false),
                Part(visuals, "Altar diamond west", altar.transform, -5, 5, 2, 2, ClearingPalette.Stone, -19, false),
                Part(visuals, "Altar diamond east", altar.transform, 5, 5, 2, 2, ClearingPalette.Stone, -19, false),
                Part(visuals, "Altar diamond south", altar.transform, 0, 0, 2, 2, ClearingPalette.Stone, -19, false),
                Part(visuals, "Altar cross upright", altar.transform, 0, 5, 2, 8, ClearingPalette.Stone, -19, false),
                Part(visuals, "Altar cross bar", altar.transform, 0, 5, 8, 2, ClearingPalette.Stone, -19, false)
            };
            altarSpent = Part(visuals, "Guardian altar empty notch", altar.transform, 0, 5, 10, 2,
                ClearingPalette.Stone, -19, false);

            GameObject warningObject = new GameObject("Guardian locked attack footprint");
            warningObject.transform.SetParent(parent, false);
            warning = warningObject.transform;
            warningBorder = new SpriteRenderer[4];
            for (int side = 0; side < warningBorder.Length; side++)
                warningBorder[side] = Part(visuals, "Guardian warning border " + side, warning, 0, 0, 2, 2,
                    ClearingPalette.AmberBright, -2, false);
            warningFill = Part(visuals, "Guardian warning charge", warning, 0, 0, 2, 2,
                ClearingPalette.Amber, -3, false);
            warningCrossA = Part(visuals, "Guardian active cross A", warning, 0, 0, 2, 2,
                ClearingPalette.Cream, -1, false);
            warningCrossB = Part(visuals, "Guardian active cross B", warning, 0, 0, 2, 2,
                ClearingPalette.Cream, -1, false);
            warningCrossA.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            warningCrossB.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            GameObject impactObject = new GameObject("Guardian independent hit confirmation");
            impactObject.transform.SetParent(parent, false);
            impact = impactObject.transform;
            impactPieces = new SpriteRenderer[4];
            for (int side = 0; side < impactPieces.Length; side++)
            {
                bool horizontal = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                impactPieces[side] = Part(visuals, "Guardian impact edge " + side, impact,
                    horizontal ? 0f : 11f * sign, horizontal ? 11f * sign : 0f,
                    horizontal ? 14f : 2f, horizontal ? 2f : 14f,
                    ClearingPalette.Cream, 30003, true);
            }
            Reset();
        }

        private void AddPiece(List<SpriteRenderer> pieces, ClearingVisuals visuals, string name,
            float x, float y, float width, float height, Color color, int order)
        {
            pieces.Add(Part(visuals, name, art, x, y, width, height, color, order, true));
        }

        private static SpriteRenderer Part(ClearingVisuals visuals, string name, Transform parent,
            float x, float y, float width, float height, Color color, int order, bool actorLayer)
        {
            return visuals.Block(name, parent, new Vector2(x * Pixel, y * Pixel),
                new Vector2(width * Pixel, height * Pixel), color, order, actorLayer);
        }

        internal void Refresh(ClearingRun run)
        {
            if (disposed || run == null) return;
            ClearingBossState boss = run.Boss;
            bool live = run.RequiresBoss && boss.IsAwake && !boss.IsDead && !run.IsDead && !run.IsComplete;
            actor.SetActive(live);
            Body.simulated = live;
            collider.enabled = live;
            altar.SetActive(run.RequiresBoss);
            bool ready = run.RequiresBoss && run.DefeatedCount == ClearingRun.SentinelCount && !boss.IsAwake;
            altarBase.color = ready ? ClearingPalette.StoneLight : ClearingPalette.StoneShadow;
            for (int i = 0; i < altarGlyph.Length; i++)
            {
                altarGlyph[i].gameObject.SetActive(!boss.IsAwake);
                altarGlyph[i].color = ready ? ClearingPalette.CyanBright : ClearingPalette.Stone;
            }
            altarSpent.gameObject.SetActive(boss.IsAwake);
            if (live)
            {
                int depth = Mathf.RoundToInt(-Position.y * 30f) * 16;
                bool hurting = run.Time < hurtUntil;
                for (int i = 0; i < bodyPieces.Length; i++)
                {
                    bodyPieces[i].color = hurting ? ClearingPalette.Cream : bodyColors[i];
                    bodyPieces[i].sortingOrder = depth + bodyOrders[i];
                }
                // A discrete two-pixel anticipation pose never scales the actor/collider.
                art.localPosition = new Vector3(0f, boss.AttackPhase == SentinelAttackPhase.Telegraph ? -2f * Pixel : 0f, 0f);
                eye.color = hurting || boss.AttackPhase == SentinelAttackPhase.Active ? ClearingPalette.Cream :
                    boss.AttackPhase == SentinelAttackPhase.Telegraph ? ClearingPalette.Danger :
                    boss.Enraged ? ClearingPalette.Danger : ClearingPalette.AmberBright;
                eye.sortingOrder = depth + 7;
                for (int i = 0; i < phaseMarks.Length; i++)
                {
                    phaseMarks[i].gameObject.SetActive(boss.Enraged);
                    phaseMarks[i].sortingOrder = depth + 7;
                }
                float ratio = Mathf.Clamp01(boss.Health / ClearingBossState.MaxHealth);
                health.transform.localScale = new Vector3(50f * Pixel * ratio, 2f * Pixel, 1f);
                health.transform.localPosition = new Vector3((ratio - 1f) * 25f * Pixel, 72f * Pixel, 0f);
                health.color = hurting ? ClearingPalette.Cream : boss.Enraged ? ClearingPalette.Danger : ClearingPalette.Amber;
                health.sortingOrder = depth + 9;
                healthBackground.sortingOrder = depth + 8;
            }
            RefreshWarning(boss, live);
            RefreshImpact(run.Time, !run.IsDead && !run.IsComplete);
        }

        private void RefreshWarning(ClearingBossState boss, bool live)
        {
            bool active = boss.AttackPhase == SentinelAttackPhase.Active;
            SentinelFootprint footprint = boss.AttackFootprint;
            bool visible = live && footprint != null && (active || boss.AttackPhase == SentinelAttackPhase.Telegraph);
            warning.gameObject.SetActive(visible);
            if (!visible) return;
            warning.position = new Vector3(footprint.CenterX, footprint.CenterY, 0f);
            warning.rotation = Quaternion.Euler(0f, 0f, footprint.AngleDegrees);
            warning.localScale = Vector3.one;
            float width = footprint.Width;
            float height = footprint.Height;
            Color danger = boss.AttackKind == SentinelAttackKind.Sigil ? ClearingPalette.CyanBright : ClearingPalette.AmberBright;
            for (int side = 0; side < warningBorder.Length; side++)
            {
                bool horizontal = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                warningBorder[side].transform.localPosition = new Vector3(horizontal ? 0f : width * .5f * sign,
                    horizontal ? height * .5f * sign : 0f, 0f);
                warningBorder[side].transform.localScale = new Vector3(horizontal ? width : 2f * Pixel,
                    horizontal ? 2f * Pixel : height, 1f);
                warningBorder[side].color = active ? ClearingPalette.Cream : danger;
            }
            float progress = active ? 1f : Mathf.Clamp01(boss.PhaseProgress);
            // Charge is inside the fixed outline. Only it grows; the contact edge stays locked.
            warningFill.transform.localScale = new Vector3(width * progress, height * progress, 1f);
            warningFill.color = ClearingPalette.WithAlpha(active ? ClearingPalette.Danger : danger,
                active ? .32f : .08f + .14f * progress);
            float crossLength = (float)Math.Floor(.68f * Mathf.Min(width, height) / Pixel) * Pixel;
            warningCrossA.transform.localScale = warningCrossB.transform.localScale = new Vector3(crossLength, 2f * Pixel, 1f);
            warningCrossA.gameObject.SetActive(active);
            warningCrossB.gameObject.SetActive(active);
        }

        internal void ShowImpact(bool isKill, bool isBurst, double simulationTime)
        {
            if (disposed || !Finite(simulationTime) || simulationTime < 0d) return;
            impactStartedAt = simulationTime;
            impactUntil = simulationTime + (isKill ? .3d : .12d);
            hurtUntil = simulationTime + .12d;
            impactWasKill = isKill;
            impactColor = isKill ? ClearingPalette.AmberBright : isBurst ? ClearingPalette.CyanBright : ClearingPalette.Cream;
            Vector2 point = Position + new Vector2(0f, 1f);
            impact.position = new Vector3(point.x, point.y, 0f);
            impact.localRotation = Quaternion.Euler(0f, 0f, isKill || isBurst ? 45f : 0f);
            RefreshImpact(simulationTime, true);
        }

        private void RefreshImpact(double simulationTime, bool playable)
        {
            bool visible = playable && Finite(simulationTime) && Finite(impactStartedAt) &&
                simulationTime >= impactStartedAt && simulationTime < impactUntil;
            impact.gameObject.SetActive(visible);
            if (!visible) return;
            float progress = Mathf.Clamp01((float)((simulationTime - impactStartedAt) / (impactUntil - impactStartedAt)));
            float radius = impactWasKill ? 11f + (float)Math.Floor(progress * 8f) : 9f + (float)Math.Floor(progress * 2f);
            for (int side = 0; side < impactPieces.Length; side++)
            {
                bool horizontal = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                impactPieces[side].transform.localPosition = new Vector3(horizontal ? 0f : radius * sign * Pixel,
                    horizontal ? radius * sign * Pixel : 0f, 0f);
                impactPieces[side].color = ClearingPalette.WithAlpha(impactColor, 1f - progress);
            }
        }

        internal void Clear()
        {
            if (disposed) return;
            Body.velocity = Vector2.zero;
            Body.simulated = false;
            impactStartedAt = impactUntil = hurtUntil = double.NaN;
            warning.gameObject.SetActive(false);
            impact.gameObject.SetActive(false);
            art.localPosition = Vector3.zero;
            for (int i = 0; i < bodyPieces.Length; i++) bodyPieces[i].color = bodyColors[i];
        }

        internal void Reset()
        {
            if (disposed) return;
            Clear();
            Body.position = SpawnPosition;
            Body.velocity = Vector2.zero;
            Body.simulated = false;
            collider.enabled = false;
            actor.SetActive(false);
            altar.SetActive(true);
            altarBase.color = ClearingPalette.StoneShadow;
            altarSpent.gameObject.SetActive(false);
            for (int i = 0; i < altarGlyph.Length; i++)
            {
                altarGlyph[i].gameObject.SetActive(true);
                altarGlyph[i].color = ClearingPalette.Stone;
            }
        }

        internal void Dispose()
        {
            if (disposed) return;
            Clear();
            UnityEngine.Object.Destroy(actor);
            UnityEngine.Object.Destroy(altar);
            UnityEngine.Object.Destroy(warning.gameObject);
            UnityEngine.Object.Destroy(impact.gameObject);
            // The common sprite/texture remain owned by ClearingVisuals.
            disposed = true;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
