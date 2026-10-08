using System;
using System.Reflection;
using UnityEngine;

// Every check executes PlayerMovement from Assets, plus the actual generated
// PlayerActions wrapper. Only external Unity APIs are replaced by doubles.
public static class MovementBehaviorChecks
{
    sealed class Fixture
    {
        public readonly PlayerMovement Movement = new PlayerMovement();
        public readonly PlayerStats Stats = new PlayerStats { Health = 10, MaxHealth = 10 };
        public readonly Rigidbody2D Body = new Rigidbody2D();
        public readonly Animator Animator = new Animator();
        public readonly PlayerActions Actions;
        public readonly Player Player;
        public Fixture()
        {
            Player = new Player();
            Set(Player, "stats", Stats);
            var animation = new PlayerAnimations();
            animation.Components[typeof(Animator)] = Animator;
            Invoke(animation, "Awake");
            Movement.Components[typeof(Player)] = Player;
            Movement.Components[typeof(PlayerAnimations)] = animation;
            Movement.Components[typeof(Rigidbody2D)] = Body;
            Set(Movement, "speed", 4f);
            Invoke(Movement, "Awake");
            Actions = (PlayerActions)Get(Movement, "actions");
            Invoke(Movement, "OnEnable");
        }
        public void Tick(string message) { Invoke(Movement, message); }
        public Vector2 Input { set { Actions.asset.TestInput = value; } }
        public bool Moving
        {
            get { bool value; return Animator.Bools.TryGetValue(Animator.StringToHash("Moving"), out value) && value; }
        }
        public void BeginRight()
        {
            Input = new Vector2(1, 0);
            Tick("Update");
            Tick("FixedUpdate");
            Tick("Update");
        }
        public void Dispose() { Tick("OnDisable"); Tick("OnDestroy"); }
    }
    static FieldInfo Field(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
    static void Set(object target, string name, object value) { Field(target, name).SetValue(target, value); }
    static object Get(object target, string name) { return Field(target, name).GetValue(target); }
    static void Invoke(object target, string message)
    {
        var method = target.GetType().GetMethod(message, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null) method.Invoke(target, null);
    }
    static void Expect(bool condition, string detail) { if (!condition) throw new Exception(detail); }
    static bool Near(float a, float b) { return Math.Abs(a - b) < .0001f; }

    static int Main()
    {
        int passed = 0, failed = 0;
        Action<string, Action> test = (name, body) =>
        {
            try { body(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
        };
        test("fixed tick owns displacement", () =>
        {
            var f = new Fixture(); f.Input = new Vector2(1, 0); f.Tick("Update");
            f.Body.MoveRequests = 0; f.Tick("FixedUpdate");
            Expect(f.Body.MoveRequests == 1, "expected one MovePosition request in FixedUpdate, got " + f.Body.MoveRequests);
            f.Dispose();
        });
        test("render-only frames cannot move physics", () =>
        {
            var f = new Fixture(); f.BeginRight(); f.Body.MoveRequests = 0;
            for (int i = 0; i < 4; i++) f.Tick("Update");
            Expect(f.Body.MoveRequests == 0, "render-only frames queued " + f.Body.MoveRequests + " moves"); f.Dispose();
        });
        test("death cancels cached input before next move", () =>
        {
            var f = new Fixture(); f.BeginRight(); float start = f.Body.position.x;
            f.Body.velocity = new Vector2(4, 0); f.Stats.Health = 0;
            f.Tick("FixedUpdate"); f.Tick("Update");
            Expect(Near(f.Body.position.x, start) && !f.Moving && f.Body.velocity == Vector2.zero,
                "death retained displacement, velocity, or Moving=true"); f.Dispose();
        });
        test("disable clears physics velocity and locomotion", () =>
        {
            var f = new Fixture(); f.BeginRight(); f.Body.velocity = new Vector2(2, 3); f.Tick("OnDisable");
            Expect(f.Body.velocity == Vector2.zero && !f.Moving && !f.Actions.Movement.enabled,
                "disable left velocity, walk animation, or input active"); f.Tick("OnDestroy");
        });
        test("reenable cannot reuse stale input", () =>
        {
            var f = new Fixture(); f.BeginRight(); f.Tick("OnDisable"); f.Input = Vector2.zero;
            f.Tick("OnEnable"); float start = f.Body.position.x; f.Tick("Update"); f.Tick("FixedUpdate");
            Expect(Near(f.Body.position.x, start) && !f.Moving, "reenable produced an unrequested step"); f.Dispose();
        });
        test("input resources released on destroy", () =>
        {
            var f = new Fixture(); f.Dispose(); Expect(f.Actions.asset.Destroyed, "generated PlayerActions.Dispose was never called");
        });
        test("partial action value retains proportional speed", () =>
        {
            var f = new Fixture(); f.Input = new Vector2(.25f, 0); f.Tick("Update"); f.Tick("FixedUpdate");
            Expect(Near(f.Body.position.x, .02f), "0.25 action value requested " + f.Body.position.x + ", expected 0.02"); f.Dispose();
        });
        test("diagonal input never exceeds cardinal speed", () =>
        {
            var f = new Fixture(); f.Input = new Vector2(1, 1); f.Tick("Update"); f.Tick("FixedUpdate");
            Expect(Near(f.Body.position.magnitude, .08f), "diagonal request was " + f.Body.position.magnitude + ", expected 0.08"); f.Dispose();
        });
        test("one second uses fixed ticks at 25/50/100/144 render FPS", () =>
        {
            foreach (int fps in new[] { 25, 50, 100, 144 })
            {
                var f = new Fixture(); f.Input = new Vector2(1, 0); f.Tick("Update");
                int rendered = 0;
                for (int tick = 1; tick <= 50; tick++)
                {
                    while (rendered < (tick * fps) / 50) { f.Tick("Update"); rendered++; }
                    f.Tick("FixedUpdate");
                }
                Expect(f.Body.MoveRequests == 50 && Near(f.Body.position.x, 4f),
                    fps + " FPS issued " + f.Body.MoveRequests + " moves totaling " + f.Body.position.x); f.Dispose();
            }
        });
        test("latest render input snapshot is used by following physics tick", () =>
        {
            var f = new Fixture(); f.Input = new Vector2(1, 0); f.Tick("Update");
            f.Input = new Vector2(0, -1); f.Tick("Update"); f.Tick("FixedUpdate");
            Expect(Near(f.Body.position.x, 0) && Near(f.Body.position.y, -.08f), "physics used stale direction"); f.Dispose();
        });
        test("release stops next physics tick and preserves facing", () =>
        {
            var f = new Fixture(); f.BeginRight(); float start = f.Body.position.x;
            f.Input = Vector2.zero; f.Tick("Update"); f.Tick("FixedUpdate");
            float facing; f.Animator.Floats.TryGetValue(Animator.StringToHash("MoveX"), out facing);
            Expect(Near(f.Body.position.x, start) && !f.Moving && Near(facing, 1), "release moved or reset facing"); f.Dispose();
        });
        test("missing stats fail closed without moving or throwing", () =>
        {
            var f = new Fixture(); Set(f.Player, "stats", null); f.Input = new Vector2(1, 0);
            f.Tick("Update"); f.Tick("FixedUpdate");
            Expect(f.Body.MoveRequests == 0 && !f.Moving, "missing stats allowed movement"); f.Dispose();
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; real project C# with Unity boundary doubles, not native Unity.");
        return failed == 0 ? 0 : 1;
    }
}
