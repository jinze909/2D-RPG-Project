using System;

namespace Rpg.Gameplay
{
    public struct ThornwoodPoint
    {
        public readonly float X;
        public readonly float Y;
        public ThornwoodPoint(float x, float y) { X = x; Y = y; }
    }

    /// <summary>World-space axis-aligned rectangle; X/Y are its centre.</summary>
    public struct ThornwoodRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;
        public ThornwoodRect(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }

    /// <summary>
    /// One source of truth for runtime colliders, art and conservative foot-circle
    /// path checks. The forest is a bounded pocket reached through its waystone,
    /// not an incomplete adjacent corridor or a goal-dependent exit wall.
    /// </summary>
    public static class ThornwoodLayout
    {
        public const int SeedCount = 3;
        public const float MinX = 14f, MaxX = 30f, MinY = -5f, MaxY = 5f;
        public const float CenterX = 22f, CenterY = 0f, FootRadius = .23f;
        public static readonly ThornwoodPoint Entry = new ThornwoodPoint(22f, -3.8f);
        public static readonly ThornwoodPoint Cache = new ThornwoodPoint(22f, 3.5f);
        public static readonly ThornwoodPoint ReturnToClearing = new ThornwoodPoint(0f, 3.35f);

        private static readonly ThornwoodPoint[] seeds = {
            new ThornwoodPoint(18f, -.8f), new ThornwoodPoint(26f, .5f), new ThornwoodPoint(22f, 2.3f)
        };
        private static readonly ThornwoodPoint[] spawns = {
            new ThornwoodPoint(17.8f, 1f), new ThornwoodPoint(25.8f, -2.5f), new ThornwoodPoint(24f, 1.5f)
        };
        private static readonly ThornwoodRect[] obstacles = {
            new ThornwoodRect(16f, -1.8f, 1.2f, 3f),
            new ThornwoodRect(20f, -.6f, 1.2f, 1.8f),
            new ThornwoodRect(24f, -1.8f, 1.2f, 2.6f),
            new ThornwoodRect(28f, 1.3f, 1.2f, 3f),
            new ThornwoodRect(18.7f, 2.7f, 1.4f, 1.2f),
            new ThornwoodRect(25.5f, 2.8f, 1.3f, 1f)
        };
        // Full overlap at all four corners, with inner faces exactly on the bounds.
        private static readonly ThornwoodRect[] boundaries = {
            new ThornwoodRect(13.8f, 0f, .4f, 10.8f),
            new ThornwoodRect(30.2f, 0f, .4f, 10.8f),
            new ThornwoodRect(22f, -5.2f, 16.8f, .4f),
            new ThornwoodRect(22f, 5.2f, 16.8f, .4f)
        };

        public static int ObstacleCount { get { return obstacles.Length; } }
        public static int BoundaryCount { get { return boundaries.Length; } }
        public static ThornwoodRect GetObstacle(int index) { return Checked(obstacles, index); }
        public static ThornwoodRect GetBoundary(int index) { return Checked(boundaries, index); }
        public static ThornwoodPoint SeedPosition(int index) { return Checked(seeds, index); }
        public static ThornwoodPoint EnemySpawn(int index) { return Checked(spawns, index); }

        /// <summary>Inclusive region bounds and conservatively nonintersecting foot-circle.</summary>
        public static bool CanOccupy(float x, float y, float radius = FootRadius)
        {
            if (!Finite(x) || !Finite(y) || !Finite(radius) || radius < 0f ||
                (double)x - radius < MinX || (double)x + radius > MaxX ||
                (double)y - radius < MinY || (double)y + radius > MaxY) return false;
            for (int i = 0; i < obstacles.Length; i++)
            {
                ThornwoodRect wall = obstacles[i];
                if (Math.Abs((double)x - wall.X) <= wall.Width * .5d + radius &&
                    Math.Abs((double)y - wall.Y) <= wall.Height * .5d + radius) return false;
            }
            return true;
        }

        /// <summary>
        /// A conservative swept-circle path. Expanded AABBs intentionally reject
        /// corner grazing; every accepted segment is clear for the full foot radius.
        /// Endpoint-only checks would allow a large movement step through a trunk.
        /// </summary>
        public static bool CanTravel(float fromX, float fromY, float toX, float toY, float radius = FootRadius)
        {
            if (!CanOccupy(fromX, fromY, radius) || !CanOccupy(toX, toY, radius)) return false;
            for (int i = 0; i < obstacles.Length; i++)
                if (Intersects(obstacles[i], fromX, fromY, toX, toY, radius)) return false;
            return true;
        }

        /// <summary>Wall LOS without player-radius expansion; boundary and nonfinite points reject.</summary>
        public static bool HasLineOfSight(float fromX, float fromY, float toX, float toY)
        {
            if (!CanOccupy(fromX, fromY, 0f) || !CanOccupy(toX, toY, 0f)) return false;
            for (int i = 0; i < obstacles.Length; i++)
                if (Intersects(obstacles[i], fromX, fromY, toX, toY, 0f)) return false;
            return true;
        }

        private static bool Intersects(ThornwoodRect wall, float fromX, float fromY, float toX, float toY, float expansion)
        {
            double low = 0d, high = 1d;
            return Clip(fromX, (double)toX - fromX, wall.X - wall.Width * .5d - expansion,
                wall.X + wall.Width * .5d + expansion, ref low, ref high) &&
                Clip(fromY, (double)toY - fromY, wall.Y - wall.Height * .5d - expansion,
                wall.Y + wall.Height * .5d + expansion, ref low, ref high);
        }

        private static bool Clip(double start, double direction, double min, double max, ref double low, ref double high)
        {
            if (direction == 0d) return start >= min && start <= max;
            double first = (min - start) / direction, second = (max - start) / direction;
            if (first > second) { double swap = first; first = second; second = swap; }
            low = Math.Max(low, first); high = Math.Min(high, second);
            return low <= high;
        }

        private static T Checked<T>(T[] values, int index)
        {
            if (index < 0 || index >= values.Length) throw new ArgumentOutOfRangeException("index");
            return values[index];
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
