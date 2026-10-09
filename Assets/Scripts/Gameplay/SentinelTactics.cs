using System;

namespace Rpg.Gameplay
{
    public enum SentinelDecision { Hold, Approach, Retreat, Attack }

    /// <summary>
    /// An immutable attack snapshot shared by warnings and contact checks. Creating
    /// a footprint locks the aim/target; subsequent actor movement cannot retarget it.
    /// Width follows the local X axis and Height follows the local Y axis.
    /// </summary>
    public sealed class SentinelFootprint
    {
        private readonly double directionX;
        private readonly double directionY;

        public float OriginX { get; private set; }
        public float OriginY { get; private set; }
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float AngleDegrees { get; private set; }

        internal SentinelFootprint(float originX, float originY, float centerX, float centerY,
            float width, float height, double aimX, double aimY)
        {
            OriginX = originX;
            OriginY = originY;
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
            directionX = aimX;
            directionY = aimY;
            AngleDegrees = (float)(Math.Atan2(aimY, aimX) * 180d / Math.PI);
        }

        public bool Contains(float pointX, float pointY)
        {
            if (!SentinelTactics.Finite(pointX) || !SentinelTactics.Finite(pointY)) return false;
            double offsetX = (double)pointX - CenterX;
            double offsetY = (double)pointY - CenterY;
            double along = offsetX * directionX + offsetY * directionY;
            double across = -offsetX * directionY + offsetY * directionX;
            // Float scene positions at a rotated boundary can differ by a few ULPs.
            // This tolerance is far below one pixel at the project's PPU of 30.
            const double boundaryTolerance = 0.000001d;
            return Math.Abs(along) <= Width * 0.5d + boundaryTolerance &&
                Math.Abs(across) <= Height * 0.5d + boundaryTolerance;
        }
    }

    /// <summary>Deterministic encounter roles and tactical admission, without Unity.</summary>
    public static class SentinelTactics
    {
        public static SentinelAttackKind RoleForIndex(int index)
        {
            switch (index)
            {
                case 0: return SentinelAttackKind.Sweep;
                case 1: return SentinelAttackKind.Lance;
                case 2: return SentinelAttackKind.Sigil;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public static SentinelDecision ChooseAction(SentinelAttackKind kind, float distance,
            bool clear, bool canRetreat = true)
        {
            if (!clear || !Finite(distance) || distance < 0f) return SentinelDecision.Hold;
            switch (kind)
            {
                case SentinelAttackKind.Sweep:
                    if (distance <= 0.85f) return SentinelDecision.Attack;
                    return distance < 3.7f ? SentinelDecision.Approach : SentinelDecision.Hold;
                case SentinelAttackKind.Lance:
                    if (distance <= 2.8f) return SentinelDecision.Attack;
                    return distance < 5f ? SentinelDecision.Approach : SentinelDecision.Hold;
                case SentinelAttackKind.Sigil:
                    if (distance < 2.2f) return canRetreat ? SentinelDecision.Retreat : SentinelDecision.Attack;
                    if (distance <= 3.4f) return SentinelDecision.Attack;
                    return distance < 6f ? SentinelDecision.Approach : SentinelDecision.Hold;
                default: return SentinelDecision.Hold;
            }
        }

        public static SentinelFootprint CreateFootprint(SentinelAttackKind kind,
            float originX, float originY, float targetX, float targetY)
        {
            if (!Finite(originX) || !Finite(originY) || !Finite(targetX) || !Finite(targetY)) return null;
            switch (kind)
            {
                case SentinelAttackKind.Sweep:
                    return new SentinelFootprint(originX, originY, originX, originY, 1.9f, 1.9f, 1d, 0d);
                case SentinelAttackKind.Lance:
                    double aimX = (double)targetX - originX;
                    double aimY = (double)targetY - originY;
                    double length = Math.Sqrt(aimX * aimX + aimY * aimY);
                    if (length > 0d)
                    {
                        aimX /= length;
                        aimY /= length;
                    }
                    else
                    {
                        aimX = 0d;
                        aimY = -1d;
                    }
                    return new SentinelFootprint(originX, originY,
                        (float)(originX + aimX * 1.6d), (float)(originY + aimY * 1.6d),
                        3.2f, 0.7f, aimX, aimY);
                case SentinelAttackKind.Sigil:
                    return new SentinelFootprint(originX, originY, targetX, targetY, 1.4f, 1.4f, 1d, 0d);
                default: return null;
            }
        }

        internal static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
