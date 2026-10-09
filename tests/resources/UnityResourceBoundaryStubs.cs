using System;
using System.Collections.Generic;

// Records calls at Unity's boundary only. These doubles do not simulate
// Animator transitions, native lifetime, rendering, or hardware input.
namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public class SerializeField : Attribute {}
    public class RequireComponent : Attribute { public RequireComponent(Type type) {} }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
    public class Object
    {
        public bool Destroyed;
        public static T Instantiate<T>(T original) where T : Object { return (T)original.MemberwiseClone(); }
        public static void Destroy(Object target) { target.Destroyed = true; }
    }
    public class ScriptableObject : Object {}
    public class MonoBehaviour : Object
    {
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public T GetComponent<T>() { object value; return Components.TryGetValue(typeof(T), out value) ? (T)value : default(T); }
    }
    public struct Vector2 { public float x, y; }
    public static class Mathf { public static float Max(float a, float b) { return Math.Max(a, b); } }
    public class Animator
    {
        public readonly List<int> Triggers = new List<int>();
        public static int StringToHash(string name) { return name.GetHashCode(); }
        public void SetTrigger(int key) { Triggers.Add(key); }
        public void ResetTrigger(int key) { Triggers.RemoveAll(value => value == key); }
        public void SetBool(int key, bool value) {}
        public void SetFloat(int key, float value) {}
    }
    public enum KeyCode { P }
    public static class Input
    {
        public static bool DamageKeyPressed;
        public static bool GetKeyDown(KeyCode key) { return key == KeyCode.P && DamageKeyPressed; }
    }
}
namespace UnityEngine.EventSystems {}
