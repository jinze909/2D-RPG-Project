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
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Presentation method absent: " + name);
        method.Invoke(target, args);
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
        internal bool ControlsEnabled { get { return Get<bool>(Get<PlayerMovement>(Runtime, "movement"), "controlsEnabled"); } }
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

    private sealed class RecordedProgressStore : IClearingProgressStore
    {
        internal ClearingProgressData Saved;
        internal bool AcceptWrites = true;
        internal readonly bool Writable;
        internal readonly ProgressLoadKind Kind;
        internal int Attempts, Writes;
        internal RecordedProgressStore(ClearingProgressData data = null, bool writable = true,
            ProgressLoadKind kind = ProgressLoadKind.New)
        {
            Saved = data ?? ClearingProgressData.Fresh;
            Writable = writable;
            Kind = kind;
        }
        public ProgressLoadResult Load() { return new ProgressLoadResult(Saved, Kind, Writable); }
        public bool TrySave(ClearingProgressData expected, ClearingProgressData next)
        {
            Attempts++;
            if (!AcceptWrites || !Writable || !object.ReferenceEquals(expected, Saved)) return false;
            Saved = next;
            Writes++;
            return true;
        }
    }

    private sealed class ProgressFixture
    {
        internal readonly RuntimeFixture F = new RuntimeFixture();
        internal readonly PlayerStats Template;
        internal readonly ClearingHud Hud;
        internal readonly RecordedProgressStore Store;
        internal readonly ClearingProgress Progress;
        internal PlayerStats Stats { get { return F.Player.Stats; } }
        internal string RewardId { get { return Get<string>(F.Runtime, "rewardId"); } }
        internal string Terminal { get { return Get<Text>(Hud, "terminal").text; } }
        internal ProgressFixture(ClearingProgressData data = null, bool writable = true,
            ProgressLoadKind kind = ProgressLoadKind.New)
        {
            Input.ClearPressed();
            // Exercise the actual actor-owned clone, never substitute a saved
            // authoring asset or hide template mutations behind a fake Player.
            Template = F.Stats;
            Invoke(F.Player, "Awake");
            Expect(!object.ReferenceEquals(Template, Stats), "Player Awake did not clone its authoring stats");
            Hud = new ClearingHud(F.Runtime.transform);
            ((RectTransform)Get<Transform>(Hud, "root")).rect = new Rect(0, 0, 960, 540);
            Set(F.Runtime, "hud", Hud);
            Store = new RecordedProgressStore(data, writable, kind);
            Progress = new ClearingProgress(Store);
            Invoke(F.Runtime, "InitializeProgress", Progress);
            Invoke(F.Runtime, "ResetRun");
        }
        internal void Update(params KeyCode[] keys)
        {
            Time.unscaledTime += .1f;
            Set(F.Runtime, "nextHudAt", 0f);
            Input.SetPressed(keys);
            try { Invoke(F.Runtime, "Update"); }
            finally { Input.ClearPressed(); }
        }
        internal void Unlock()
        {
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
                while (!F.Run.GetSentinel(i).IsDead)
                {
                    long token;
                    Expect(F.Run.BeginPlayerAttack(PlayerAttackKind.Light, Stats.Mana, out token), "progress setup attack rejected");
                    Expect(F.Run.TryHitSentinel(token, i), "progress setup contact rejected");
                    F.Run.Advance(ClearingRun.LightCooldown);
                }
        }
        internal void Complete()
        {
            Unlock();
            F.Body.position = ClearingVisuals.BeaconPosition - ClearingVisuals.PlayerFootOffset;
            Update(KeyCode.E);
            Expect(F.Run.IsComplete, "actual near-beacon E did not complete the clearing");
        }
        internal void AssertTemplate()
        {
            Expect(Near(Template.Health, 10f) && Near(Template.MaxHealth, 10f)
                && Near(Template.Mana, 12f) && Near(Template.MaxMana, 12f),
                "progression mutated the authored PlayerStats template");
        }
    }

    private static ClearingProgressData SavedCoins(int clears)
    {
        return new ClearingProgressData(clears, clears * ClearingProgressData.CompletionCoins, clears, 0, 0,
            clears == 0 ? "" : Guid.NewGuid().ToString("N"));
    }

    private static void RefreshProgress(HudFixture f, ClearingProgress progress, string rewardId,
        string notice = "", bool paused = false)
    {
        var method = typeof(ClearingHud).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null || method.GetParameters().Length != 9)
            throw new Exception("Progression HUD Refresh signature absent");
        method.Invoke(f.Hud, new object[] { f.Stats, f.Run, paused, false, true, "", progress, rewardId, notice });
    }

    private static string HudText(ClearingHud hud)
    {
        return Get<Text>(hud, "resources").text + "\n" + Get<Text>(hud, "objective").text + "\n"
            + Get<Text>(hud, "terminal").text;
    }

    private static Rect RecordedLabelRect(Text text, Vector2 viewport)
    {
        // These labels have fixed anchors/pivots and no layout group. Compute
        // only their declared rectangles; do not infer glyph or native layout.
        RectTransform rect = text.rectTransform;
        return new Rect(rect.anchorMin.x * viewport.x + rect.anchoredPosition.x - rect.pivot.x * rect.sizeDelta.x,
            rect.anchorMin.y * viewport.y + rect.anchoredPosition.y - rect.pivot.y * rect.sizeDelta.y,
            rect.sizeDelta.x, rect.sizeDelta.y);
    }

    private static bool RectanglesOverlap(Rect first, Rect second)
    {
        return first.x < second.x + second.width && second.x < first.x + first.width
            && first.y < second.y + second.height && second.y < first.y + first.height;
    }

    private static void AddProgressionChecks(Action<string, Action> test)
    {
        test("actual E cannot bank a locked objective or an unlocked far beacon", () =>
        {
            var p = new ProgressFixture();
            p.Update(KeyCode.E);
            Expect(!p.F.Run.IsComplete && p.Store.Attempts == 0 && p.Progress.Data.BankCoins == 0,
                "locked objective deposited rewards");
            p.Unlock(); p.Update(KeyCode.E);
            Expect(!p.F.Run.IsComplete && p.Store.Attempts == 0 && p.Progress.Data.ClearedRuns == 0,
                "far beacon deposited an unfinished encounter");
        });
        test("actual near-beacon E banks thirty once and locks completed movement", () =>
        {
            var p = new ProgressFixture(); p.Complete();
            Expect(p.Progress.Data.BankCoins == 30 && p.Progress.Data.ClearedRuns == 1 && p.Store.Writes == 1
                && p.Progress.IsCompletionBanked(p.RewardId), "completed encounter was not banked once");
            Expect(!p.F.ControlsEnabled && p.F.Body.velocity == Vector2.zero, "banked completion retained movement");
            int cues = p.F.Audio.OneShots.Count;
            p.Update(KeyCode.E); p.Update(KeyCode.E);
            Invoke(p.F.Runtime, "OnDisable"); p.Update(KeyCode.E);
            Expect(p.Progress.Data.BankCoins == 30 && p.Progress.Data.ClearedRuns == 1
                && p.Store.Writes == 1 && p.F.Audio.OneShots.Count == cues,
                "repeated E or disable/resume duplicated banked rewards or audio");
            p.AssertTemplate();
        });
        test("failed completion preserves ledger and E retries the same reward exactly once", () =>
        {
            var p = new ProgressFixture(); p.Store.AcceptWrites = false;
            ClearingProgressData before = p.Progress.Data; string id = p.RewardId;
            p.Complete();
            Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Progress.SaveFailed && p.Store.Writes == 0
                && p.F.Run.IsComplete && p.RewardId == id, "save failure changed ledger or reward identity");
            string failure = p.Terminal.ToLowerInvariant();
            Expect(failure.Contains("unbanked") && failure.Contains("e") && failure.Contains("r")
                && (failure.Contains("lost") || failure.Contains("lose")), "failed reward lacks retry/continue-loss choices");
            p.Update(KeyCode.E);
            Expect(p.Store.Writes == 0 && p.Progress.Data.BankCoins == 0, "failed retry awarded live coins");
            p.Store.AcceptWrites = true; p.Update(KeyCode.E);
            Expect(p.Progress.Data.BankCoins == 30 && p.Progress.Data.ClearedRuns == 1
                && p.Store.Writes == 1 && !p.Progress.SaveFailed && p.RewardId == id,
                "same completed reward did not recover once after storage resumed");
            p.Update(KeyCode.E);
            Expect(p.Store.Writes == 1 && p.Progress.Data.BankCoins == 30, "successful retry duplicated a deposit");
        });
        test("R after an unbanked completion continues without inventing saved rewards", () =>
        {
            var p = new ProgressFixture(); p.Store.AcceptWrites = false; p.Complete();
            string previousId = p.RewardId; p.Update(KeyCode.R);
            Expect(!p.F.Run.IsComplete && !p.F.Run.IsDead && p.F.Run.RewardCoins == 0
                && p.Progress.Data.BankCoins == 0 && p.Progress.Data.ClearedRuns == 0 && p.Store.Writes == 0,
                "continuing an unbanked result silently awarded or retained its coins");
            Expect(previousId != p.RewardId && p.RewardId.Length == 32 && p.F.ControlsEnabled,
                "next encounter did not get a fresh identity and controls");
            p.AssertTemplate();
        });
        test("upgrade keys cannot spend in active paused dead or unbanked states", () =>
        {
            foreach (int state in new[] { 0, 1, 2, 3, 4 })
            {
                var p = new ProgressFixture(SavedCoins(3));
                if (state == 1) Invoke(p.F.Runtime, "SetPaused", true);
                if (state == 2) { p.Stats.Health = 0f; p.Update(); }
                if (state == 3) { p.Store.AcceptWrites = false; p.Complete(); }
                if (state == 4) { p.Complete(); Invoke(p.F.Runtime, "SetPaused", true); }
                ClearingProgressData before = p.Progress.Data; int attempts = p.Store.Attempts;
                foreach (KeyCode key in new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Keypad1, KeyCode.Keypad2 }) p.Update(key);
                Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Store.Attempts == attempts,
                    "upgrade key spent coins in disabled state " + state);
                p.AssertTemplate();
            }
        });
        foreach (bool focus in new[] { false, true })
        {
            bool isFocus = focus;
            test((isFocus ? "Focus" : "Vitality") + " main and keypad choices spend saved coins and activate next run only", () =>
            {
                foreach (KeyCode key in isFocus ? new[] { KeyCode.Alpha2, KeyCode.Keypad2 } : new[] { KeyCode.Alpha1, KeyCode.Keypad1 })
                {
                    var p = new ProgressFixture(); p.Complete();
                    p.Stats.Health = 7f; p.Stats.Mana = 8f;
                    int cues = p.F.Audio.OneShots.Count; p.Update(key);
                    Expect(p.Progress.Data.BankCoins == 0 && p.Store.Writes == 2
                        && (isFocus ? p.Progress.Data.FocusRank : p.Progress.Data.VitalityRank) == 1,
                        "accepted upgrade did not consume its thirty saved coins");
                    Expect(Near(p.Stats.MaxHealth, 10f) && Near(p.Stats.MaxMana, 12f)
                        && Near(p.Stats.Health, 7f) && Near(p.Stats.Mana, 8f),
                        "purchase changed the already completed encounter's resources");
                    Expect(p.F.Audio.OneShots.Count == cues + 1, "saved purchase produced no distinct reward cue");
                    p.Update(KeyCode.R);
                    Expect(Near(p.Stats.MaxHealth, isFocus ? 10f : 12f) && Near(p.Stats.MaxMana, isFocus ? 14f : 12f)
                        && Near(p.Stats.Health, p.Stats.MaxHealth) && Near(p.Stats.Mana, p.Stats.MaxMana),
                        "retry did not apply and fill the purchased next-run bonus");
                    p.AssertTemplate();
                }
            });
        }
        test("failed purchase preserves bank rank actor and audio until the same choice saves", () =>
        {
            var p = new ProgressFixture(); p.Complete(); p.Store.AcceptWrites = false;
            ClearingProgressData before = p.Progress.Data; int cues = p.F.Audio.OneShots.Count;
            p.Update(KeyCode.Alpha1);
            Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Progress.SaveFailed
                && p.Progress.Data.VitalityRank == 0 && p.Progress.Data.BankCoins == 30
                && p.F.Audio.OneShots.Count == cues && Near(p.Stats.MaxHealth, 10f),
                "failed purchase mutated bank/rank/resources or played a success cue");
            Expect(p.Terminal.ToLowerInvariant().Contains("fail") || p.Terminal.ToLowerInvariant().Contains("not saved"),
                "failed purchase has no truthful storage failure feedback");
            p.Store.AcceptWrites = true; p.Update(KeyCode.Alpha1);
            Expect(p.Progress.Data.VitalityRank == 1 && p.Progress.Data.BankCoins == 0 && p.Store.Writes == 2
                && !p.Progress.SaveFailed && p.F.Audio.OneShots.Count == cues + 1, "purchase retry did not commit once");
            p.Update(KeyCode.Alpha1);
            Expect(p.Store.Writes == 2 && p.F.Audio.OneShots.Count == cues + 1, "insufficient coins replayed success");
        });
        test("loaded bonuses and repeated retries never stack or mutate the authoring template", () =>
        {
            var data = new ClearingProgressData(5, 30, 3, 1, 1, Guid.NewGuid().ToString("N"));
            var p = new ProgressFixture(data, kind: ProgressLoadKind.Loaded);
            Expect(Near(p.Stats.MaxHealth, 12f) && Near(p.Stats.MaxMana, 14f), "loaded ranks were not applied at run start");
            for (int i = 0; i < 4; i++)
            {
                string id = p.RewardId; p.Stats.Health = 0f; p.Stats.Mana = 1f; p.Update(); p.Update(KeyCode.R);
                Expect(Near(p.Stats.MaxHealth, 12f) && Near(p.Stats.MaxMana, 14f)
                    && Near(p.Stats.Health, 12f) && Near(p.Stats.Mana, 14f) && p.RewardId != id,
                    "retry accumulated bonuses, retained old reward identity or failed to refill");
                Expect(p.Progress.Data.BankCoins == 30 && p.Progress.Data.ClearedRuns == 3 && p.Store.Attempts == 0,
                    "death or retry banked an unfinished encounter");
                p.AssertTemplate();
            }
        });
        test("terminal R cannot admit held-frame attacks or beacon input into the next encounter", () =>
        {
            foreach (bool completed in new[] { false, true })
            {
                var p = new ProgressFixture();
                if (completed) p.Complete();
                else { p.Stats.Health = 0f; p.Update(); }
                int writes = p.Store.Writes;
                p.Update(KeyCode.R, KeyCode.J, KeyCode.K, KeyCode.E);
                Expect(!p.F.Run.IsComplete && !p.F.Run.IsDead && !p.F.Run.PlayerAttackActive
                    && Near(p.F.Run.LightCooldownRemaining, 0f) && Near(p.F.Run.BurstCooldownRemaining, 0f)
                    && Near(p.Stats.Mana, p.Stats.MaxMana) && p.Store.Writes == writes,
                    "restart admitted a previous result-frame attack or objective input");
                p.AssertTemplate();
            }
        });
        test("protected progress permits clearing but never claims or writes a saved completion", () =>
        {
            var p = new ProgressFixture(SavedCoins(3), false, ProgressLoadKind.Unsupported);
            ClearingProgressData before = p.Progress.Data; p.Complete();
            p.Update(KeyCode.E); p.Update(KeyCode.Alpha1); p.Update(KeyCode.Alpha2);
            Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Store.Attempts == 0 && p.F.Run.IsComplete,
                "protected save was overwritten by completion or upgrade input");
            string terminal = p.Terminal.ToLowerInvariant();
            Expect(terminal.Contains("unbanked") && (terminal.Contains("protected") || terminal.Contains("unavailable")),
                "protected completion was presented as successfully saved");
        });
        test("full profile capacity preserves its ledger and permits an explicitly unbanked next run", () =>
        {
            var p = new ProgressFixture(SavedCoins(ClearingProgressData.MaxClearedRuns));
            ClearingProgressData before = p.Progress.Data; p.Complete();
            Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Store.Attempts == 0,
                "full ledger deposited beyond its bounded completion count");
            string terminal = p.Terminal.ToLowerInvariant();
            Expect(terminal.Contains("unbanked") && (terminal.Contains("cap") || terminal.Contains("limit")),
                "capacity rejection was advertised as saved or omitted its cause");
            p.Update(KeyCode.Alpha1); p.Update(KeyCode.Alpha2); p.Update(KeyCode.R);
            Expect(object.ReferenceEquals(before, p.Progress.Data) && p.Store.Attempts == 0
                && !p.F.Run.IsComplete && p.F.ControlsEnabled, "capacity rejection locked play or spent an unbanked result");
        });
        test("all upgrade price tiers commit once and capped input never spends or plays success", () =>
        {
            foreach (KeyCode key in new[] { KeyCode.Alpha1, KeyCode.Alpha2 })
            {
                var p = new ProgressFixture(SavedCoins(10)); p.Complete();
                int coins = 330;
                foreach (int price in new[] { 30, 45, 60 })
                {
                    Expect(p.Terminal.Contains(price.ToString()), "result omits the next valid upgrade price");
                    p.Update(key); coins -= price;
                    Expect(p.Progress.Data.BankCoins == coins, "actual input used the wrong upgrade tier price");
                }
                int attempts = p.Store.Attempts, cues = p.F.Audio.OneShots.Count;
                p.Update(key);
                Expect(p.Progress.Data.BankCoins == coins && p.Store.Attempts == attempts
                    && p.F.Audio.OneShots.Count == cues && p.Terminal.ToLowerInvariant().Contains("max"),
                    "capped input spent coins, wrote state or emitted success without a purchase");
                Expect(Near(p.Stats.MaxHealth, 10f) && Near(p.Stats.MaxMana, 12f), "tiered purchases applied before a new encounter");
                p.Update(KeyCode.R);
                Expect(Near(p.Stats.MaxHealth, key == KeyCode.Alpha1 ? 16f : 10f)
                    && Near(p.Stats.MaxMana, key == KeyCode.Alpha2 ? 18f : 12f), "maximum saved rank was not applied once on restart");
                p.AssertTemplate();
            }
        });
        test("HUD displays saved bank and next-run upgrade ranks during actual play", () =>
        {
            var f = new HudFixture();
            var data = new ClearingProgressData(5, 30, 3, 1, 1, Guid.NewGuid().ToString("N"));
            var progress = new ClearingProgress(new RecordedProgressStore(data));
            RefreshProgress(f, progress, Guid.NewGuid().ToString("N"));
            string text = HudText(f.Hud).ToLowerInvariant();
            Expect(text.Contains("bank") && text.Contains("30") && text.Contains("vitality") && text.Contains("focus"),
                "live HUD has no bank/rank summary");
            Expect(Get<Text>(f.Hud, "terminal").text == "", "active game displays terminal choices over combat");
        });
        test("empty platform persistence path creates a read-only profile instead of relative files", () =>
        {
            string original = Application.persistentDataPath;
            string taskDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rpg-empty-path-" + Guid.NewGuid().ToString("N"));
            string previousDirectory = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.CreateDirectory(taskDirectory);
            try
            {
                System.IO.Directory.SetCurrentDirectory(taskDirectory);
                foreach (string path in new[] { "", " \t\r\n" })
                {
                    Application.persistentDataPath = path;
                    var create = typeof(ClearingRuntime).GetMethod("CreateProgress", BindingFlags.Static | BindingFlags.NonPublic);
                    Expect(create != null, "platform progress factory absent");
                    var progress = (ClearingProgress)create.Invoke(null, null);
                    Expect(!progress.CanWrite && progress.LoadKind == ProgressLoadKind.Unavailable
                        && progress.Data.BankCoins == 0, "empty persistence path admitted writable relative storage");
                    var run = UnlockedRun(); Expect(run.TryCompleteObjective(), "empty path setup rejected");
                    Expect(progress.BankCompletion(run, Guid.NewGuid().ToString("N")) == ProgressActionResult.Unavailable,
                        "empty platform path saved a completion");
                    Expect(System.IO.Directory.GetFileSystemEntries(taskDirectory).Length == 0,
                        "empty platform path created files in the working directory");
                }
            }
            finally
            {
                Application.persistentDataPath = original;
                System.IO.Directory.SetCurrentDirectory(previousDirectory);
                System.IO.Directory.Delete(taskDirectory, true);
            }
        });
        test("banked completion HUD shows both next-run choices prices and rank cap", () =>
        {
            var f = new HudFixture(); Expect(f.Run.TryCompleteObjective(), "HUD complete setup rejected");
            string id = Guid.NewGuid().ToString("N");
            var progress = new ClearingProgress(new RecordedProgressStore());
            Expect(progress.BankCompletion(f.Run, id) == ProgressActionResult.Saved, "HUD bank setup failed");
            RefreshProgress(f, progress, id);
            string text = Get<Text>(f.Hud, "terminal").text.ToLowerInvariant();
            Expect(text.Contains("1") && text.Contains("2") && text.Contains("vitality") && text.Contains("focus")
                && text.Contains("30") && text.Contains("next"), "banked result omits upgrade prices or next-run activation");
            var capped = new ClearingProgressData(15, 0, 9, 3, 3, id);
            RefreshProgress(f, new ClearingProgress(new RecordedProgressStore(capped)), id);
            text = Get<Text>(f.Hud, "terminal").text.ToLowerInvariant();
            Expect(text.Contains("max") && !text.Contains("cost 0"), "capped choices advertise a zero-cost purchase");
        });
        test("completion HUD limits result rectangles on short and narrow canvas boundaries", () =>
        {
            foreach (Vector2 viewport in new[] { new Vector2(620, 320), new Vector2(720, 400), new Vector2(960, 540) })
            {
                var f = new HudFixture();
                ((RectTransform)Get<Transform>(f.Hud, "root")).rect = new Rect(0, 0, viewport.x, viewport.y);
                Expect(f.Run.TryCompleteObjective(), "short HUD setup rejected");
                string id = Guid.NewGuid().ToString("N");
                var progress = new ClearingProgress(new RecordedProgressStore());
                progress.BankCompletion(f.Run, id); RefreshProgress(f, progress, id);
                var terminal = Get<Text>(f.Hud, "terminal");
                Expect(terminal.rectTransform.sizeDelta.x <= viewport.x - 28f
                    && terminal.rectTransform.sizeDelta.y <= viewport.y - 28f && terminal.fontSize >= 14,
                    "result rectangle extends outside a short/narrow recording canvas");
                Expect(terminal.text.Contains("R") && terminal.text.Contains("1") && terminal.text.Contains("2"),
                    "responsive result lost its available choices");
            }
        });
        test("completion shop rectangles never overlap visible corner labels on short or ultrawide canvases", () =>
        {
            foreach (Vector2 viewport in new[] { new Vector2(620, 320), new Vector2(960, 468), new Vector2(960, 540) })
            {
                foreach (bool banked in new[] { false, true })
                {
                    var f = new HudFixture();
                    ((RectTransform)Get<Transform>(f.Hud, "root")).rect = new Rect(0, 0, viewport.x, viewport.y);
                    Expect(f.Run.TryCompleteObjective(), "shop overlap setup rejected");
                    string id = Guid.NewGuid().ToString("N");
                    var progress = new ClearingProgress(new RecordedProgressStore());
                    if (banked) Expect(progress.BankCompletion(f.Run, id) == ProgressActionResult.Saved, "overlap bank setup rejected");
                    RefreshProgress(f, progress, id);
                    Text terminal = Get<Text>(f.Hud, "terminal");
                    Expect(!string.IsNullOrEmpty(terminal.text), "completed shop result missing");
                    Rect shop = RecordedLabelRect(terminal, viewport);
                    foreach (string name in new[] { "resources", "objective" })
                    {
                        Text corner = Get<Text>(f.Hud, name);
                        if (string.IsNullOrEmpty(corner.text)) continue;
                        Expect(!RectanglesOverlap(shop, RecordedLabelRect(corner, viewport)),
                            "completed shop overlaps visible " + name + " at " + viewport.x + "x" + viewport.y);
                    }
                }
            }
        });
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
        test("first refresh after disable cannot restore an old strike warning or impact", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick(); f.Refresh();
            Expect(f.Run.PlayerAttackActive && f.Impact().gameObject.activeSelf
                   && Get<Transform>(f.Enemies[0], "Warning").gameObject.activeSelf,
                   "disable setup is missing an attack warning or impact");
            Invoke(f.Runtime, "OnDisable"); f.Refresh();
            Expect(!Get<SpriteRenderer>(f.Runtime, "strike").gameObject.activeSelf,
                   "first refresh resurrected the interrupted player strike");
            foreach (object enemy in f.Enemies)
                Expect(!Get<Transform>(enemy, "Warning").gameObject.activeSelf
                       && !Get<Transform>(enemy, "Impact").gameObject.activeSelf,
                       "first refresh resurrected an interrupted warning or impact");
        });
        test("a fresh target takes no old player contact after disable and resume without input", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick();
            Invoke(f.Runtime, "OnDisable");
            // Inside the old strike but outside enemy attack initiation range.
            f.Position(1, new Vector2(.2f, -1.1f));
            int cues = f.Audio.OneShots.Count;
            f.Tick(); f.Refresh();
            Expect(Near(f.Run.GetSentinel(1).Health, ClearingRun.SentinelMaxHealth),
                   "resumed physics dealt stale player damage without a fresh attack input");
            Expect(f.Audio.OneShots.Count == cues && !f.Impact(1).gameObject.activeSelf,
                   "resumed physics emitted a stale contact cue or impact");
        });
        test("an interrupted enemy active window cannot hit after disable and resume", () =>
        {
            var f = new RuntimeFixture();
            f.Position(0, new Vector2(4f, 0f));
            Set(f.Enemies[0], "AttackCenter", Vector2.zero);
            Expect(f.Run.BeginSentinelAttack(0), "enemy swing setup rejected");
            f.Run.Advance(ClearingRun.TelegraphDuration);
            Expect(f.Run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Active, "setup is not an active enemy swing");
            Invoke(f.Runtime, "OnDisable");
            float health = f.Stats.Health;
            f.Tick(); f.Refresh();
            Expect(Near(f.Stats.Health, health), "resumed physics applied an interrupted enemy hit");
            Expect(!Get<Transform>(f.Enemies[0], "Warning").gameObject.activeSelf,
                   "interrupted active warning returned after resume");
        });
        test("disable preserves resources rewards cooldowns immunity and simulation time", () =>
        {
            var f = new RuntimeFixture(); LowerToLastHit(f.Run, 2);
            long token;
            Expect(f.Run.BeginPlayerAttack(PlayerAttackKind.Light, 12f, out token)
                   && f.Run.TryHitSentinel(token, 2), "reward setup failed");
            f.Run.Advance(ClearingRun.LightCooldown);
            Expect(f.Run.BeginSentinelAttack(0), "immunity swing setup rejected");
            f.Run.Advance(ClearingRun.TelegraphDuration);
            Expect(f.Run.TryResolveSentinelHit(0), "immunity setup rejected");
            f.Light(); f.Run.Advance(ClearingRun.LightAttackWindow); f.Burst();
            f.Stats.Health = 7f;
            float health = f.Stats.Health, mana = f.Stats.Mana;
            float light = f.Run.LightCooldownRemaining, burst = f.Run.BurstCooldownRemaining;
            float immunity = f.Run.PlayerInvulnerabilityRemaining;
            double time = f.Run.Time;
            int rewards = f.Run.RewardCoins, defeats = f.Run.DefeatedCount;
            float[] enemyHealth = new float[ClearingRun.SentinelCount];
            for (int i = 0; i < enemyHealth.Length; i++) enemyHealth[i] = f.Run.GetSentinel(i).Health;
            Expect(light > 0f && burst > 0f && immunity > 0f && defeats == 1, "preservation setup has no live timers or reward");
            Invoke(f.Runtime, "OnDisable"); f.Refresh();
            Expect(Near(f.Stats.Health, health) && Near(f.Stats.Mana, mana) && f.Run.Time == time,
                   "disable reset resources or advanced simulation");
            Expect(f.Run.RewardCoins == rewards && f.Run.DefeatedCount == defeats && !f.Run.IsDead && !f.Run.IsComplete,
                   "disable erased progression or ended the run");
            Expect(Near(light, f.Run.LightCooldownRemaining) && Near(burst, f.Run.BurstCooldownRemaining)
                   && Near(immunity, f.Run.PlayerInvulnerabilityRemaining), "disable refunded cooldown or immunity");
            for (int i = 0; i < enemyHealth.Length; i++)
                Expect(Near(enemyHealth[i], f.Run.GetSentinel(i).Health), "disable reset a sentinel health value");
            Expect(f.Run.GetSentinel(2).IsDead && !f.Run.PlayerAttackActive, "disable revived a defeated enemy or kept a player action");
        });
        test("disable keeps terminal movement locked and restores the prior pause time scale", () =>
        {
            foreach (bool complete in new[] { false, true })
            {
                var f = new RuntimeFixture();
                if (complete)
                {
                    for (int i = 0; i < ClearingRun.SentinelCount; i++)
                    {
                        LowerToLastHit(f.Run, i);
                        long token;
                        Expect(f.Run.BeginPlayerAttack(PlayerAttackKind.Light, 12f, out token)
                               && f.Run.TryHitSentinel(token, i), "completion defeat setup failed");
                        f.Run.Advance(ClearingRun.LightCooldown);
                    }
                    Expect(f.Run.TryCompleteObjective(), "completion setup rejected");
                }
                else { f.Stats.Health = 0f; f.Tick(); }
                Invoke(f.Runtime, "StopActors");
                Time.timeScale = .4f; Invoke(f.Runtime, "SetPaused", true);
                Invoke(f.Runtime, "OnDisable");
                Expect(!f.ControlsEnabled, "disabling a terminal run unlocked movement");
                Expect(Near(Time.timeScale, .4f) && !Get<bool>(f.Runtime, "paused"),
                       "disable did not restore the time scale saved before pause");
                Expect(complete ? f.Run.IsComplete && !f.Run.IsDead : f.Run.IsDead && !f.Run.IsComplete,
                       "disable changed the terminal result");
            }
        });
        test("pause retains the player action and its warning until simulation resumes", () =>
        {
            var f = new RuntimeFixture(); f.Light(); f.Tick(); f.Refresh();
            double time = f.Run.Time; float cooldown = f.Run.LightCooldownRemaining;
            float health = f.Run.GetSentinel(0).Health;
            Invoke(f.Runtime, "SetPaused", true);
            for (int i = 0; i < 10; i++) { f.Tick(); f.Refresh(); }
            Expect(f.Run.PlayerAttackActive && Get<SpriteRenderer>(f.Runtime, "strike").gameObject.activeSelf,
                   "pause canceled the player attack instead of freezing it");
            Expect(Get<Transform>(f.Enemies[0], "Warning").gameObject.activeSelf
                   && f.Run.Time == time && Near(cooldown, f.Run.LightCooldownRemaining),
                   "pause canceled a warning or advanced attack timers");
            Invoke(f.Runtime, "SetPaused", false); f.Tick();
            Expect(f.Run.Time > time && Near(health, f.Run.GetSentinel(0).Health),
                   "resume failed to advance or duplicated the frozen player contact");
        });
        for (int lethalId = 0; lethalId < ClearingRun.SentinelCount; lethalId++)
        {
            int attacker = lethalId;
            test("all same-tick burst contacts resolve before lethal enemy index " + attacker, () =>
            {
                var f = new RuntimeFixture();
                for (int i = 0; i < ClearingRun.SentinelCount; i++)
                    f.Position(i, new Vector2((i - 1) * .2f, -1.1f));
                f.Stats.Health = ClearingRun.SentinelDamage;
                Set(f.Enemies[attacker], "AttackCenter", Vector2.zero);
                Expect(f.Run.BeginSentinelAttack(attacker), "lethal swing setup rejected");
                f.Run.Advance(ClearingRun.TelegraphDuration - Time.fixedDeltaTime * .5f);
                f.Burst(); int swingCues = f.Audio.OneShots.Count;
                f.Tick(); f.Refresh();
                for (int i = 0; i < ClearingRun.SentinelCount; i++)
                    Expect(Near(f.Run.GetSentinel(i).Health, ClearingRun.SentinelMaxHealth - ClearingRun.BurstDamage),
                           "same admitted burst skipped target " + i + " when lethal enemy occupied index " + attacker);
                Expect(f.Run.IsDead && Near(f.Stats.Health, 0f) && f.Run.DefeatedCount == 0 && f.Run.RewardCoins == 0,
                       "lethal same-tick contact did not end with equivalent health and progression");
                Expect(f.Audio.OneShots.Count == swingCues + 1, "lethal tick produced duplicate hurt or terminal hit audio");
                Expect(!f.ControlsEnabled && !f.Run.PlayerAttackActive
                       && !Get<SpriteRenderer>(f.Runtime, "strike").gameObject.activeSelf,
                       "lethal tick retained player control or an action");
                foreach (object enemy in f.Enemies)
                    Expect(!Get<Transform>(enemy, "Warning").gameObject.activeSelf
                           && !Get<Transform>(enemy, "Impact").gameObject.activeSelf,
                           "lethal tick retained live warning or impact presentation");
                double time = f.Run.Time;
                f.Tick();
                Expect(f.Run.Time == time, "terminal tick continued the simulation");
            });
        }
        for (int activeId = 0; activeId < ClearingRun.SentinelCount; activeId++)
        {
            int attacker = activeId;
            test("same-tick burst defeats suppress retaliation and open gate with active enemy index " + attacker, () =>
            {
                var f = new RuntimeFixture();
                for (int i = 0; i < ClearingRun.SentinelCount; i++)
                {
                    LowerToLastHit(f.Run, i);
                    f.Position(i, new Vector2((i - 1) * .2f, -1.1f));
                }
                f.Stats.Health = ClearingRun.SentinelDamage;
                Set(f.Enemies[attacker], "AttackCenter", Vector2.zero);
                Expect(f.Run.BeginSentinelAttack(attacker), "finishing swing setup rejected");
                f.Run.Advance(ClearingRun.TelegraphDuration - Time.fixedDeltaTime * .5f);
                f.Burst(); int swingCues = f.Audio.OneShots.Count;
                f.Tick(); f.Refresh();
                Expect(Near(f.Stats.Health, ClearingRun.SentinelDamage) && !f.Run.IsDead && !f.Run.IsComplete,
                       "sentinel defeated by this tick's player contact still retaliated");
                Expect(f.Run.DefeatedCount == ClearingRun.SentinelCount
                       && f.Run.RewardCoins == ClearingRun.CoinsPerSentinel * ClearingRun.SentinelCount
                       && f.Run.GateUnlocked, "same-tick defeats lost rewards or gate progression");
                Expect(!Get<ClearingVisuals>(f.Runtime, "visuals").Gate.activeSelf,
                       "full same-tick clear did not remove the gate");
                for (int i = 0; i < ClearingRun.SentinelCount; i++)
                    Expect(f.Run.GetSentinel(i).IsDead && !Get<GameObject>(f.Enemies[i], "Object").activeSelf
                           && !Get<Transform>(f.Enemies[i], "Warning").gameObject.activeSelf
                           && f.Impact(i).gameObject.activeSelf,
                           "one same-tick defeat retained its actor/warning or lost kill feedback");
                Expect(f.Audio.OneShots.Count == swingCues + 2,
                       "one burst and gate unlock did not produce exactly one kill cue and one reward cue");
                f.Tick(); f.Refresh();
                Expect(f.Run.RewardCoins == ClearingRun.CoinsPerSentinel * ClearingRun.SentinelCount
                       && f.Audio.OneShots.Count == swingCues + 2, "repeated tick duplicated kill rewards or clear audio");
            });
        }
        AddProgressionChecks(test);
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; actual project C# with recording boundaries, not native Unity.");
        return failed == 0 ? 0 : 1;
    }
}
