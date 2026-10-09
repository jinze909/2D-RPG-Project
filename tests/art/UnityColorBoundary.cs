// Only value-type arithmetic needed by the real managed raster builders.
// This does not substitute a renderer, texture importer, Animator or physics.
namespace UnityEngine
{
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public static class Mathf
    {
        public static float Clamp01(float value) { return value < 0 ? 0 : value > 1 ? 1 : value; }
    }
}
