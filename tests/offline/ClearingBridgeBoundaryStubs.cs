using System;
using System.Collections.Generic;

// Minimal recording boundaries for compiling production scripts. Explicit
// component attachment lets presentation fixtures inspect values written by real
// project C#; no Awake/Start/Update is scheduled, no transform/layout is resolved,
// and physics, rendering and audio output remain inert. Player fixtures still
// inject components explicitly. Passing these tests is not native Unity evidence.
namespace UnityEngine
{
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) {} }
    public partial class Object { public string name; }
    public class Component : Object
    {
        public Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public GameObject gameObject;
        public Transform transform;
        public T GetComponent<T>()
        {
            object value;
            return Components.TryGetValue(typeof(T), out value) ? (T)value : default(T);
        }
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class GameObject : Object
    {
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public Transform transform;
        public int layer;
        public bool activeSelf = true;
        public static int CreatedCount;
        public GameObject(string name, params Type[] types)
        {
            this.name = name;
            CreatedCount++;
            transform = Array.IndexOf(types, typeof(RectTransform)) >= 0 ? new RectTransform() : new Transform();
            Attach(transform);
            foreach (Type type in types) if (type != typeof(RectTransform)) Attach((Component)Activator.CreateInstance(type));
        }
        private Component Attach(Component component)
        {
            component.gameObject = this;
            component.transform = transform;
            component.Components = new Dictionary<Type, object>();
            components[component.GetType()] = component;
            var graphic = component as UnityEngine.UI.Graphic;
            if (graphic != null) graphic.rectTransform = transform as RectTransform;
            return component;
        }
        public T AddComponent<T>() where T : Component { return (T)Attach((Component)Activator.CreateInstance(typeof(T))); }
        public T GetComponent<T>() { Component value; return components.TryGetValue(typeof(T), out value) ? (T)(object)value : default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public void SetActive(bool value) { activeSelf = value; }
    }
    public class Transform : Component
    {
        public Vector3 position, localPosition, localScale;
        public Quaternion rotation, localRotation;
        public Transform parent;
        public void SetParent(Transform parent, bool worldPositionStays) { this.parent = parent; }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
        public Rect rect;
    }
    public partial struct Vector2
    {
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 value) { return new Vector2(-value.x, -value.y); }
        public static Vector2 operator /(Vector2 value, float scale) { return value * (1f / scale); }
        public static Vector2 operator *(float scale, Vector2 value) { return value * scale; }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public void Normalize() { this = normalized; }
        public static implicit operator Vector3(Vector2 value) { return new Vector3(value.x, value.y, 0); }
        public static implicit operator Vector2(Vector3 value) { return new Vector2(value.x, value.y); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
    }
    public struct Quaternion { public static Quaternion Euler(float x, float y, float z) { return default(Quaternion); } }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public class Texture2D : Object
    {
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) {}
        public void SetPixel(int x, int y, Color color) {}
        public void SetPixels(Color[] colors) {}
        public void Apply() {}
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) {}
    }
    public class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return new Sprite(); }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType) { return new Sprite(); }
    }
    public enum SpriteMeshType { Tight, FullRect }
    public class SpriteRenderer : Behaviour
    {
        public Sprite sprite;
        public Color color;
        public string sortingLayerName;
        public int sortingOrder;
    }
    public class Collider2D : Behaviour { public Vector2 offset; public bool isTrigger; }
    public class BoxCollider2D : Collider2D { public Vector2 size; }
    public class CapsuleCollider2D : Collider2D { public Vector2 size; public CapsuleDirection2D direction; }
    public enum CapsuleDirection2D { Vertical, Horizontal }
    public class CircleCollider2D : Collider2D { public float radius; }
    public struct RaycastHit2D { public Collider2D collider; }
    public static class Physics2D
    {
        // Explicit fixture response, not a geometric query or physics simulation.
        public static Collider2D LinecastResponse;
        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask) { return new RaycastHit2D { collider = LinecastResponse }; }
    }
    public class Camera : Behaviour
    {
        public bool orthographic;
        public float orthographicSize, aspect;
        public Color backgroundColor;
        public CameraClearFlags clearFlags;
    }
    public enum CameraClearFlags { SolidColor }
    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum RigidbodyConstraints2D { None, FreezeRotation }
    public enum CollisionDetectionMode2D { Discrete, Continuous }
    public enum RigidbodyInterpolation2D { None, Interpolate }
    public static partial class Time { public static float deltaTime, unscaledDeltaTime, unscaledTime, timeScale = 1f; }
    public partial class Rigidbody2D
    {
        public RigidbodyType2D bodyType;
        public RigidbodyConstraints2D constraints;
        public CollisionDetectionMode2D collisionDetectionMode;
        public RigidbodyInterpolation2D interpolation;
        public float gravityScale, mass;
    }
    public partial class Animator
    {
        public float GetFloat(string key)
        {
            float value;
            return Floats.TryGetValue(StringToHash(key), out value) ? value : 0f;
        }
    }
    public static partial class Debug { public static void LogError(object message, Object context) {} }
    public static partial class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Rad2Deg = 180f / PI;
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Sin(float value) { return (float)Math.Sin(value); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static int RoundToInt(float value) { return (int)Math.Round(value); }
    }
    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return new AudioClip { name = name }; }
        public bool SetData(float[] samples, int offsetSamples) { return false; }
    }
    public class AudioSource : Behaviour
    {
        public bool playOnAwake, mute;
        public float spatialBlend, volume, pitch;
        public readonly List<AudioClip> OneShots = new List<AudioClip>();
        public int StopRequests;
        public void PlayOneShot(AudioClip clip) { OneShots.Add(clip); }
        public void Stop() { StopRequests++; }
    }
    public class Font : Object {}
    public enum TextAnchor { UpperLeft, UpperRight, LowerLeft, LowerRight, MiddleCenter }
    public enum HorizontalWrapMode { Wrap }
    public enum VerticalWrapMode { Truncate }
    public enum RenderMode { ScreenSpaceOverlay }
    public class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }
    public class CanvasRenderer : Component {}
    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object { return default(T); }
    }
}
namespace UnityEngine.UI
{
    using UnityEngine;
    public class Graphic : Behaviour
    {
        public Color color;
        public bool raycastTarget;
        public RectTransform rectTransform;
    }
    public class Text : Graphic
    {
        public Font font;
        public int fontSize;
        public TextAnchor alignment;
        public string text;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
    }
    public class Outline : Behaviour { public Color effectColor; public Vector2 effectDistance; }
    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ScaleWithScreenSize }
        public enum ScreenMatchMode { MatchWidthOrHeight }
        public ScaleMode uiScaleMode;
        public ScreenMatchMode screenMatchMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
    }
}
