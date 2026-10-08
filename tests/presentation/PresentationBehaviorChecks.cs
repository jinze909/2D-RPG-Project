using System;
using System.Reflection;
using Rpg.Gameplay;
using UnityEngine;
using UnityEngine.UI;

// Executes actual HUD/presentation C# using recording boundaries, with explicit
// state and component injection. It does not run Unity UI layout or rendering.
public static class PresentationBehaviorChecks
{
    private static FieldInfo Field(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
    private static T Get<T>(object target, string name)
    {
        var field = Field(target, name);
        if (field == null) throw new Exception("Presentation field absent: " + name);
        return (T)field.GetValue(target);
    }
    private static void Set(object target, string name, object value) { Field(target, name).SetValue(target, value); }
    private static void Invoke(object target, string name, params object[] args)
    {
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .0001f; }

    private static ClearingRun UnlockedRun()
    {
        var run = new ClearingRun();
        for (int target = 0; target < ClearingRun.SentinelCount; target++)
            while (!run.GetSentinel(target).IsDead)
            {
                long token;
                Expect(run.BeginPlayerAttack(PlayerAttackKind.Light, 12f, out token), "setup attack rejected");
                Expect(run.TryHitSentinel(token, target), "setup contact rejected");
                run.Advance(.46f);
            }
        return run;
    }

    private sealed class HudFixture
    {
        internal readonly ClearingHud Hud;
        internal readonly PlayerStats Stats = new PlayerStats { Health = 10f, MaxHealth = 10f, Mana = 12f, MaxMana = 12f };
        internal readonly ClearingRun Run = UnlockedRun();
        internal HudFixture()
        {
            Hud = new ClearingHud(new GameObject("HUD parent").transform);
            var root = (RectTransform)Get<Transform>(Hud, "root");
            root.rect = new Rect(0, 0, 960, 540);
        }
        internal Text Message { get { return Get<Text>(Hud, "message"); } }
        internal Text Help { get { return Get<Text>(Hud, "help"); } }
        internal void Width(float width) { ((RectTransform)Get<Transform>(Hud, "root")).rect = new Rect(0, 0, width, 540); }
        internal void Refresh(bool paused = false, bool near = true, string feedback = "")
        {
            Hud.Refresh(Stats, Run, paused, false, near, feedback);
        }
    }

    private static SpriteRenderer Renderer(string name)
    {
        var owner = new GameObject(name);
        return owner.AddComponent<SpriteRenderer>();
    }

    private sealed class RuntimeFixture
    {
        internal readonly ClearingRuntime Runtime = new ClearingRuntime();
        internal readonly ClearingRun Run = new ClearingRun();
        internal readonly PlayerStats Stats = new PlayerStats { Health = 10f, MaxHealth = 10f, Mana = 12f, MaxMana = 12f };
        internal readonly Player Player = new Player();
        internal readonly Rigidbody2D Body = new Rigidbody2D { position = -ClearingVisuals.PlayerFootOffset };
        internal readonly ClearingAudio Sound = new ClearingAudio(new GameObject("Recorded audio"));
        internal readonly object[] Enemies = new object[ClearingRun.SentinelCount];
        internal AudioSource Audio { get { return Get<AudioSource>(Sound, "source"); } }
        internal RuntimeFixture()
        {
            Runtime.gameObject = new GameObject("Runtime");
            Runtime.transform = Runtime.gameObject.transform;
            Physics2D.LinecastResponse = null;
            Time.timeScale = 1f;
            Set(Player, "stats", Stats);
            var animator = new Animator();
            var animations = new PlayerAnimations();
            animations.Components[typeof(Animator)] = animator; Invoke(animations, "Awake");
            Player.Components[typeof(PlayerAnimations)] = animations;
            var movement = new PlayerMovement();
            movement.Components[typeof(Player)] = Player;
            movement.Components[typeof(PlayerAnimations)] = animations;
            movement.Components[typeof(Rigidbody2D)] = Body; Invoke(movement, "Awake");
            var health = new PlayerHealth();
            health.Components[typeof(Player)] = Player;
            health.Components[typeof(PlayerAnimations)] = animations; Invoke(health, "Awake");
            var mana = new PlayerMana(); mana.Components[typeof(Player)] = Player; Invoke(mana, "Awake");
            var visuals = new ClearingVisuals(Runtime.transform);
            Set(visuals, "<Gate>k__BackingField", new GameObject("Gate"));
            Set(visuals, "<Beacon>k__BackingField", Renderer("Beacon"));
            Set(Runtime, "run", Run); Set(Runtime, "player", Player); Set(Runtime, "movement", movement);
            Set(Runtime, "health", health); Set(Runtime, "mana", mana); Set(Runtime, "playerBody", Body);
            Set(Runtime, "animator", animator); Set(Runtime, "playerRenderer", Renderer("Player"));
            Set(Runtime, "strike", Renderer("Strike")); Set(Runtime, "visuals", visuals); Set(Runtime, "sound", Sound);
            Type enemyType = typeof(ClearingRuntime).GetNestedType("EnemyView", BindingFlags.NonPublic);
            Array runtimeEnemies = Get<Array>(Runtime, "enemies");
            for (int i = 0; i < Enemies.Length; i++)
            {
                object enemy = Activator.CreateInstance(enemyType, true);
                Set(enemy, "Object", new GameObject("Enemy " + i));
                Set(enemy, "Body", new Rigidbody2D { position = i == 0 ? new Vector2(0, -.6f) : new Vector2(i == 1 ? 4 : -4, 0) });
                Set(enemy, "Eye", Renderer("Eye")); Set(enemy, "Health", Renderer("Health"));
                Set(enemy, "Warning", new GameObject("Warning").transform);
                Set(enemy, "Renderers", new SpriteRenderer[0]); Set(enemy, "LocalOrders", new int[0]); Set(enemy, "ActorLayers", new bool[0]);
                runtimeEnemies.SetValue(enemy, i); Enemies[i] = enemy;
                var prepare = typeof(ClearingRuntime).GetMethod("PreparePresentation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (prepare != null) prepare.Invoke(Runtime, new object[] { enemy, i });
            }
        }
        internal void Light() { Invoke(Runtime, "StartAttack", PlayerAttackKind.Light); }
        internal void Burst() { Invoke(Runtime, "StartAttack", PlayerAttackKind.Burst); }
        internal void Tick() { Invoke(Runtime, "FixedUpdate"); }
        internal void Refresh() { Invoke(Runtime, "RefreshViews"); }
        internal Transform Impact(int id = 0) { return Get<Transform>(Enemies[id], "Impact"); }
        internal void Position(int id, Vector2 value) { Get<Rigidbody2D>(Enemies[id], "Body").position = value; }
    }

    private static void LowerToLastHit(ClearingRun run, int target)
    {
        while (run.GetSentinel(target).Health > ClearingRun.LightDamage)
        {
            long token;
            Expect(run.BeginPlayerAttack(PlayerAttackKind.Light, 12f, out token), "setup attack rejected");
            Expect(run.TryHitSentinel(token, target), "setup contact rejected");
            run.Advance(.46f);
        }
    }

    private static int Main()
    {
        int passed = 0, failed = 0;
        Action<string, Action> test = (name, body) =>
        {
            try { body(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
        };
        test("beacon prompt is actionable during an active unlocked run", () =>
        {
            var f = new HudFixture(); f.Refresh();
            Expect(f.Message.text.Contains("E -"), "active nearby objective has no interaction prompt");
        });
        test("paused run hides disabled beacon interaction", () =>
        {
            var f = new HudFixture(); f.Refresh(paused: true);
            Expect(string.IsNullOrEmpty(f.Message.text), "paused encounter still advertises disabled E interaction");
        });
        test("dead run hides disabled beacon interaction", () =>
        {
            var f = new HudFixture(); f.Run.NotifyPlayerDeath(); f.Refresh();
            Expect(string.IsNullOrEmpty(f.Message.text), "dead player still sees a disabled E interaction");
        });
        test("feedback and movement help share one unobstructive edge slot", () =>
        {
            var f = new HudFixture(); f.Refresh(near: false, feedback: "Sentinel defeated");
            Expect(f.Message.rectTransform.anchorMin.x == 1f && f.Message.rectTransform.anchorMin.y == 0f,
                   "live feedback remains centered over the south combat band");
            Expect(string.IsNullOrEmpty(f.Help.text), "feedback and movement help overlap in the same slot");
            f.Refresh(near: false);
            Expect(string.IsNullOrEmpty(f.Message.text) && !string.IsNullOrEmpty(f.Help.text), "movement help did not return after feedback expired");
        });
        test("actual HUD refresh fits narrow edge columns and hides terminal feedback", () =>
        {
            foreach (float viewport in new[] { 0f, 620f, 720f, 960f, 1280f })
            {
                var f = new HudFixture(); f.Width(viewport); f.Refresh(near: false, feedback: "Hit confirmed");
                float width = viewport == 0f ? 960f : viewport;
                Expect(f.Message.rectTransform.sizeDelta == f.Help.rectTransform.sizeDelta, "feedback/help dimensions drift on a narrow canvas");
                var left = Get<Text>(f.Hud, "skills").rectTransform;
                var right = f.Message.rectTransform;
                Expect(left.anchoredPosition.x + left.sizeDelta.x < width + right.anchoredPosition.x - right.sizeDelta.x,
                       "actual refreshed skill and feedback rectangles overlap");
                Expect(right.anchoredPosition.y + right.sizeDelta.y <= 540f * .15f,
                       "actual feedback rectangle rises into the south combat band");
                foreach (int terminalState in new[] { 0, 1, 2 })
                {
                    var ended = new HudFixture();
                    if (terminalState == 1) ended.Run.NotifyPlayerDeath();
                    if (terminalState == 2) Expect(ended.Run.TryCompleteObjective(), "completion setup rejected");
                    ended.Refresh(paused: terminalState == 0, near: false, feedback: "Old live feedback");
                    Expect(string.IsNullOrEmpty(ended.Message.text) && string.IsNullOrEmpty(ended.Help.text)
                           && string.IsNullOrEmpty(Get<Text>(ended.Hud, "skills").text), "terminal run retains live actions/feedback");
                    Expect(!string.IsNullOrEmpty(Get<Text>(ended.Hud, "terminal").text), "terminal result was hidden with live HUD text");
                }
            }
        });
        test("accepted contact produces one hit cue and repeated token produces none", () =>
        {
            var f = new RuntimeFixture(); f.Light(); int swingCues = f.Audio.OneShots.Count;
            f.Tick();
            Expect(f.Run.GetSentinel(0).Health < ClearingRun.SentinelMaxHealth, "setup did not hit the sentinel");
            Expect(f.Audio.OneShots.Count == swingCues + 1, "accepted contact produced no distinct hit cue");
            f.Tick();
            Expect(f.Audio.OneShots.Count == swingCues + 1, "same action/contact duplicated hit feedback");
        });
        test("missed expired and wall-rejected contacts never emit hit feedback", () =>
        {
            foreach (int rejection in new[] { 0, 1, 2 })
            {
                var f = new RuntimeFixture();
                if (rejection == 0) f.Position(0, new Vector2(4, 0));
                f.Light(); int swingCues = f.Audio.OneShots.Count;
                if (rejection == 1) f.Run.Advance(.3f);
                if (rejection == 2) Physics2D.LinecastResponse = new BoxCollider2D();
                f.Tick();
                Expect(Near(f.Run.GetSentinel(0).Health, ClearingRun.SentinelMaxHealth), "rejected contact dealt damage");
                Expect(f.Audio.OneShots.Count == swingCues, "rejected contact produced a hit cue");
                var field = Field(f.Enemies[0], "Impact");
                if (field != null) Expect(!f.Impact().gameObject.activeSelf, "rejected contact produced an impact");
                Physics2D.LinecastResponse = null;
            }
        });
        test("one burst contacting two targets emits one cue and two cached impacts", () =>
        {
            var f = new RuntimeFixture(); f.Position(1, new Vector2(.2f, -.6f));
            f.Burst(); int swingCues = f.Audio.OneShots.Count, objects = GameObject.CreatedCount;
            f.Tick(); f.Refresh();
            Expect(f.Run.GetSentinel(0).Health < ClearingRun.SentinelMaxHealth && f.Run.GetSentinel(1).Health < ClearingRun.SentinelMaxHealth,
                   "burst setup did not contact two distinct targets");
            Expect(f.Audio.OneShots.Count == swingCues + 1, "multi-target burst emitted duplicate/no contact cues");
            Expect(f.Impact().gameObject.activeSelf && f.Impact(1).gameObject.activeSelf, "one accepted target has no impact");
            f.Tick(); f.Refresh();
            Expect(f.Audio.OneShots.Count == swingCues + 1 && GameObject.CreatedCount == objects, "repeated contact allocated effects or duplicated sound");
        });
        test("a later kill upgrades one action once even when another target also dies", () =>
        {
            var f = new RuntimeFixture(); LowerToLastHit(f.Run, 1); LowerToLastHit(f.Run, 2);
            f.Light(); int swingCues = f.Audio.OneShots.Count, objects = GameObject.CreatedCount;
            f.Tick();
            Expect(f.Audio.OneShots.Count == swingCues + 1, "first live contact has no cue");
            string contactClip = f.Audio.OneShots[f.Audio.OneShots.Count - 1].name;
            f.Position(1, new Vector2(.2f, -.6f)); f.Tick();
            Expect(f.Run.GetSentinel(1).IsDead && f.Audio.OneShots.Count == swingCues + 2
                   && f.Audio.OneShots[f.Audio.OneShots.Count - 1].name != contactClip,
                   "the first later kill did not upgrade contact feedback once");
            f.Position(2, new Vector2(-.2f, -.6f)); f.Tick();
            Expect(f.Run.GetSentinel(2).IsDead && !f.Run.GateUnlocked && f.Run.RewardCoins == 2 * ClearingRun.CoinsPerSentinel,
                   "distinct kills lost rewards or opened the gate before the third enemy died");
            Expect(f.Audio.OneShots.Count == swingCues + 2 && GameObject.CreatedCount == objects,
                   "a second kill in the same action duplicated the upgrade or allocated feedback");
        });
        test("kill confirmation survives hidden enemy and expires without allocation", () =>
        {
            var hit = new RuntimeFixture(); hit.Light(); hit.Tick();
            string contactClip = hit.Audio.OneShots[hit.Audio.OneShots.Count - 1].name;
            var f = new RuntimeFixture(); LowerToLastHit(f.Run, 0); f.Light();
            int swingCues = f.Audio.OneShots.Count, objects = GameObject.CreatedCount;
            f.Tick(); f.Refresh();
            Expect(f.Run.GetSentinel(0).IsDead && !Get<GameObject>(f.Enemies[0], "Object").activeSelf, "defeated actor was not removed");
            Expect(f.Impact().gameObject.activeSelf && f.Impact().parent == f.Runtime.transform,
                   "kill confirmation disappeared with the defeated enemy");
            Expect(f.Audio.OneShots.Count == swingCues + 1 && f.Audio.OneShots[f.Audio.OneShots.Count - 1].name != contactClip,
                   "kill did not receive a distinct once-only cue");
            f.Run.Advance(.35f); f.Refresh();
            Expect(!f.Impact().gameObject.activeSelf && GameObject.CreatedCount == objects, "expired kill effect was retained or recreated");
        });
        test("telegraph charge and active motif differ inside the fixed contact outline", () =>
        {
            var f = new RuntimeFixture(); object enemy = f.Enemies[0];
            Vector2 center = new Vector2(.4f, -.2f); Set(enemy, "AttackCenter", center);
            Expect(f.Run.BeginSentinelAttack(0), "warning setup rejected");
            f.Run.Advance(.1f); f.Refresh();
            var fill = Get<SpriteRenderer>(enemy, "WarningFill");
            float early = fill.transform.localScale.x;
            var cross = Get<SpriteRenderer>(enemy, "WarningCrossA");
            Expect(!cross.gameObject.activeSelf, "active motif appeared during charge");
            f.Run.Advance(.3f); f.Refresh();
            Expect(fill.transform.localScale.x > early && fill.transform.localScale.x < 1.9f, "charge does not communicate progress");
            f.Run.Advance(.26f); f.Refresh();
            var warning = Get<Transform>(enemy, "Warning");
            Expect(f.Run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Active && cross.gameObject.activeSelf,
                   "active window has no non-color motif");
            Expect(Near(fill.transform.localScale.x, 1.9f) && (Vector2)warning.position == center,
                   "active visual moved or exceeded the actual fixed damage footprint");
            f.Run.Advance(.13f); f.Refresh();
            Expect(!warning.gameObject.activeSelf, "recovery still advertises an active warning");
        });
        test("pause freezes impact state and resume lets the cached pulse expire", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick(); f.Refresh();
            var pulse = f.Impact(); float scale = pulse.localScale.x;
            float alpha = Get<SpriteRenderer[]>(f.Enemies[0], "ImpactSprites")[0].color.a;
            double time = f.Run.Time; Invoke(f.Runtime, "SetPaused", true);
            Time.unscaledTime += 20f;
            for (int i = 0; i < 10; i++) { f.Tick(); f.Refresh(); }
            Expect(f.Run.Time == time && pulse.gameObject.activeSelf && Near(scale, pulse.localScale.x)
                   && Near(alpha, Get<SpriteRenderer[]>(f.Enemies[0], "ImpactSprites")[0].color.a),
                   "pause advanced or erased a simulation-time impact");
            Invoke(f.Runtime, "SetPaused", false);
            f.Run.Advance(.2f); f.Refresh();
            Expect(!pulse.gameObject.activeSelf && Near(Time.timeScale, 1f), "resume retained expired feedback or changed time scale");
        });
        test("death and retry clear cached feedback and stop stale sounds", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick(); f.Refresh();
            Expect(f.Impact().gameObject.activeSelf, "setup has no live impact");
            f.Stats.Health = 0f; f.Tick(); f.Refresh();
            Expect(f.Run.IsDead && !f.Impact().gameObject.activeSelf, "death retained live impact feedback");
            int objects = GameObject.CreatedCount, stops = f.Audio.StopRequests;
            Invoke(f.Runtime, "ResetRun"); f.Refresh();
            foreach (object enemy in f.Enemies)
            {
                Expect(!Get<Transform>(enemy, "Impact").gameObject.activeSelf && !Get<Transform>(enemy, "Warning").gameObject.activeSelf,
                       "retry kept previous impact or warning visible");
            }
            Expect(!f.Run.IsDead && f.Audio.StopRequests == stops + 1 && GameObject.CreatedCount == objects,
                   "retry did not stop audio or allocated a replacement effect pool");
        });
        test("disabling encounter immediately clears warnings impacts strikes and audio", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick(); f.Refresh();
            var warning = Get<Transform>(f.Enemies[0], "Warning");
            var strike = Get<SpriteRenderer>(f.Runtime, "strike");
            Expect(warning.gameObject.activeSelf && f.Impact().gameObject.activeSelf && strike.gameObject.activeSelf,
                   "disable setup has no active presentation");
            int stops = f.Audio.StopRequests;
            double time = f.Run.Time;
            Invoke(f.Runtime, "OnDisable");
            Expect(!warning.gameObject.activeSelf && !f.Impact().gameObject.activeSelf && !strike.gameObject.activeSelf,
                   "disabled encounter retained live warning/impact/strike objects");
            Expect(f.Audio.StopRequests == stops + 1 && f.Run.Time == time,
                   "disable retained pending sounds or advanced simulation time");
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; actual project C# with recording boundaries, not native Unity.");
        return failed == 0 ? 0 : 1;
    }
}
