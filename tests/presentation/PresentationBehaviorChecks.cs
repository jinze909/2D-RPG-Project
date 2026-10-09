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
            Physics2D.LinecastQuery = null;
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
        if (method == null || method.GetParameters().Length != 10)
            throw new Exception("Progression HUD Refresh signature absent");
        method.Invoke(f.Hud, new object[] { f.Stats, f.Run, paused, false, true, "", progress, rewardId, notice, "" });
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

    private static RuntimeFixture RoleFixture(int id, Vector2 enemyPoint, Vector2 playerPoint)
    {
        var f = new RuntimeFixture();
        for (int i = 0; i < f.Enemies.Length; i++) f.Position(i, new Vector2(20f + i, 20f));
        f.Position(id, enemyPoint);
        f.Body.position = playerPoint - ClearingVisuals.PlayerFootOffset;
        Invoke(f.Runtime, "ConfigureSentinelRole", f.Enemies[id], id);
        return f;
    }

    private static void EnterActive(RuntimeFixture f, int id)
    {
        SentinelState state = f.Run.GetSentinel(id);
        Expect(state.AttackPhase == SentinelAttackPhase.Telegraph, "actual tactical initiation did not telegraph");
        f.Run.Advance(ClearingRun.WindupDuration(state.AttackKind) - Time.fixedDeltaTime * .5f);
        f.Tick();
        Expect(state.AttackPhase == SentinelAttackPhase.Active, "actual runtime tick did not enter the role's active phase");
    }

    private static Transform NamedChild(Transform parent, string name)
    {
        foreach (Transform child in parent.Children)
            if (child.gameObject.name == name) return child;
        throw new Exception("Authored child absent: " + name);
    }

    private static void AddTacticsChecks(Action<string, Action> test)
    {
        test("actual role configuration admits each distinct attack in FixedUpdate", () =>
        {
            foreach (int id in new[] { 0, 1, 2 })
            {
                float distance = id == 0 ? .6f : id == 1 ? 2f : 3f;
                var f = RoleFixture(id, new Vector2(-distance, 0f), Vector2.zero);
                f.Tick();
                SentinelAttackKind expected = SentinelTactics.RoleForIndex(id);
                Expect(Get<SentinelAttackKind>(f.Enemies[id], "Kind") == expected
                    && f.Run.GetSentinel(id).AttackKind == expected
                    && f.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Telegraph,
                    "configured role did not govern real attack admission for index " + id);
                Expect(Get<SentinelFootprint>(f.Enemies[id], "Footprint") != null,
                    "actual initiated attack has no locked shared footprint");
            }
        });
        test("Lancer locks diagonal aim and holds position through charge active and recovery", () =>
        {
            var f = RoleFixture(1, new Vector2(-1.8f, -1.8f), Vector2.zero); f.Tick();
            object enemy = f.Enemies[1];
            var footprint = Get<SentinelFootprint>(enemy, "Footprint");
            Expect(Near(footprint.AngleDegrees, 45f) && Near(footprint.Width, 3.2f) && Near(footprint.Height, .7f),
                "diagonal Lancer did not lock a narrow directional lane");
            Vector2 origin = Get<Rigidbody2D>(enemy, "Body").position;
            f.Body.position = new Vector2(2f, -2f) - ClearingVisuals.PlayerFootOffset;
            f.Tick();
            Expect(object.ReferenceEquals(footprint, Get<SentinelFootprint>(enemy, "Footprint")),
                "Lancer retargeted after the player dodged");
            EnterActive(f, 1);
            f.Run.Advance(ClearingRun.ActiveDuration(SentinelAttackKind.Lance)); f.Tick();
            Expect(f.Run.GetSentinel(1).AttackPhase == SentinelAttackPhase.Recovery
                && Get<Rigidbody2D>(enemy, "Body").position == origin
                && Get<Rigidbody2D>(enemy, "Body").velocity == Vector2.zero,
                "stationary Lancer moved while committed or recovering");
        });
        test("actual narrow rotated lane rejects side dodges and accepts one later contact", () =>
        {
            foreach (bool diagonal in new[] { false, true })
            {
                Vector2 origin = diagonal ? new Vector2(-1.8f, -1.8f) : new Vector2(-2f, 0f);
                var f = RoleFixture(1, origin, Vector2.zero); f.Tick();
                var footprint = Get<SentinelFootprint>(f.Enemies[1], "Footprint");
                Vector2 center = new Vector2(footprint.CenterX, footprint.CenterY);
                Vector2 aim = (Vector2.zero - origin).normalized;
                Vector2 side = new Vector2(-aim.y, aim.x);
                f.Body.position = center + side * .4f - ClearingVisuals.PlayerFootOffset;
                int cues = f.Audio.OneShots.Count; EnterActive(f, 1);
                Expect(Near(f.Stats.Health, 10f) && f.Audio.OneShots.Count == cues,
                    "player beside the visible lane received damage or hurt audio");
                f.Body.position = center - ClearingVisuals.PlayerFootOffset; f.Tick(); f.Tick();
                Expect(Near(f.Stats.Health, 10f - ClearingRun.SentinelDamage) && f.Audio.OneShots.Count == cues + 1,
                    "one in-lane contact did not apply exactly one damage/audio event");
            }
        });
        test("locked attack still requires current wall line of sight before contact", () =>
        {
            var f = RoleFixture(1, new Vector2(-2f, 0f), Vector2.zero); f.Tick();
            int cues = f.Audio.OneShots.Count; Physics2D.LinecastResponse = new BoxCollider2D();
            try
            {
                EnterActive(f, 1);
                Expect(Near(f.Stats.Health, 10f) && f.Audio.OneShots.Count == cues,
                    "wall-blocked locked lane dealt damage or sounded a hit");
                Physics2D.LinecastResponse = null; f.Tick();
                Expect(Near(f.Stats.Health, 8f) && f.Audio.OneShots.Count == cues + 1,
                    "unblocked valid contact was prematurely consumed by the wall rejection");
            }
            finally { Physics2D.LinecastResponse = null; }
        });
        test("Seer retreats when crowded and casts a locked mark when retreat is obstructed", () =>
        {
            var retreat = RoleFixture(2, new Vector2(1f, 0f), Vector2.zero); retreat.Tick();
            Expect(Get<Rigidbody2D>(retreat.Enemies[2], "Body").position.x > 1f
                && retreat.Run.GetSentinel(2).AttackPhase == SentinelAttackPhase.Ready
                && Get<SentinelFootprint>(retreat.Enemies[2], "Footprint") == null,
                "crowded Seer did not request retreat before attacking");
            var f = RoleFixture(2, new Vector2(1f, 0f), Vector2.zero);
            Physics2D.LinecastQuery = (start, end, mask) => end.x > start.x ? new BoxCollider2D() : null;
            try
            {
                f.Tick(); var footprint = Get<SentinelFootprint>(f.Enemies[2], "Footprint");
                Expect(f.Run.GetSentinel(2).AttackPhase == SentinelAttackPhase.Telegraph && footprint != null
                    && Near(footprint.CenterX, 0f) && Near(footprint.CenterY, 0f)
                    && Near(footprint.Width, 1.4f) && Get<Rigidbody2D>(f.Enemies[2], "Body").position == new Vector2(1f, 0f),
                    "wall-blocked retreat left the Seer inert instead of marking the locked player point");
                f.Body.position = new Vector2(.8f, 0f) - ClearingVisuals.PlayerFootOffset;
                EnterActive(f, 2); f.Refresh();
                Expect(Near(f.Stats.Health, 10f) && object.ReferenceEquals(footprint, Get<SentinelFootprint>(f.Enemies[2], "Footprint")),
                    "Seer mark followed a dodge or damaged a player outside its marked square");
                f.Body.position = new Vector2(.5f, 0f) - ClearingVisuals.PlayerFootOffset; f.Tick();
                Expect(Near(f.Stats.Health, 8f), "valid later contact inside the locked Seer mark dealt no damage");
            }
            finally { Physics2D.LinecastQuery = null; }
        });
        test("zero-distance Seer casts instead of retreating forever on an empty direction", () =>
        {
            var f = RoleFixture(2, Vector2.zero, Vector2.zero); f.Tick();
            Expect(f.Run.GetSentinel(2).AttackPhase == SentinelAttackPhase.Telegraph
                && Get<SentinelFootprint>(f.Enemies[2], "Footprint") != null,
                "coincident player and Seer became permanently inert with zero retreat displacement");
        });
        test("tactical approach obeys awareness range and wall admission", () =>
        {
            foreach (int id in new[] { 1, 2 })
            {
                var f = RoleFixture(id, new Vector2(-3.5f, 0f), Vector2.zero); f.Tick();
                Expect(Get<Rigidbody2D>(f.Enemies[id], "Body").position.x > -3.5f
                    && f.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Ready,
                    "visible out-of-cast-range enemy did not approach");
                var blocked = RoleFixture(id, new Vector2(-2.5f, 0f), Vector2.zero);
                Physics2D.LinecastResponse = new BoxCollider2D();
                try
                {
                    blocked.Tick();
                    Expect(Get<Rigidbody2D>(blocked.Enemies[id], "Body").position == new Vector2(-2.5f, 0f)
                        && blocked.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Ready
                        && Get<SentinelFootprint>(blocked.Enemies[id], "Footprint") == null,
                        "wall-obscured target caused movement or a new warning");
                }
                finally { Physics2D.LinecastResponse = null; }
                var distant = RoleFixture(id, new Vector2(-7f, 0f), Vector2.zero); distant.Tick();
                Expect(Get<Rigidbody2D>(distant.Enemies[id], "Body").position.x == -7f
                    && distant.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Ready,
                    "out-of-awareness enemy pursued or attacked");
            }
        });
        test("warning center angle and charge dimensions use the same locked tactical footprint", () =>
        {
            foreach (int id in new[] { 0, 1, 2 })
            {
                Vector2 origin = id == 0 ? new Vector2(-.6f, 0f) : id == 1 ? new Vector2(-1.8f, -1.8f) : new Vector2(-3f, 0f);
                var f = RoleFixture(id, origin, Vector2.zero); f.Tick(); f.Run.Advance(.2f); f.Refresh();
                object enemy = f.Enemies[id]; var footprint = Get<SentinelFootprint>(enemy, "Footprint");
                Transform warning = Get<Transform>(enemy, "Warning");
                Transform fill = Get<SpriteRenderer>(enemy, "WarningFill").transform;
                float charge = f.Run.GetSentinel(id).PhaseProgress;
                Expect((Vector2)warning.position == new Vector2(footprint.CenterX, footprint.CenterY)
                    && Near(warning.rotation.RecordedEulerZ, footprint.AngleDegrees)
                    && Near(warning.localScale.x, 1f) && Near(warning.localScale.y, 1f),
                    "warning outer pose or scale disagrees with its locked contact footprint");
                Expect(Near(fill.localScale.x, footprint.Width * charge) && Near(fill.localScale.y, footprint.Height * charge),
                    "charge fill dimensions do not use the role's actual contact bounds");
                f.Run.Advance(ClearingRun.WindupDuration(f.Run.GetSentinel(id).AttackKind) - .2f); f.Refresh();
                Expect(f.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Active
                    && Near(fill.localScale.x, footprint.Width) && Near(fill.localScale.y, footprint.Height),
                    "active warning does not fill exactly the saved footprint");
                foreach (string crossName in new[] { "WarningCrossA", "WarningCrossB" })
                {
                    var cross = Get<SpriteRenderer>(enemy, crossName);
                    Expect(cross.gameObject.activeSelf && cross.transform.localScale.x <= Math.Min(footprint.Width, footprint.Height),
                        "active non-color motif exceeds the role's narrowest contact dimension");
                }
            }
        });
        test("a timing hitch skips new role damage and leaves recovery stationary", () =>
        {
            foreach (int id in new[] { 1, 2 })
            {
                var f = RoleFixture(id, new Vector2(id == 1 ? -2f : -3f, 0f), Vector2.zero); f.Tick();
                Vector2 origin = Get<Rigidbody2D>(f.Enemies[id], "Body").position;
                SentinelAttackKind kind = f.Run.GetSentinel(id).AttackKind;
                int cues = f.Audio.OneShots.Count;
                f.Run.Advance(ClearingRun.WindupDuration(kind) + ClearingRun.ActiveDuration(kind) + .05f); f.Tick(); f.Refresh();
                Expect(f.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Recovery && Near(f.Stats.Health, 10f)
                    && f.Audio.OneShots.Count == cues && Get<Rigidbody2D>(f.Enemies[id], "Body").position == origin
                    && !Get<Transform>(f.Enemies[id], "Warning").gameObject.activeSelf,
                    "hitch applied a delayed hit, chased during recovery or retained an active warning");
            }
        });
        test("pause freezes locked role snapshots and resumes their same timing", () =>
        {
            foreach (int id in new[] { 1, 2 })
            {
                var f = RoleFixture(id, new Vector2(id == 1 ? -2f : -3f, 0f), Vector2.zero); f.Tick(); f.Refresh();
                var footprint = Get<SentinelFootprint>(f.Enemies[id], "Footprint"); double time = f.Run.Time;
                Invoke(f.Runtime, "SetPaused", true);
                for (int tick = 0; tick < 8; tick++) { f.Tick(); f.Refresh(); }
                Expect(f.Run.Time == time && object.ReferenceEquals(footprint, Get<SentinelFootprint>(f.Enemies[id], "Footprint"))
                    && Get<Transform>(f.Enemies[id], "Warning").gameObject.activeSelf,
                    "pause advanced, replaced or cleared an in-flight role warning");
                Invoke(f.Runtime, "SetPaused", false); f.Tick();
                Expect(f.Run.Time > time && object.ReferenceEquals(footprint, Get<SentinelFootprint>(f.Enemies[id], "Footprint")),
                    "resume did not retain and advance the locked role attack");
            }
        });
        test("disable death and retry clear tactical footprints without losing role assignment", () =>
        {
            foreach (int id in new[] { 1, 2 }) foreach (int ending in new[] { 0, 1, 2 })
            {
                var f = RoleFixture(id, new Vector2(id == 1 ? -2f : -3f, 0f), Vector2.zero); f.Tick();
                Expect(Get<SentinelFootprint>(f.Enemies[id], "Footprint") != null, "cleanup setup has no actual footprint");
                if (ending == 0) Invoke(f.Runtime, "OnDisable");
                if (ending == 1) { f.Stats.Health = 0f; f.Tick(); }
                if (ending == 2) Invoke(f.Runtime, "ResetRun");
                f.Refresh();
                Expect(Get<SentinelFootprint>(f.Enemies[id], "Footprint") == null
                    && !Get<Transform>(f.Enemies[id], "Warning").gameObject.activeSelf
                    && Get<SentinelAttackKind>(f.Enemies[id], "Kind") == SentinelTactics.RoleForIndex(id),
                    "terminal/interruption cleanup retained a snapshot or lost its configured role");
                if (ending == 0)
                {
                    f.Position(id, new Vector2(-10f, 0f)); f.Tick();
                    Expect(Near(f.Stats.Health, 10f) && Get<SentinelFootprint>(f.Enemies[id], "Footprint") == null,
                        "resumed role applied a stale contact without fresh admission");
                }
            }
        });
        test("new role defeats still resolve all burst targets before active retaliation", () =>
        {
            foreach (int id in new[] { 1, 2 })
            {
                var f = new RuntimeFixture();
                for (int i = 0; i < f.Enemies.Length; i++)
                {
                    LowerToLastHit(f.Run, i); f.Position(i, new Vector2((i - 1) * .2f, -1.1f));
                }
                Invoke(f.Runtime, "ConfigureSentinelRole", f.Enemies[id], id);
                Physics2D.LinecastQuery = (start, end, mask) => end.y < start.y ? new BoxCollider2D() : null;
                try
                {
                    f.Tick(); Expect(f.Run.GetSentinel(id).AttackPhase == SentinelAttackPhase.Telegraph, "role defeat setup did not begin an attack");
                    f.Run.Advance(ClearingRun.WindupDuration(f.Run.GetSentinel(id).AttackKind) - Time.fixedDeltaTime * .5f);
                    // Player strike LOS differs from the Seer's backward retreat probe.
                    Physics2D.LinecastQuery = null;
                    f.Stats.Health = ClearingRun.SentinelDamage; f.Burst(); f.Tick(); f.Refresh();
                    Expect(!f.Run.IsDead && Near(f.Stats.Health, ClearingRun.SentinelDamage)
                        && f.Run.DefeatedCount == 3 && f.Run.RewardCoins == 30 && f.Run.GateUnlocked,
                        "new active role retaliated after the same burst defeated every target");
                }
                finally { Physics2D.LinecastQuery = null; }
            }
        });
        test("actual sentinel creation records distinct markers and role-sized four-border warnings", () =>
        {
            var visuals = new ClearingVisuals(new GameObject("Role visual fixture").transform);
            try
            {
                foreach (int id in new[] { 0, 1, 2 })
                {
                    SpriteRenderer eye, health; Transform warning;
                    GameObject actor = visuals.CreateSentinel(id, Vector2.zero, out eye, out health, out warning);
                    NamedChild(actor.transform, id == 0 ? "Warden crest" : id == 1 ? "Lancer side spear" : "Seer rune");
                    float width = id == 0 ? 1.9f : id == 1 ? 3.2f : 1.4f;
                    float height = id == 1 ? .7f : width;
                    foreach (string name in new[] { "North warning", "South warning" })
                    {
                        Transform border = NamedChild(warning, name);
                        Expect(Near(border.localScale.x, width) && Near(border.localScale.y, .067f)
                            && Near(Math.Abs(border.localPosition.y), height * .5f), "horizontal role warning border disagrees with contact bounds");
                    }
                    foreach (string name in new[] { "West warning", "East warning" })
                    {
                        Transform border = NamedChild(warning, name);
                        Expect(Near(border.localScale.x, .067f) && Near(border.localScale.y, height)
                            && Near(Math.Abs(border.localPosition.x), width * .5f), "vertical role warning border disagrees with contact bounds");
                    }
                }
            }
            finally { visuals.Dispose(); }
        });
        test("live role guidance shares the existing edge help slot and yields to active feedback", () =>
        {
            foreach (float width in new[] { 620f, 960f, 1280f })
            {
                var f = new HudFixture(); f.Width(width); f.Refresh(near: false);
                Expect(f.Help.text.Contains("Lancer: sidestep lane") && f.Help.text.Contains("Seer: leave mark"),
                    "live edge help omits the actual new threat responses");
                Rect rect = RecordedLabelRect(f.Help, new Vector2(width, 540f));
                Expect(rect.y + rect.height <= 81f && f.Help.rectTransform.sizeDelta == f.Message.rectTransform.sizeDelta,
                    "role guidance extends into the south combat band or drifts from its shared slot");
                f.Refresh(near: false, feedback: "Accepted contact");
                Expect(f.Help.text == "" && f.Message.text == "Accepted contact", "role guide overlaps transient feedback");
                f.Refresh(near: true);
                Expect(f.Help.text == "" && f.Message.text.Contains("E -"), "role guide hides actionable beacon interaction");
            }
        });
        test("role guide advertises no unavailable actions in paused dead or completed results", () =>
        {
            foreach (int state in new[] { 0, 1, 2 })
            {
                var f = new HudFixture();
                if (state == 1) f.Run.NotifyPlayerDeath();
                if (state == 2) Expect(f.Run.TryCompleteObjective(), "role guide completion setup rejected");
                f.Refresh(paused: state == 0, near: false);
                Expect(string.IsNullOrEmpty(f.Help.text) && string.IsNullOrEmpty(f.Message.text)
                    && !string.IsNullOrEmpty(Get<Text>(f.Hud, "terminal").text),
                    "result retains live role guides or hides the result prompt");
            }
        });
    }

    private sealed class DamageFixture
    {
        internal readonly RuntimeFixture F = new RuntimeFixture();
        internal readonly ClearingDamageNumbers Numbers;
        internal DamageFixture()
        {
            if (Field(F.Runtime, "damageNumbers") == null) throw new Exception("Runtime damage-number integration absent");
            Numbers = new ClearingDamageNumbers(F.Runtime.transform);
            Set(F.Runtime, "damageNumbers", Numbers);
        }
        internal object Slot(int index) { return Get<Array>(Numbers, "slots").GetValue(index); }
        internal Transform Root(int index) { return Get<Transform>(Slot(index), "Root"); }
        internal string Text(int index) { return Get<string>(Slot(index), "Text"); }
        internal float Amount(int index) { return Get<float>(Slot(index), "Amount"); }
        internal void AssertCleared()
        {
            for (int i = 0; i < 4; i++) Expect(!Root(i).gameObject.activeSelf && string.IsNullOrEmpty(Text(i)) && Near(Amount(i), 0f),
                "terminal/interruption retained damage snapshot in slot " + i);
        }
    }

    private static void SetupSentinelLoss(ClearingRun run, int target, PlayerAttackKind kind)
    {
        long token;
        Expect(run.BeginPlayerAttack(kind, 12f, out token) && run.TryHitSentinel(token, target), "capped-damage setup rejected");
        run.Advance(kind == PlayerAttackKind.Light ? ClearingRun.LightCooldown : ClearingRun.BurstCooldown);
    }

    private static void AddDamageNumberChecks(Action<string, Action> test)
    {
        test("actual accepted light and burst contacts show signed losses with matching feedback colors", () =>
        {
            foreach (bool burst in new[] { false, true })
            {
                var p = new DamageFixture();
                if (burst) p.F.Burst(); else p.F.Light();
                p.F.Tick(); p.F.Refresh();
                float expected = burst ? 30f : 18f; Color color = burst ? ClearingPalette.CyanBright : ClearingPalette.Cream;
                Color shown = Get<Color>(p.Slot(0), "Color");
                Expect(p.Root(0).gameObject.activeSelf && Near(p.Amount(0), expected) && p.Text(0) == (burst ? "-30" : "-18"),
                    "accepted contact has no exact signed numeric feedback");
                Expect(Near(shown.r, color.r) && Near(shown.g, color.g) && Near(shown.b, color.b),
                    "attack kind and numeric feedback colors disagree");
                Expect(p.Root(0).parent == p.F.Runtime.transform && Get<Vector2>(p.Slot(0), "Origin") == new Vector2(0f, 1.1f),
                    "enemy readout is attached to the actor or not anchored above the captured hit body");
            }
        });
        test("lethal enemy numbers show actual remaining six or twenty-four HP instead of nominal damage", () =>
        {
            foreach (bool burst in new[] { false, true })
            {
                var p = new DamageFixture(); SetupSentinelLoss(p.F.Run, 0, PlayerAttackKind.Burst);
                if (!burst) SetupSentinelLoss(p.F.Run, 0, PlayerAttackKind.Light);
                float remaining = p.F.Run.GetSentinel(0).Health;
                if (burst) p.F.Burst(); else p.F.Light();
                p.F.Tick(); p.F.Refresh();
                Expect(p.F.Run.GetSentinel(0).IsDead && !Get<GameObject>(p.F.Enemies[0], "Object").activeSelf,
                    "clamped readout setup did not actually defeat the actor");
                Expect(Near(remaining, burst ? 24f : 6f) && Near(p.Amount(0), remaining)
                    && p.Text(0) == (burst ? "-24" : "-6") && p.Root(0).gameObject.activeSelf,
                    "lethal feedback inflated damage or disappeared with the hidden enemy");
            }
        });
        test("accepted player loss uses a minus sign and shared immunity admits one hurt readout", () =>
        {
            var p = new DamageFixture();
            for (int id = 0; id < 2; id++)
            {
                p.F.Position(id, new Vector2(10f + id, 0f)); Set(p.F.Enemies[id], "AttackCenter", Vector2.zero);
                Expect(p.F.Run.BeginSentinelAttack(id), "hurt feedback setup rejected");
            }
            p.F.Run.Advance(ClearingRun.TelegraphDuration - Time.fixedDeltaTime * .5f); p.F.Tick(); p.F.Refresh();
            Color color = Get<Color>(p.Slot(3), "Color");
            Expect(Near(p.F.Stats.Health, 8f) && p.Text(3) == "-2" && Near(p.Amount(3), 2f)
                && p.Root(3).gameObject.activeSelf && Near(color.r, ClearingPalette.Danger.r),
                "accepted hurt lacks signed danger feedback or multiple simultaneous swings bypassed immunity");
            Expect(Get<Vector2>(p.Slot(3), "Origin") == new Vector2(0f, 2.35f), "player hurt readout was not anchored above the captured player point");
            double started = Get<double>(p.Slot(3), "StartedAt"); p.F.Tick(); p.F.Refresh();
            Expect(Near(p.F.Stats.Health, 8f) && Get<double>(p.Slot(3), "StartedAt") == started,
                "repeated or immunity-rejected enemy swing replaced the accepted hurt readout");
            var fatal = new DamageFixture(); fatal.F.Stats.Health = 1f; fatal.F.Position(0, new Vector2(10f, 0f));
            Set(fatal.F.Enemies[0], "AttackCenter", Vector2.zero); Expect(fatal.F.Run.BeginSentinelAttack(0), "fatal setup rejected");
            fatal.F.Run.Advance(ClearingRun.TelegraphDuration - Time.fixedDeltaTime * .5f); fatal.F.Tick(); fatal.F.Refresh();
            Expect(fatal.F.Run.IsDead && Near(fatal.F.Stats.Health, 0f), "fatal hurt did not synchronize death"); fatal.AssertCleared();
        });
        test("missed expired wall-rejected and repeated player contacts create no new damage readouts", () =>
        {
            foreach (int rejection in new[] { 0, 1, 2 })
            {
                var p = new DamageFixture();
                if (rejection == 0) p.F.Position(0, new Vector2(4f, 0f));
                p.F.Light(); if (rejection == 1) p.F.Run.Advance(.3f);
                if (rejection == 2) Physics2D.LinecastResponse = new BoxCollider2D();
                try { p.F.Tick(); p.F.Refresh(); p.AssertCleared(); }
                finally { Physics2D.LinecastResponse = null; }
            }
            var accepted = new DamageFixture(); accepted.F.Light(); accepted.F.Tick(); accepted.F.Refresh();
            double started = Get<double>(accepted.Slot(0), "StartedAt"); float amount = accepted.Amount(0);
            accepted.F.Tick(); accepted.F.Refresh();
            Expect(Get<double>(accepted.Slot(0), "StartedAt") == started && Near(accepted.Amount(0), amount)
                && Near(accepted.F.Run.GetSentinel(0).Health, 36f), "one attack token refreshed or duplicated its readout");
        });
        test("multi-target readouts reuse separate slots without new objects textures or sprites", () =>
        {
            var p = new DamageFixture(); p.F.Position(1, new Vector2(.2f, -.6f));
            int objects = GameObject.CreatedCount, textures = Texture2D.CreatedCount, sprites = Sprite.CreatedCount;
            object slot0 = p.Slot(0), slot1 = p.Slot(1); var glyphs = Get<Sprite[]>(p.Numbers, "glyphSprites");
            p.F.Burst(); p.F.Tick(); p.F.Refresh(); p.F.Tick(); p.F.Refresh();
            Expect(p.Text(0) == "-30" && p.Text(1) == "-30" && p.Root(0).gameObject.activeSelf && p.Root(1).gameObject.activeSelf
                && !p.Root(2).gameObject.activeSelf && !p.Root(3).gameObject.activeSelf,
                "one burst did not produce two distinct bounded target labels");
            Expect(object.ReferenceEquals(slot0, p.Slot(0)) && object.ReferenceEquals(slot1, p.Slot(1))
                && object.ReferenceEquals(glyphs, Get<Sprite[]>(p.Numbers, "glyphSprites"))
                && GameObject.CreatedCount == objects && Texture2D.CreatedCount == textures && Sprite.CreatedCount == sprites,
                "accepted/repeated hits replaced slots or allocated presentation resources after construction");
        });
        test("kill numbers retain captured origin then rise fade and expire on simulation time", () =>
        {
            var p = new DamageFixture(); LowerToLastHit(p.F.Run, 0); p.F.Light(); p.F.Tick(); p.F.Refresh();
            Vector2 origin = Get<Vector2>(p.Slot(0), "Origin"); p.F.Position(0, new Vector2(10f, 10f));
            p.F.Run.Advance(.15f); p.F.Refresh();
            SpriteRenderer[] renderers = Get<SpriteRenderer[]>(p.Slot(0), "Renderers");
            Expect(p.Root(0).gameObject.activeSelf && Near(p.Root(0).position.x, origin.x)
                && Near(p.Root(0).position.y, origin.y + .35f * .25f)
                && Near(renderers[0].color.a, .84375f),
                "kill readout followed the hidden body or used incorrect rise/fade timing");
            p.F.Run.Advance(.46f); p.F.Refresh();
            Expect(!p.Root(0).gameObject.activeSelf && string.IsNullOrEmpty(p.Text(0)), "expired damage number stayed visible");
        });
        test("pause freezes numeric position and opacity until simulation resumes", () =>
        {
            var p = new DamageFixture(); p.F.Light(); p.F.Tick(); p.F.Refresh(); p.F.Run.Advance(.1f); p.F.Refresh();
            Vector2 position = p.Root(0).position; float alpha = Get<SpriteRenderer[]>(p.Slot(0), "Renderers")[0].color.a;
            double time = p.F.Run.Time; Invoke(p.F.Runtime, "SetPaused", true); Time.unscaledTime += 50f;
            for (int tick = 0; tick < 8; tick++) { p.F.Tick(); p.F.Refresh(); }
            Expect(p.F.Run.Time == time && p.Root(0).gameObject.activeSelf && (Vector2)p.Root(0).position == position
                && Near(Get<SpriteRenderer[]>(p.Slot(0), "Renderers")[0].color.a, alpha), "pause advanced or erased an active numeric readout");
            Invoke(p.F.Runtime, "SetPaused", false); p.F.Run.Advance(.6f); p.F.Refresh();
            Expect(!p.Root(0).gameObject.activeSelf, "resumed simulation retained the expired readout");
        });
        test("a new accepted hit replaces the same bounded target slot and clock", () =>
        {
            var p = new DamageFixture(); p.F.Light(); p.F.Tick(); p.F.Refresh();
            object slot = p.Slot(0); Transform root = p.Root(0); SpriteRenderer[] renderers = Get<SpriteRenderer[]>(slot, "Renderers");
            double started = Get<double>(slot, "StartedAt"); p.F.Run.Advance(ClearingRun.LightCooldown);
            int objects = GameObject.CreatedCount, textures = Texture2D.CreatedCount, sprites = Sprite.CreatedCount;
            p.F.Burst(); p.F.Tick(); p.F.Refresh();
            Expect(object.ReferenceEquals(slot, p.Slot(0)) && object.ReferenceEquals(root, p.Root(0))
                && object.ReferenceEquals(renderers, Get<SpriteRenderer[]>(slot, "Renderers"))
                && p.Text(0) == "-30" && Get<double>(slot, "StartedAt") > started
                && GameObject.CreatedCount == objects && Texture2D.CreatedCount == textures && Sprite.CreatedCount == sprites,
                "later accepted hit accumulated resources or failed to replace the target snapshot");
        });
        test("disable death retry and actual beacon completion clear all numeric snapshots", () =>
        {
            foreach (int ending in new[] { 0, 1, 2 })
            {
                var p = new DamageFixture(); p.F.Light(); p.F.Tick(); p.F.Refresh();
                Expect(p.Root(0).gameObject.activeSelf, "cleanup setup has no accepted readout");
                if (ending == 0) Invoke(p.F.Runtime, "OnDisable");
                if (ending == 1) { p.F.Stats.Health = 0f; p.F.Tick(); }
                if (ending == 2) Invoke(p.F.Runtime, "ResetRun");
                p.F.Refresh(); p.AssertCleared();
            }
            var completed = new DamageFixture();
            for (int id = 0; id < 2; id++)
            {
                LowerToLastHit(completed.F.Run, id); SetupSentinelLoss(completed.F.Run, id, PlayerAttackKind.Light);
            }
            LowerToLastHit(completed.F.Run, 2); completed.F.Position(2, new Vector2(0f, -.6f));
            completed.F.Light(); completed.F.Tick(); completed.F.Refresh();
            Expect(completed.F.Run.GateUnlocked && completed.Root(2).gameObject.activeSelf, "completion setup lacks a final kill readout");
            Set(completed.F.Runtime, "hud", new ClearingHud(completed.F.Runtime.transform));
            completed.F.Body.position = ClearingVisuals.BeaconPosition - ClearingVisuals.PlayerFootOffset;
            Input.SetPressed(KeyCode.E);
            try { Invoke(completed.F.Runtime, "Update"); }
            finally { Input.ClearPressed(); }
            Expect(completed.F.Run.IsComplete, "actual E beacon did not complete the terminal cleanup fixture"); completed.AssertCleared();
        });
        test("cached numeric pool rejects invalid input and disposal prevents resurrection", () =>
        {
            var numbers = new ClearingDamageNumbers(new GameObject("Independent numeric pool").transform);
            numbers.Show(0, 18f, Vector2.zero, ClearingPalette.Cream, 0d);
            object slot = Get<Array>(numbers, "slots").GetValue(0);
            numbers.Show(-1, 30f, Vector2.zero, ClearingPalette.Cream, 0d);
            numbers.Show(4, 30f, Vector2.zero, ClearingPalette.Cream, 0d);
            foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) numbers.Show(0, amount, Vector2.zero, ClearingPalette.Cream, 0d);
            numbers.Show(0, 30f, new Vector2(float.NaN, 0f), ClearingPalette.Cream, 0d);
            numbers.Show(0, 30f, Vector2.zero, ClearingPalette.Cream, double.NaN);
            Expect(Get<string>(slot, "Text") == "-18" && Near(Get<float>(slot, "Amount"), 18f), "invalid feedback request overwrote the valid bounded snapshot");
            Sprite[] sprites = Get<Sprite[]>(numbers, "glyphSprites"); Texture2D[] textures = Get<Texture2D[]>(numbers, "glyphTextures");
            numbers.Dispose(); numbers.Dispose(); numbers.Show(0, 30f, Vector2.zero, ClearingPalette.Cream, 0d); numbers.Refresh(0d);
            Expect(!Get<Transform>(slot, "Root").gameObject.activeSelf, "disposed pool resurrected its numeric root");
            foreach (Sprite sprite in sprites) Expect(sprite == null || sprite.Destroyed, "dispose did not request cached glyph sprite destruction");
            foreach (Texture2D texture in textures) Expect(texture == null || texture.Destroyed, "dispose did not request cached glyph texture destruction");
        });
    }


    // Discovery, E input, actor resources and props are actual project C#.
    // Physics visibility, key presses and hierarchy writes are recorded only;
    // these checks establish integration, not native traversal or rendering.
    private sealed class SupplyFixture
    {
        internal readonly ProgressFixture P = new ProgressFixture();
        internal readonly ClearingSupplyVisuals Props;
        internal ClearingSupplies Supplies { get { return Get<ClearingSupplies>(P.F.Runtime, "supplies"); } }
        internal SupplyFixture()
        {
            Props = new ClearingSupplyVisuals(P.F.Runtime.transform, Get<ClearingVisuals>(P.F.Runtime, "visuals"));
            Set(P.F.Runtime, "supplyVisuals", Props);
        }
        internal static Vector2 Point(int index)
        {
            return new Vector2(ClearingSupplies.PositionX(index), ClearingSupplies.PositionY(index));
        }
        internal void Feet(int index, Vector2 offset)
        {
            P.F.Body.position = Point(index) + offset - ClearingVisuals.PlayerFootOffset;
        }
        internal GameObject Prop(int index) { return Get<GameObject[]>(Props, "points")[index]; }
        internal SpriteRenderer Glyph(int index) { return Get<SpriteRenderer[][]>(Props, "glyphs")[index][0]; }
        internal SpriteRenderer Empty(int index) { return Get<SpriteRenderer[]>(Props, "emptyMarks")[index]; }
        internal SpriteRenderer Pulse(int index) { return Get<SpriteRenderer[][]>(Props, "pulsePieces")[index][0]; }
    }

    private static void NoSupplyColliders(Transform root)
    {
        Expect(root.gameObject.GetComponent<BoxCollider2D>() == null
            && root.gameObject.GetComponent<CircleCollider2D>() == null
            && root.gameObject.GetComponent<CapsuleCollider2D>() == null
            && root.gameObject.GetComponent<Rigidbody2D>() == null,
            "a decorative supply changed world collisions");
        foreach (Transform child in root.Children) NoSupplyColliders(child);
    }

    private static void AddSupplyChecks(Action<string, Action> test)
    {
        test("supplies discover from actor feet inside range and recorded Default-wall visibility", () =>
        {
            var f = new SupplyFixture();
            f.P.Update();
            Expect(!f.Supplies.IsDiscovered(0) && !f.Supplies.IsDiscovered(1),
                "supply was discovered from the distant spawn");
            f.Feet(0, new Vector2(ClearingSupplies.DiscoveryRadius + .01f, 0f));
            f.P.Update();
            Expect(!f.Supplies.IsDiscovered(0), "outside discovery radius revealed herbs");
            f.Feet(0, new Vector2(ClearingSupplies.DiscoveryRadius - .01f, 0f));
            Vector2 feet = f.P.F.Body.position + ClearingVisuals.PlayerFootOffset;
            bool observed = false;
            Physics2D.LinecastQuery = (start, end, mask) =>
            {
                if (end == SupplyFixture.Point(0))
                {
                    Expect(start == feet && mask == 1,
                        "supply visibility used actor center or a non-wall layer mask");
                    observed = true;
                }
                return null;
            };
            try { f.P.Update(); }
            finally { Physics2D.LinecastQuery = null; }
            Expect(observed && f.Supplies.IsDiscovered(0) && !f.Supplies.IsDiscovered(1)
                && !f.Supplies.IsClaimed(0), "visible discovery consumed or revealed both supplies");
        });
        test("actual E respects foot interaction radius and restores only the actor-owned health clone once", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Health = 5f;
            int cues = f.P.F.Audio.OneShots.Count;
            // With centered Viola frames, standing body center on the prop
            // leaves the actual feet 1.1 units away and outside the use radius.
            f.P.F.Body.position = SupplyFixture.Point(0);
            f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsDiscovered(0) && !f.Supplies.IsClaimed(0)
                && Near(f.P.Stats.Health, 5f) && f.P.F.Audio.OneShots.Count == cues,
                "body-center proximity bypassed the foot interaction radius");
            f.Feet(0, Vector2.zero); f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(0) && Near(f.P.Stats.Health, 9f)
                && Near(f.P.Stats.Mana, 12f) && f.P.F.Audio.OneShots.Count == cues + 1,
                "E did not apply one four-health gain and one accepted reward cue");
            string accepted = Get<Text>(f.P.Hud, "message").text.ToLowerInvariant();
            Expect(accepted.Contains("+4") && accepted.Contains("hp"), "accepted health gain was hidden by a spent-supply hint");
            f.P.Stats.Health = 1f; f.P.Update(KeyCode.E); f.P.Update(KeyCode.E);
            Expect(Near(f.P.Stats.Health, 1f) && f.P.F.Audio.OneShots.Count == cues + 1
                && f.P.Store.Attempts == 0 && f.P.Progress.Data.BankCoins == 0,
                "spent herbs replayed healing/audio or awarded persistent coins");
            f.P.AssertTemplate();
        });
        test("actual rune E restores capped mana without health coins or template changes", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Health = 7f; f.P.Stats.Mana = 10f;
            f.Feet(1, Vector2.zero); int cues = f.P.F.Audio.OneShots.Count;
            f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(1) && !f.Supplies.IsClaimed(0)
                && Near(f.P.Stats.Mana, 12f) && Near(f.P.Stats.Health, 7f)
                && f.P.F.Audio.OneShots.Count == cues + 1,
                "rune did not restore the actual two-mana deficit once");
            string accepted = Get<Text>(f.P.Hud, "message").text.ToLowerInvariant();
            Expect(accepted.Contains("+2") && accepted.Contains("mp"), "actual capped mana gain was hidden by its spent hint");
            f.P.Stats.Mana = 0f; f.P.Update(KeyCode.E);
            Expect(Near(f.P.Stats.Mana, 0f) && f.P.F.Audio.OneShots.Count == cues + 1
                && f.P.Store.Attempts == 0 && f.P.Progress.Data.ClearedRuns == 0,
                "spent rune restored again or wrote progression");
            f.P.AssertTemplate();
        });
        test("a discovered rune supplies exactly one admitted burst through real E then K input", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Mana = 0f;
            f.Feet(1, Vector2.zero); int cues = f.P.F.Audio.OneShots.Count;
            f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(1) && Near(f.P.Stats.Mana, ClearingRun.BurstManaCost),
                "empty mana did not receive exactly one six-mana burst budget");
            f.P.Update(KeyCode.K);
            Expect(f.P.F.Run.PlayerAttackActive && f.P.F.Run.BurstCooldownRemaining > 0f
                && Near(f.P.Stats.Mana, 0f) && f.P.F.Audio.OneShots.Count == cues + 2,
                "restored actor mana did not fund one actual K burst with its cooldown and cue");
            long action = Get<long>(f.P.F.Run, "currentPlayerActionId");
            f.P.Update(KeyCode.E, KeyCode.K);
            Expect(Get<long>(f.P.F.Run, "currentPlayerActionId") == action && Near(f.P.Stats.Mana, 0f)
                && f.P.F.Audio.OneShots.Count == cues + 2 && f.Supplies.IsClaimed(1),
                "spent rune or active attack funded or replayed another burst");
            f.P.AssertTemplate();
        });
        test("full health and mana E preserve supplies and success audio until useful", () =>
        {
            foreach (int id in new[] { 0, 1 })
            {
                var f = new SupplyFixture(); f.Feet(id, Vector2.zero);
                int cues = f.P.F.Audio.OneShots.Count;
                f.P.Update(KeyCode.E);
                Expect(f.Supplies.IsDiscovered(id) && !f.Supplies.IsClaimed(id)
                    && f.P.F.Audio.OneShots.Count == cues,
                    "full resource consumed a supply or emitted a success cue");
                string prompt = Get<Text>(f.P.Hud, "message").text.ToLowerInvariant();
                Expect(prompt.Contains("full") && prompt.Contains(id == 0 ? "hp" : "mp"),
                    "full supply lacks the actual capacity reason in its edge hint");
                if (id == 0) f.P.Stats.Health = 8.5f;
                else f.P.Stats.Mana = 9.75f;
                f.P.Update(KeyCode.E);
                Expect(f.Supplies.IsClaimed(id) && f.P.F.Audio.OneShots.Count == cues + 1
                    && Near(id == 0 ? f.P.Stats.Health : f.P.Stats.Mana, id == 0 ? 10f : 12f),
                    "previous full-resource refusal prevented a later capped restoration");
            }
        });
        test("recorded wall blocks discovery and later known-supply E without spending or success cue", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Health = 5f; f.Feet(0, Vector2.zero);
            int cues = f.P.F.Audio.OneShots.Count;
            Physics2D.LinecastResponse = new BoxCollider2D();
            try { f.P.Update(KeyCode.E); }
            finally { Physics2D.LinecastResponse = null; }
            Expect(!f.Supplies.IsDiscovered(0) && !f.Supplies.IsClaimed(0)
                && Near(f.P.Stats.Health, 5f) && f.P.F.Audio.OneShots.Count == cues,
                "blocked wall visibility discovered or consumed herbs");
            f.P.Update();
            Expect(f.Supplies.IsDiscovered(0), "clear discovery did not recover after wall response ended");
            Physics2D.LinecastResponse = new BoxCollider2D();
            try { f.P.Update(KeyCode.E); }
            finally { Physics2D.LinecastResponse = null; }
            Expect(!f.Supplies.IsClaimed(0) && Near(f.P.Stats.Health, 5f)
                && f.P.F.Audio.OneShots.Count == cues, "remembered discovery bypassed current wall visibility");
            f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(0) && Near(f.P.Stats.Health, 9f),
                "a blocked attempt permanently consumed the useful supply");
        });
        test("paused dead and completed Update never discover or restore supplies", () =>
        {
            foreach (int state in new[] { 0, 1, 2 })
            {
                var f = new SupplyFixture();
                if (state == 0) Invoke(f.P.F.Runtime, "SetPaused", true);
                if (state == 1) { f.P.Stats.Health = 0f; f.P.Update(); }
                if (state == 2) f.P.Complete();
                f.Feet(0, Vector2.zero);
                float health = f.P.Stats.Health, mana = f.P.Stats.Mana;
                int cues = f.P.F.Audio.OneShots.Count;
                f.P.Update(KeyCode.E);
                Expect(!f.Supplies.IsDiscovered(0) && !f.Supplies.IsClaimed(0)
                    && Near(f.P.Stats.Health, health) && Near(f.P.Stats.Mana, mana)
                    && f.P.F.Audio.OneShots.Count == cues,
                    "inactive state " + state + " discovered/healed or emitted a supply reward");
            }
            var known = new SupplyFixture(); known.Feet(0, Vector2.zero); known.P.Update();
            known.P.Stats.Health = 4f; Invoke(known.P.F.Runtime, "SetPaused", true);
            known.P.Update(KeyCode.E);
            Expect(known.Supplies.IsDiscovered(0) && !known.Supplies.IsClaimed(0)
                && Near(known.P.Stats.Health, 4f), "pause erased discovery or accepted a known supply");
            Invoke(known.P.F.Runtime, "SetPaused", false); known.P.Update(KeyCode.E);
            Expect(known.Supplies.IsClaimed(0) && Near(known.P.Stats.Health, 8f),
                "resume could not use the still-available supply");
        });
        test("disable preserves discovery and spent charges while genuine death R retry restocks", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Health = 5f; f.P.Stats.Mana = 0f;
            f.Feet(0, Vector2.zero); f.P.Update(KeyCode.E);
            f.Feet(1, Vector2.zero); f.P.Update();
            Expect(f.Supplies.IsClaimed(0) && f.Supplies.IsDiscovered(1) && !f.Supplies.IsClaimed(1),
                "restock preservation setup failed");
            Invoke(f.P.F.Runtime, "OnDisable");
            f.P.Stats.Health = 1f; f.Feet(0, Vector2.zero);
            int cues = f.P.F.Audio.OneShots.Count; f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(0) && f.Supplies.IsDiscovered(1) && !f.Supplies.IsClaimed(1)
                && Near(f.P.Stats.Health, 1f) && f.P.F.Audio.OneShots.Count == cues,
                "disable/resume restocked herbs or forgot the rune discovery");
            f.P.Stats.Health = 0f; f.P.Update(); f.P.Update(KeyCode.R);
            Expect(!f.P.F.Run.IsDead && !f.Supplies.IsDiscovered(0) && !f.Supplies.IsDiscovered(1)
                && !f.Supplies.IsClaimed(0) && !f.Supplies.IsClaimed(1),
                "genuine new attempt failed to reset both supply lifecycles");
            f.P.Stats.Health = 5f; f.Feet(0, Vector2.zero); f.P.Update(KeyCode.E);
            Expect(f.Supplies.IsClaimed(0) && Near(f.P.Stats.Health, 9f), "new-attempt herbs were not usable");
            f.P.AssertTemplate();
        });
        test("beacon completion takes E priority and completed E still retries storage near a supply", () =>
        {
            var f = new SupplyFixture(); f.Feet(0, Vector2.zero); f.P.Update();
            f.P.Stats.Health = 5f; f.P.Store.AcceptWrites = false;
            f.P.Complete();
            Expect(f.P.F.Run.IsComplete && !f.Supplies.IsClaimed(0)
                && Near(f.P.Stats.Health, 5f) && f.P.Store.Writes == 0,
                "beacon E was replaced by herb restoration or saved a refused write");
            string id = f.P.RewardId; f.Feet(0, Vector2.zero); f.P.Store.AcceptWrites = true;
            f.P.Update(KeyCode.E); f.P.Update(KeyCode.E);
            Expect(f.P.Store.Writes == 1 && f.P.Progress.Data.BankCoins == 30
                && f.P.Progress.IsCompletionBanked(id) && f.P.RewardId == id
                && Near(f.P.Stats.Health, 5f) && !f.Supplies.IsClaimed(0),
                "terminal E near herbs diverted storage retry or duplicated the completed reward");
        });
        test("supply props cache distinct nonblocking glyphs and swap discovered content for spent marks", () =>
        {
            var shared = new ClearingVisuals(new GameObject("Shared supply-art owner").transform);
            int initialTextures = Texture2D.CreatedCount, initialSprites = Sprite.CreatedCount;
            var isolated = new ClearingSupplyVisuals(new GameObject("Nonblocking supply fixture").transform, shared);
            Expect(Texture2D.CreatedCount == initialTextures && Sprite.CreatedCount == initialSprites,
                "decorative supplies created replacement textures or sprites instead of reusing the clearing pixel");
            isolated.Dispose(); shared.Dispose();
            var f = new SupplyFixture();
            int objects = GameObject.CreatedCount, textures = Texture2D.CreatedCount, sprites = Sprite.CreatedCount;
            Expect(Get<SpriteRenderer[][]>(f.Props, "glyphs")[0].Length != Get<SpriteRenderer[][]>(f.Props, "glyphs")[1].Length,
                "health and mana content lost their distinct authored silhouettes");
            f.Props.Refresh(f.Supplies, f.P.F.Run.Time);
            foreach (int id in new[] { 0, 1 })
            {
                NoSupplyColliders(f.Prop(id).transform);
                Expect((Vector2)f.Prop(id).transform.localPosition == SupplyFixture.Point(id),
                    "prop local world position differs from interaction rules");
                Expect(f.Glyph(id).gameObject.activeSelf && !f.Empty(id).gameObject.activeSelf,
                    "fresh prop lacks an unclaimed motif");
            }
            Color hidden = f.Glyph(0).color;
            f.Feet(0, Vector2.zero); f.P.Update();
            Color revealed = f.Glyph(0).color;
            Expect(!Near(hidden.r, revealed.r) || !Near(hidden.g, revealed.g) || !Near(hidden.b, revealed.b),
                "discovery has no visible palette-state change");
            f.P.Stats.Health = 5f; f.P.Update(KeyCode.E);
            Expect(!f.Glyph(0).gameObject.activeSelf && f.Empty(0).gameObject.activeSelf
                && f.Glyph(1).gameObject.activeSelf && !f.Empty(1).gameObject.activeSelf,
                "claim did not swap only its own content for a spent mark");
            for (int i = 0; i < 25; i++) f.P.Update();
            Expect(GameObject.CreatedCount == objects && Texture2D.CreatedCount == textures
                && Sprite.CreatedCount == sprites, "supply refresh/claim allocated new objects or rasters");
        });
        test("claim pulse freezes on pause then expires and clears on disable retry and disposal", () =>
        {
            var f = new SupplyFixture(); f.P.Stats.Health = 5f;
            f.Feet(0, Vector2.zero); f.P.Update(KeyCode.E);
            Expect(f.Pulse(0).gameObject.activeSelf, "accepted supply has no cached claim pulse");
            float alpha = f.Pulse(0).color.a, scale = f.Pulse(0).transform.localScale.x;
            double time = f.P.F.Run.Time; int objects = GameObject.CreatedCount;
            Invoke(f.P.F.Runtime, "SetPaused", true);
            Time.unscaledTime += 50f;
            for (int i = 0; i < 8; i++) { f.P.F.Tick(); f.P.Update(); }
            Expect(f.P.F.Run.Time == time && f.Pulse(0).gameObject.activeSelf
                && Near(f.Pulse(0).color.a, alpha) && Near(f.Pulse(0).transform.localScale.x, scale),
                "pause advanced or erased the simulation-time supply pulse");
            Invoke(f.P.F.Runtime, "SetPaused", false);
            f.P.F.Run.Advance(.5f); f.P.Update();
            Expect(!f.Pulse(0).gameObject.activeSelf, "expired claim pulse stayed active");
            f.Props.ShowClaim(0, f.P.F.Run.Time); f.P.F.Refresh();
            Invoke(f.P.F.Runtime, "OnDisable"); f.P.F.Refresh();
            Expect(!f.Pulse(0).gameObject.activeSelf && f.Supplies.IsClaimed(0),
                "disable retained a pulse or restocked the consumed supply");
            f.Props.ShowClaim(0, f.P.F.Run.Time); f.P.F.Refresh();
            Expect(f.Pulse(0).gameObject.activeSelf, "retry clear setup has no live claim pulse");
            Invoke(f.P.F.Runtime, "ResetRun"); f.P.F.Refresh();
            Expect(!f.Pulse(0).gameObject.activeSelf && !f.Pulse(1).gameObject.activeSelf
                && GameObject.CreatedCount == objects, "retry retained a claim pulse or allocated replacement props");
            GameObject first = f.Prop(0), second = f.Prop(1); f.Props.Dispose();
            Expect(first.Destroyed && second.Destroyed, "owned supply roots were not disposed");
        });
        test("supply hints share the existing edge slot behind the beacon and vanish in terminal states", () =>
        {
            foreach (float width in new[] { 620f, 960f, 1280f })
            {
                var f = new HudFixture(); f.Width(width);
                f.Hud.Refresh(f.Stats, f.Run, false, false, false, "Accepted contact", null, "", "", "E - herbs: +4 HP");
                Expect(f.Message.text == "E - herbs: +4 HP" && f.Help.text == "",
                    "nearby supply action did not replace transient feedback in its shared slot");
                Rect rect = RecordedLabelRect(f.Message, new Vector2(width, 540f));
                Expect(rect.y + rect.height <= 81f && f.Message.rectTransform.sizeDelta == f.Help.rectTransform.sizeDelta,
                    "supply hint extended into the south combat view or allocated a second edge column");
                f.Hud.Refresh(f.Stats, f.Run, false, false, true, "Accepted contact", null, "", "", "E - herbs: +4 HP");
                Expect(f.Message.text.Contains("beacon") && !f.Message.text.Contains("herbs"),
                    "supply hint hid the available beacon action");
                f.Hud.Refresh(f.Stats, f.Run, false, false, false, "Accepted contact");
                Expect(f.Message.text == "Accepted contact", "leaving supply range did not restore existing transient feedback");
                f.Hud.Refresh(f.Stats, f.Run, true, false, false, "", null, "", "", "E - herbs: +4 HP");
                Expect(f.Message.text == "" && f.Help.text == "", "pause still advertises a supply action");
            }
            foreach (bool complete in new[] { false, true })
            {
                var f = new HudFixture();
                if (complete) Expect(f.Run.TryCompleteObjective(), "supply HUD completion setup rejected");
                else f.Run.NotifyPlayerDeath();
                f.Hud.Refresh(f.Stats, f.Run, false, false, false, "", null, "", "", "E - herbs: +4 HP");
                Expect(f.Message.text == "" && f.Help.text == "" && !string.IsNullOrEmpty(Get<Text>(f.Hud, "terminal").text),
                    "terminal HUD advertises a supply or hides its result");
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
        AddTacticsChecks(test);
        AddDamageNumberChecks(test);
        AddSupplyChecks(test);
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; actual project C# with recording boundaries, not native Unity.");
        return failed == 0 ? 0 : 1;
    }
}
