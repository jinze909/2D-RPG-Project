using System;
using System.Collections;
using System.Collections.Generic;

// Minimal boundary doubles for compiling the real project scripts without an
// Editor license. These record API calls; they do not simulate Unity physics,
// rendering, Animator transitions, package import, or hardware input.
namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) {} }
    public class SerializeField : Attribute {}
    public class RequireComponent : Attribute { public RequireComponent(Type first, Type second, Type third) {} }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
    public class Object
    {
        public bool Destroyed;
        public static void Destroy(Object target) { target.Destroyed = true; }
    }
    public class ScriptableObject : Object {}
    public class MonoBehaviour : Object
    {
        public Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public T GetComponent<T>() { return (T)Components[typeof(T)]; }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public Vector2 normalized { get { return magnitude > 0 ? this * (1f / magnitude) : zero; } }
        public static Vector2 ClampMagnitude(Vector2 value, float maximum)
        {
            return value.sqrMagnitude > maximum * maximum ? value.normalized * maximum : value;
        }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator *(Vector2 a, float b) { return new Vector2(a.x * b, a.y * b); }
        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) < 1e-10f;
        }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
        public override bool Equals(object value) { return value is Vector2 && this == (Vector2)value; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
    }
    public static class Time { public static float fixedDeltaTime = .02f; }
    public static class Mathf { public static float Max(float a, float b) { return Math.Max(a, b); } }
    public class Rigidbody2D
    {
        public Vector2 position, velocity;
        public int MoveRequests;
        public readonly List<Vector2> Targets = new List<Vector2>();
        public void MovePosition(Vector2 target)
        {
            MoveRequests++;
            Targets.Add(target);
            // Accumulate requested displacement only. Native collisions and
            // the delayed physics integration require a licensed Unity test.
            position = target;
        }
    }
    public class Animator
    {
        public Dictionary<int, bool> Bools = new Dictionary<int, bool>();
        public Dictionary<int, float> Floats = new Dictionary<int, float>();
        public static int StringToHash(string name) { return name.GetHashCode(); }
        public void SetTrigger(int key) {}
        public void SetBool(int key, bool value) { Bools[key] = value; }
        public void SetFloat(int key, float value) { Floats[key] = value; }
    }
    public static class Input { public static bool GetKeyDown(KeyCode key) { return false; } }
    public enum KeyCode { P }
    public static class Debug
    {
        public static void Assert(bool condition, string message)
        {
            // Unity logs this assertion; it does not throw from a finalizer.
            if (!condition) Console.Error.WriteLine("Unity boundary assertion: " + message);
        }
    }
    public static class GUILayout { public static bool Button(string text) { return false; } }
}
namespace UnityEngine.EventSystems {}
namespace UnityEditor
{
    public class CustomEditor : Attribute { public CustomEditor(Type type) {} }
    public class Editor { public object target; public virtual void OnInspectorGUI() {} }
}
namespace UnityEngine.InputSystem.Utilities { public struct ReadOnlyArray<T> {} }
namespace UnityEngine.InputSystem
{
    using UnityEngine;
    using UnityEngine.InputSystem.Utilities;
    public class DefaultInputActions {}
    public class InputDevice {}
    public struct InputControlScheme {}
    public struct InputBinding {}
    public interface IInputActionCollection2 : IEnumerable<InputAction> {}
    public class InputAction
    {
        public struct CallbackContext {}
        public event Action<CallbackContext> started, performed, canceled;
        private readonly InputActionAsset owner;
        public InputAction(InputActionAsset owner) { this.owner = owner; }
        public T ReadValue<T>() { return (T)(object)(owner.Map.enabled ? owner.TestInput : Vector2.zero); }
    }
    public class InputActionMap
    {
        public bool enabled;
        public readonly InputAction Action;
        public InputActionMap(InputActionAsset owner) { Action = new InputAction(owner); }
        public InputAction FindAction(string name, bool throwIfNotFound) { return Action; }
        public void Enable() { enabled = true; }
        public void Disable() { enabled = false; }
    }
    public class InputActionAsset : Object, IEnumerable<InputAction>
    {
        public Vector2 TestInput;
        public readonly InputActionMap Map;
        public InputActionAsset() { Map = new InputActionMap(this); }
        public static InputActionAsset FromJson(string json) { return new InputActionAsset(); }
        public InputActionMap FindActionMap(string name, bool throwIfNotFound) { return Map; }
        public InputBinding? bindingMask { get; set; }
        public ReadOnlyArray<InputDevice>? devices { get; set; }
        public ReadOnlyArray<InputControlScheme> controlSchemes { get { return new ReadOnlyArray<InputControlScheme>(); } }
        public IEnumerable<InputBinding> bindings { get { return new InputBinding[0]; } }
        public bool Contains(InputAction action) { return action == Map.Action; }
        public void Enable() { Map.Enable(); }
        public void Disable() { Map.Disable(); }
        public InputAction FindAction(string name, bool throwIfNotFound) { return Map.Action; }
        public int FindBinding(InputBinding binding, out InputAction action) { action = Map.Action; return 0; }
        public IEnumerator<InputAction> GetEnumerator() { yield return Map.Action; }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
