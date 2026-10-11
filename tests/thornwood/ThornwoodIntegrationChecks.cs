using System;
using System.Reflection;
using Rpg.Gameplay;
using UnityEngine;
using UnityEngine.UI;

// Actual project methods with explicit lifecycle/input/contact injection. These
// recording boundaries do not schedule Unity, simulate collisions or render UI,
// play Animator frames, resolve real Input System devices or audition audio.
public static class ThornwoodIntegrationChecks
{
    private static FieldInfo Field(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Production field absent: " + target.GetType().Name + "." + name);
        return field;
    }
    private static T Get<T>(object target, string name) { return (T)Field(target, name).GetValue(target); }
    private static void Set(object target, string name, object value) { Field(target, name).SetValue(target, value); }
    private static object Call(object target, string name, params object[] args)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Production method absent: " + target.GetType().Name + "." + name);
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static bool Near(float first, float second) { return Math.Abs(first - second) < .0002f; }
    private static bool Near(Vector2 first, Vector2 second) { return Near(first.x, second.x) && Near(first.y, second.y); }
    private static Vector2 Point(ThornwoodPoint value) { return new Vector2(value.X, value.Y); }

    private sealed class RecordedProgressStore : IClearingProgressStore
    {
        internal ClearingProgressData Saved;
        internal bool AcceptWrites = true;
        internal int Attempts, Writes;
        internal readonly bool Writable;
        internal RecordedProgressStore(ClearingProgressData data = null, bool writable = true)
        { Saved = data ?? ClearingProgressData.Fresh; Writable = writable; }
        public ProgressLoadResult Load() { return new ProgressLoadResult(Saved, ProgressLoadKind.New, Writable); }
        public bool TrySave(ClearingProgressData expected, ClearingProgressData next)
        {
            Attempts++;
            if (!AcceptWrites || !Writable || !object.ReferenceEquals(expected, Saved)) return false;
            Saved = next; Writes++; return true;
        }
    }

    private sealed class Fixture
    {
        internal readonly ClearingRuntime Clearing;
        internal readonly Player Player;
        internal readonly PlayerStats Template;
        internal readonly Rigidbody2D Body;
        internal readonly PlayerMovement Movement;
        internal readonly Camera Camera;
        internal readonly RecordedProgressStore Store;
        internal readonly ClearingProgress Progress;
        internal PlayerStats Stats { get { return Player.Stats; } }
        internal ClearingRun ClearingRun { get { return Get<ClearingRun>(Clearing, "run"); } }
        internal ThornwoodRuntime Forest { get { return Get<ThornwoodRuntime>(Clearing, "forest"); } }
        internal ThornwoodRun ForestRun { get { return Get<ThornwoodRun>(Forest, "run"); } }
        internal ClearingHud Hud { get { return Get<ClearingHud>(Clearing, "hud"); } }
        internal string ClearingRewardId { get { return Get<string>(Clearing, "rewardId"); } }
        internal string ForestRewardId { get { return Get<string>(Forest, "rewardId"); } }
        internal AudioSource Audio { get { return Get<AudioSource>(Get<ClearingAudio>(Clearing, "sound"), "source"); } }
        internal bool Controls { get { return Get<bool>(Movement, "controlsEnabled"); } }

        internal Fixture(ClearingProgressData data = null, bool writable = true)
        {
            Input.ClearPressed(); Physics2D.LinecastResponse = null; Physics2D.LinecastQuery = null;
            Time.timeScale = 1f; Time.fixedDeltaTime = .02f; Time.unscaledTime = 0f;
            Template = new PlayerStats { Health = 10f, MaxHealth = 10f, Mana = 12f, MaxMana = 12f };
            var actor = new GameObject("Recorded original actor");
            Player = actor.AddComponent<Player>(); Set(Player, "stats", Template); Call(Player, "Awake");
            Body = actor.AddComponent<Rigidbody2D>();
            var animator = actor.AddComponent<Animator>();
            var animations = actor.AddComponent<PlayerAnimations>();
            animations.Components[typeof(Animator)] = animator; Call(animations, "Awake");
            Movement = actor.AddComponent<PlayerMovement>();
            Movement.Components[typeof(Player)] = Player; Movement.Components[typeof(PlayerAnimations)] = animations;
            Movement.Components[typeof(Rigidbody2D)] = Body; Call(Movement, "Awake");
            var health = actor.AddComponent<PlayerHealth>();
            health.Components[typeof(Player)] = Player; health.Components[typeof(PlayerAnimations)] = animations; Call(health, "Awake");
            var mana = actor.AddComponent<PlayerMana>(); mana.Components[typeof(Player)] = Player; Call(mana, "Awake");
            var renderer = actor.AddComponent<SpriteRenderer>();
            Player.Components[typeof(PlayerAnimations)] = animations; Player.Components[typeof(PlayerMovement)] = Movement;
            Player.Components[typeof(PlayerHealth)] = health; Player.Components[typeof(PlayerMana)] = mana;
            Player.Components[typeof(Rigidbody2D)] = Body; Player.Components[typeof(Animator)] = animator;
            Player.Components[typeof(SpriteRenderer)] = renderer;
            Camera = new GameObject("Recorded scene Camera").AddComponent<Camera>(); Camera.aspect = 16f / 9f;
            Clearing = new GameObject("Actual Clearing runtime").AddComponent<ClearingRuntime>();
            Set(Clearing, "player", Player); Set(Clearing, "sceneCamera", Camera);
            string path = Application.persistentDataPath; Application.persistentDataPath = "";
            try { Call(Clearing, "Start"); } finally { Application.persistentDataPath = path; }
            Store = new RecordedProgressStore(data, writable); Progress = new ClearingProgress(Store);
            Call(Clearing, "InitializeProgress", Progress); Call(Clearing, "ResetRun");
            Forest.Dispose();
            Set(Clearing, "forest", new ThornwoodRuntime(Clearing.transform, Player, Camera,
                Get<ClearingVisuals>(Clearing, "visuals"), Get<ClearingAudio>(Clearing, "sound"), Progress, 10f, 12f));
            var rect = (RectTransform)Get<Transform>(Hud, "root"); rect.rect = new Rect(0, 0, 960, 540);
            Expect(!object.ReferenceEquals(Template, Stats), "Fixture actor did not clone its template");
        }
        internal void Position(Vector2 feet) { Body.position = feet - ClearingVisuals.PlayerFootOffset; }
        internal void Position(ThornwoodPoint feet) { Position(Point(feet)); }
        internal void Update(params KeyCode[] keys)
        {
            Time.unscaledTime += .1f; Set(Clearing, "nextHudAt", 0f); Input.SetPressed(keys);
            try { Call(Clearing, "Update"); } finally { Input.ClearPressed(); }
        }
        internal void Tick(int count = 1) { for (int i = 0; i < count; i++) Call(Clearing, "FixedUpdate"); }
        internal void Late() { Call(Clearing, "LateUpdate"); }
        internal void FinishClearing(bool bank = true, bool retainBossImmunity = false)
        {
            // Setup uses actual domain APIs; the completion/bank/forest admission
            // being checked below always enters through the actual runtime input.
            for (int id = 0; id < ClearingRun.SentinelCount; id++)
                while (!ClearingRun.GetSentinel(id).IsDead)
                {
                    long token; Expect(ClearingRun.BeginPlayerAttack(PlayerAttackKind.Light, Stats.Mana, out token), "clearing setup light rejected");
                    Expect(ClearingRun.TryHitSentinel(token, id), "clearing setup contact rejected");
                    ClearingRun.Advance(ClearingRun.LightCooldown);
                }
            Expect(ClearingRun.TryAwakenBoss(), "clearing setup Boss awakening rejected");
            while (!ClearingRun.Boss.IsDead)
            {
                if (retainBossImmunity && ClearingRun.Boss.Health <= ClearingRun.LightDamage)
                {
                    Expect(ClearingRun.TryBeginBossAttack(0f, 0f, 0f, -1f, true), "final Boss contact setup rejected");
                    ClearingRun.Advance(ClearingRun.Boss.WindupDuration);
                    Expect(ClearingRun.TryResolveBossHit(), "final Boss contact did not establish actual clearing immunity");
                }
                long token; Expect(ClearingRun.BeginPlayerAttack(PlayerAttackKind.Light, Stats.Mana, out token), "Boss setup light rejected");
                Expect(ClearingRun.TryHitBoss(token), "Boss setup contact rejected"); ClearingRun.Advance(ClearingRun.LightCooldown);
            }
            Position(ClearingVisuals.BeaconPosition);
            if (!bank) Store.AcceptWrites = false;
            Update(KeyCode.E);
            Expect(ClearingRun.IsComplete, "actual beacon E did not complete the clearing");
        }
        internal void Enter()
        {
            Position(ThornwoodLayout.ReturnToClearing); Update(KeyCode.F);
            Expect(Forest.Active && ForestRun.Entered, "actual F at forest waystone did not enter");
        }
        internal void Leave() { Position(ThornwoodLayout.Entry); Update(KeyCode.E); Expect(!Forest.Active, "actual E at forest entry did not leave"); }
        internal void AssertTemplate()
        {
            Expect(Near(Template.MaxHealth, 10f) && Near(Template.Health, 10f)
                && Near(Template.MaxMana, 12f) && Near(Template.Mana, 12f), "runtime modified authoring PlayerStats");
        }
        internal object Number(int id) { return Get<Array>(Get<ClearingDamageNumbers>(Forest, "damageNumbers"), "slots").GetValue(id); }
        internal ThornwoodEnemyView Enemy(int id) { return Get<ThornwoodVisuals>(Forest, "visuals").Enemies[id]; }
        internal Rigidbody2D EnemyBody(int id) { return Enemy(id).Body; }
        internal void CompleteForest()
        {
            for (int id = 0; id < ThornwoodRun.SeedCount; id++) { Position(ThornwoodLayout.SeedPosition(id)); Update(KeyCode.E); }
            Expect(ForestRun.SeedsCollected == 3, "actual seed E interactions did not collect all three");
            // Each enemy contact is executed by actual StartAttack/FixedUpdate.
            for (int id = 0; id < ClearingRun.SentinelCount; id++)
            {
                Vector2 feet = new Vector2(22f, -3f); Position(feet);
                for (int other = 0; other < ClearingRun.SentinelCount; other++)
                    EnemyBody(other).position = other == id ? feet + Vector2.down * .7f : new Vector2(17f + other, 3.8f);
                while (!ForestRun.Combat.GetSentinel(id).IsDead)
                {
                    while (ForestRun.Combat.LightCooldownRemaining > 0f || ForestRun.Combat.PlayerAttackActive) Tick();
                    Update(KeyCode.J); Tick();
                }
            }
            Position(ThornwoodLayout.Cache); Update(KeyCode.E);
            Expect(ForestRun.Combat.IsComplete, "actual unlocked cache E did not complete forest");
        }
        internal Fixture Entered()
        { FinishClearing(); Enter(); return this; }
        internal void StageSingleEnemy(int id = 0)
        {
            Vector2 feet = new Vector2(22f, -3f); Position(feet);
            for (int other = 0; other < ClearingRun.SentinelCount; other++)
                EnemyBody(other).position = other == id ? feet + Vector2.down * .7f : new Vector2(17f + other, 3.8f);
        }
        internal void StageLastHit(int id)
        {
            while (ForestRun.Combat.GetSentinel(id).Health > ClearingRun.LightDamage)
            {
                long token; Expect(ForestRun.TryBeginPlayerAttack(PlayerAttackKind.Light, Stats.Mana, out token), "last-hit setup rejected");
                Expect(ForestRun.TryHitStalker(token, id), "last-hit setup contact rejected");
                ForestRun.Advance(ClearingRun.LightCooldown);
            }
        }
    }

    public static int Main()
    {
        int passed = 0, failed = 0;
        Action<string, Action> test = (name, body) =>
        {
            try { body(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
            finally { Input.ClearPressed(); Physics2D.LinecastResponse = null; Physics2D.LinecastQuery = null; Time.timeScale = 1f; }
        };
        AddChecks(test);
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed; actual project C# with recording boundaries, not native Unity.");
        return failed == 0 ? 0 : 1;
    }

    private static void AddChecks(Action<string, Action> test)
    {
        test("clearing F cannot enter before objective completion or saving", () =>
        {
            var f = new Fixture(); f.Position(ThornwoodLayout.ReturnToClearing); f.Update(KeyCode.F);
            Expect(!f.Forest.Active && f.Store.Attempts == 0 && f.Stats.Health == 10f, "locked forest entry mutated state");
            f.FinishClearing(false); f.Update(KeyCode.F);
            Expect(!f.Forest.Active && f.Progress.Data.BankCoins == 0 && !f.Controls, "unbanked clearing admitted expedition");
            f.Store.AcceptWrites = true; f.Update(KeyCode.E); f.Update(KeyCode.F);
            Expect(f.Forest.Active && f.Progress.Data.BankCoins == 30 && f.Store.Writes == 1, "saved completion did not unlock F");
            f.AssertTemplate();
        });
        test("clearing F rejects far waystone and paused completion without changing resources", () =>
        {
            var f = new Fixture(); f.FinishClearing(); f.Stats.Health = 3f; f.Stats.Mana = 2f;
            f.Position(Vector2.zero); f.Update(KeyCode.F);
            Expect(!f.Forest.Active && Near(f.Stats.Health, 3f) && Near(f.Stats.Mana, 2f), "far F refilled or entered");
            f.Position(ThornwoodLayout.ReturnToClearing); Call(f.Clearing, "SetPaused", true); f.Update(KeyCode.F);
            Expect(!f.Forest.Active && Time.timeScale == 0f && Near(f.Stats.Health, 3f), "paused F refilled or entered");
            Call(f.Clearing, "SetPaused", false); f.Enter();
            Expect(f.Forest.Active && Near(f.Stats.Health, 10f) && Near(f.Stats.Mana, 12f), "first admission failed to start expedition resources");
        });
        test("forest first admission fills same actor once and reentry preserves HP MP seeds and identity", () =>
        {
            var f = new Fixture(); f.FinishClearing(); PlayerStats stats = f.Stats;
            f.Stats.Health = 1f; f.Stats.Mana = 0f; f.Enter(); string id = f.ForestRewardId;
            Expect(object.ReferenceEquals(stats, f.Stats) && Near(f.Stats.Health, 10f) && Near(f.Stats.Mana, 12f)
                && id.Length == 32 && f.Controls, "entry replaced actor state or did not fill once");
            f.Position(ThornwoodLayout.SeedPosition(0)); f.Update(KeyCode.E);
            f.Stats.Health = 4f; f.Stats.Mana = 3f; f.Leave(); f.Enter();
            Expect(object.ReferenceEquals(stats, f.Stats) && Near(f.Stats.Health, 4f) && Near(f.Stats.Mana, 3f)
                && f.ForestRun.SeedsCollected == 1 && f.ForestRewardId == id, "leave/reentry restocked or duplicated attempt");
            f.AssertTemplate();
        });
        test("forest starts at entry and restores clearing beacon camera and terminal controls", () =>
        {
            var f = new Fixture(); f.FinishClearing(); f.Late();
            Vector3 camera = f.Camera.transform.position; float zoom = f.Camera.orthographicSize;
            f.Enter(); Expect(Near(f.Body.position + ClearingVisuals.PlayerFootOffset, Point(ThornwoodLayout.Entry)) && f.Controls,
                "entry did not teleport feet or enable controls");
            f.Late(); Expect(Near(f.Camera.transform.position.x, ThornwoodLayout.CenterX), "forest did not own camera");
            f.Leave(); f.Late();
            Expect(Near(f.Body.position + ClearingVisuals.PlayerFootOffset, ClearingVisuals.BeaconPosition)
                && Near(f.Camera.transform.position.x, camera.x) && Near(f.Camera.transform.position.y, camera.y)
                && Near(f.Camera.orthographicSize, zoom) && !f.Controls && f.ClearingRun.IsComplete,
                "return lost reachable beacon camera or completed clearing controls");
            f.Update(KeyCode.Alpha1); Expect(f.Progress.Data.VitalityRank == 1 && f.Progress.Data.BankCoins == 0,
                "returned completed clearing upgrade controls are unavailable");
        });
        test("first F frame grants forest sole hero presentation ownership despite retained clearing immunity", () =>
        {
            var f = new Fixture(); f.FinishClearing(retainBossImmunity: true);
            Expect(f.ClearingRun.PlayerInvulnerabilityRemaining > 0f, "fixture did not retain actual clearing contact immunity");
            f.Enter();
            SpriteRenderer actor = f.Player.GetComponent<SpriteRenderer>();
            Expect(f.ForestRun.Combat.PlayerInvulnerabilityRemaining == 0f
                && Near(actor.color.r, 1f) && Near(actor.color.g, 1f) && Near(actor.color.b, 1f) && Near(actor.color.a, 1f),
                "clearing RefreshViews overwrote the newly entered forest actor with an old clearing hurt tint");
        });
        test("actual seed E needs range and clear line and never deposits an early reward", () =>
        {
            var f = new Fixture().Entered(); f.Position(new Vector2(22f, -3f)); f.Update(KeyCode.E);
            Expect(f.ForestRun.SeedsCollected == 0 && f.Progress.Data.BankCoins == 30, "far seed or incidental E deposited reward");
            f.Position(ThornwoodLayout.SeedPosition(0)); Physics2D.LinecastResponse = new BoxCollider2D(); f.Update(KeyCode.E);
            Expect(f.ForestRun.SeedsCollected == 0, "blocked seed was accepted");
            Physics2D.LinecastResponse = null; f.Update(KeyCode.E); int cues = f.Audio.OneShots.Count;
            f.Update(KeyCode.E);
            Expect(f.ForestRun.SeedsCollected == 1 && f.Store.Writes == 1 && f.Audio.OneShots.Count == cues,
                "duplicate seed replayed rewards or banked without cache");
        });
        test("north cache requires all seeds and defeated stalkers before actual E can bank", () =>
        {
            var f = new Fixture().Entered(); f.Position(ThornwoodLayout.Cache); f.Update(KeyCode.E);
            Expect(!f.ForestRun.Combat.IsComplete && f.Store.Writes == 1, "empty objective unlocked cache");
            for (int i = 0; i < 3; i++) { f.Position(ThornwoodLayout.SeedPosition(i)); f.Update(KeyCode.E); }
            f.Position(ThornwoodLayout.Cache); f.Update(KeyCode.E);
            Expect(!f.ForestRun.Combat.IsComplete && f.Progress.Data.BankCoins == 30, "seeds alone unlocked cache");
            f.CompleteForest();
            Expect(f.Progress.Data.BankCoins == 60 && f.Progress.Data.ClearedRuns == 2 && f.Store.Writes == 2,
                "complete actual forest loop did not save one additional thirty");
        });
        test("Light actual runtime contact removes eighteen HP once and keeps MP cost zero", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); float mp = f.Stats.Mana;
            f.Update(KeyCode.J); long id = Get<long>(f.Forest, "strikeId"); f.Tick(); f.Update();
            Expect(f.ForestRun.Combat.GetSentinel(0).Health == 36f && Near(Get<float>(f.Number(0), "Amount"), 18f), "Light contact lost actual HP delta");
            Expect(Near(f.Stats.Mana, mp) && f.ForestRun.Combat.LightCooldownRemaining > 0f, "Light cost/cooldown differs from clearing");
            f.Tick(); f.Update(KeyCode.J);
            Expect(f.ForestRun.Combat.GetSentinel(0).Health == 36f && Get<long>(f.Forest, "strikeId") == id,
                "same action or cooldown replayed contact");
        });
        test("Burst spends six MP once applies thirty and overkill feedback reports only remaining HP", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.StageLastHit(0);
            f.Update(KeyCode.K);
            Expect(Near(f.Stats.Mana, 6f) && Near(f.ForestRun.Combat.BurstCooldownRemaining, 1.2f), "Burst admission cost or cooldown changed");
            f.Tick(); f.Update();
            Expect(f.ForestRun.Combat.GetSentinel(0).IsDead && Near(Get<float>(f.Number(0), "Amount"), 18f), "overkill feedback displays nominal thirty");
            Expect(!f.Enemy(0).Actor.activeSelf && !f.EnemyBody(0).simulated, "dead stalker retained active body");
            float mp = f.Stats.Mana; f.Update(KeyCode.K); Expect(Near(f.Stats.Mana, mp), "cooldown Burst spent mana again");
        });
        test("forest low MP paused and out of range attacks cannot invent damage or spend", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.Stats.Mana = 5f; int cues = f.Audio.OneShots.Count;
            f.Update(KeyCode.K); Expect(Near(f.Stats.Mana, 5f) && Get<long>(f.Forest, "strikeId") == 0 && f.Audio.OneShots.Count == cues,
                "low MP Burst admitted or sounded");
            f.Update(KeyCode.Escape); f.Update(KeyCode.J, KeyCode.K, KeyCode.E);
            Expect(f.ForestRun.Paused && Near(f.Stats.Mana, 5f) && !f.ForestRun.Combat.PlayerAttackActive, "paused input mutated combat");
            f.Update(KeyCode.Escape); f.EnemyBody(0).position = new Vector2(26f, -3.7f); f.Update(KeyCode.J); f.Tick();
            Expect(f.ForestRun.Combat.GetSentinel(0).Health == 54f, "out-of-range light contacted");
        });
        test("actual forest LOS boundary rejects player hits through roots", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); Physics2D.LinecastResponse = new BoxCollider2D();
            f.Update(KeyCode.J); f.Tick();
            Expect(f.ForestRun.Combat.GetSentinel(0).Health == 54f, "blocked player contact removed HP");
        });
        test("stalker pounce captures a fixed warning and locks movement through windup", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.Tick(); f.Update();
            SentinelFootprint snapshot = f.ForestRun.GetStalkerFootprint(0);
            Expect(snapshot != null && f.ForestRun.Combat.GetSentinel(0).AttackPhase == SentinelAttackPhase.Telegraph,
                "near stalking AI did not start locked pounce");
            Vector2 origin = f.EnemyBody(0).position; int moves = f.EnemyBody(0).MoveRequests;
            f.Position(new Vector2(22.5f, -3f)); f.Tick(4); f.Update();
            Expect(object.ReferenceEquals(snapshot, f.ForestRun.GetStalkerFootprint(0))
                && Near(f.EnemyBody(0).position, origin) && f.EnemyBody(0).MoveRequests == moves,
                "warning retargeted or slid during windup");
            Expect(Get<Transform>(f.Enemy(0), "warning").gameObject.activeSelf, "live warning not visible by recorded command");
        });
        test("stalker AI approaches only a clear valid target and obeys layout swept movement", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.EnemyBody(0).position = new Vector2(22f, 1f);
            Vector2 before = f.EnemyBody(0).position; f.Tick();
            Expect(f.EnemyBody(0).MoveRequests == 1 && f.EnemyBody(0).position.y < before.y,
                "valid clear stalker target was not approached");
            Physics2D.LinecastResponse = new BoxCollider2D(); int moves = f.EnemyBody(0).MoveRequests; f.Tick();
            Expect(f.EnemyBody(0).MoveRequests == moves, "blocked LOS still moved stalker");
        });
        test("recorded pounce border and phase fill stay wholly inside the immutable contact footprint", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.Tick(); f.Update();
            SentinelFootprint saved = f.ForestRun.GetStalkerFootprint(0);
            Transform warning = Get<Transform>(f.Enemy(0), "warning");
            SpriteRenderer[] border = Get<SpriteRenderer[]>(f.Enemy(0), "warningBorder");
            SpriteRenderer fill = Get<SpriteRenderer>(f.Enemy(0), "warningFill");
            foreach (float advance in new[] { 0f, .2f, .35f, .31f })
            {
                f.ForestRun.Advance(advance); f.Position(new Vector2(22.4f, -3f)); f.Update();
                Expect(warning.gameObject.activeSelf && object.ReferenceEquals(saved, f.ForestRun.GetStalkerFootprint(0))
                    && Near(warning.position.x, saved.CenterX) && Near(warning.position.y, saved.CenterY)
                    && Near(warning.rotation.RecordedEulerZ, saved.AngleDegrees),
                    "warning followed a moving actor instead of the saved attack contact footprint");
                foreach (SpriteRenderer edge in border)
                {
                    Vector3 center = edge.transform.localPosition, size = edge.transform.localScale;
                    Expect(Math.Abs(center.x) + size.x * .5f <= saved.Width * .5f + .00001f
                        && Math.Abs(center.y) + size.y * .5f <= saved.Height * .5f + .00001f,
                        "full border stroke advertises danger outside the accepted contact rectangle");
                }
                Vector3 fillCenter = fill.transform.localPosition, fillSize = fill.transform.localScale;
                Expect(fillSize.x >= 0f && fillSize.x <= saved.Width && fillSize.y <= saved.Height
                    && Math.Abs(fillCenter.x) + fillSize.x * .5f <= saved.Width * .5f + .00001f
                    && Math.Abs(fillCenter.y) + fillSize.y * .5f <= saved.Height * .5f + .00001f,
                    "warning progress fill leaves the saved rectangle during telegraph or active contact");
            }
            Expect(f.ForestRun.Combat.GetSentinel(0).AttackPhase == SentinelAttackPhase.Active
                && Near(fill.transform.localScale.x, saved.Width), "active pounce did not fill its entire accepted lane");
            f.ForestRun.Advance(.17f); f.Update();
            Expect(f.ForestRun.Combat.GetSentinel(0).AttackPhase == SentinelAttackPhase.Recovery && !warning.gameObject.activeSelf,
                "recorded danger remained visible in recovery after contact ended");
        });
        test("actual pounce contact applies clamped actor HP delta and shares invulnerability", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.Stats.Health = 1f;
            f.Tick(); f.ForestRun.Advance(ClearingRun.WindupDuration(SentinelAttackKind.Lance)); f.Tick();
            Expect(f.Stats.Health == 0f && f.ForestRun.Combat.IsDead && !f.Controls,
                "actual active pounce did not synchronize actor death");
            // Death clears pooled presentation intentionally; the source call fed
            // actual accepted loss before that cleanup. Nonlethal case below
            // keeps its slot alive so we can inspect the production delta.
            var g = new Fixture().Entered(); g.StageSingleEnemy(); g.Tick();
            g.ForestRun.Advance(ClearingRun.WindupDuration(SentinelAttackKind.Lance)); g.Tick(); g.Update();
            Expect(Near(g.Stats.Health, 8f) && Near(Get<float>(g.Number(3), "Amount"), 2f), "hurt feedback lost actual delta");
            g.Tick(2); Expect(Near(g.Stats.Health, 8f), "one pounce ignored resolved-hit/invulnerability");
        });
        test("player-first finishing hits suppress same tick lethal stalker retaliation", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.StageLastHit(0); f.Stats.Health = 2f;
            Expect(f.ForestRun.TryBeginStalkerAttack(0, 22f, -3.7f, 22f, -3f, true), "same-tick pounce setup rejected");
            f.ForestRun.Advance(ClearingRun.WindupDuration(SentinelAttackKind.Lance) - .01f);
            f.Update(KeyCode.J); f.Tick(); f.Update();
            Expect(f.ForestRun.Combat.GetSentinel(0).IsDead && Near(f.Stats.Health, 2f) && !f.ForestRun.Combat.IsDead,
                "defeated same-tick stalker still retaliated");
            Expect(Get<Transform>(f.Enemy(0), "impact").gameObject.activeSelf, "defeat hid independent hit confirmation");
        });
        test("paused runtime freezes simulation warning cooldown numbers and feedback despite wall time", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); f.Update(KeyCode.J); f.Tick(); f.Update();
            double time = f.ForestRun.Combat.Time; float cooldown = f.ForestRun.Combat.LightCooldownRemaining;
            Transform label = Get<Transform>(f.Number(0), "Root"); Vector3 labelPoint = label.position;
            double deadline = Get<double>(f.Forest, "feedbackUntil"); f.Update(KeyCode.Escape);
            Time.unscaledTime += 100f; f.Tick(30); f.Update();
            Expect(f.ForestRun.Paused && f.ForestRun.Combat.Time == time && Near(f.ForestRun.Combat.LightCooldownRemaining, cooldown)
                && Near(label.position.y, labelPoint.y) && Get<double>(f.Forest, "feedbackUntil") == deadline && Time.timeScale == 0f,
                "paused wall time changed gameplay feedback or cooldown");
            f.Update(KeyCode.Escape); f.Tick(); Expect(f.ForestRun.Combat.Time > time && Time.timeScale == 1f && f.Controls,
                "unpause did not restore simulation and controls");
        });
        test("component disable cancels stale attacks and preserves resources seeds cooldown and attempt", () =>
        {
            var f = new Fixture().Entered(); f.Position(ThornwoodLayout.SeedPosition(0)); f.Update(KeyCode.E);
            f.StageSingleEnemy(); f.Update(KeyCode.K); long token = Get<long>(f.Forest, "strikeId");
            float mp = f.Stats.Mana; float cd = f.ForestRun.Combat.BurstCooldownRemaining; string id = f.ForestRewardId;
            Call(f.Clearing, "OnDisable");
            Expect(!f.ForestRun.Combat.PlayerAttackActive && Get<long>(f.Forest, "strikeId") == 0 && f.ForestRun.GetStalkerFootprint(0) == null,
                "disable retained resumed contact token");
            Expect(!f.ForestRun.TryHitStalker(token, 0) && Near(f.Stats.Mana, mp)
                && Near(f.ForestRun.Combat.BurstCooldownRemaining, cd) && f.ForestRewardId == id && f.ForestRun.SeedsCollected == 1,
                "disable refunded spent resources/cooldown or lost objective identity");
            f.Update(); f.Tick(); Expect(f.ForestRun.Combat.GetSentinel(0).Health == 54f, "disabled attack resurrected on resume");
        });
        test("disable from pause restores time scale without resetting expedition", () =>
        {
            var f = new Fixture().Entered(); f.Update(KeyCode.Escape); string id = f.ForestRewardId;
            Call(f.Clearing, "OnDisable");
            Expect(Time.timeScale == 1f && !f.ForestRun.Paused && f.Forest.Active && f.ForestRewardId == id,
                "component disable left next scene frozen or reset expedition");
        });
        test("death retry resets only forest attempt without replacing actor or altering saved clearing", () =>
        {
            var f = new Fixture().Entered(); PlayerStats stats = f.Stats; string clearingId = f.ClearingRewardId;
            f.Position(ThornwoodLayout.SeedPosition(0)); f.Update(KeyCode.E); string forestId = f.ForestRewardId;
            f.Stats.Health = 0f; f.Update(); Expect(f.ForestRun.Combat.IsDead && !f.Controls, "external actor death not synchronized");
            f.Update(KeyCode.R, KeyCode.J, KeyCode.K, KeyCode.E);
            Expect(f.Forest.Active && !f.ForestRun.Combat.IsDead && f.ForestRun.SeedsCollected == 0 && f.ForestRewardId != forestId
                && object.ReferenceEquals(stats, f.Stats) && Near(f.Stats.Health, 10f) && Near(f.Stats.Mana, 12f)
                && !f.ForestRun.Combat.PlayerAttackActive && f.ClearingRewardId == clearingId && f.ClearingRun.IsComplete,
                "R mixed terminal input mutated next attempt or original clearing");
            Expect(f.Store.Writes == 1 && f.Progress.Data.BankCoins == 30, "death/retry deposited unfinished forest"); f.AssertTemplate();
        });
        test("forest exit is open before objectives and death has an actionable retry then exit", () =>
        {
            var f = new Fixture().Entered(); f.Leave();
            Expect(f.ClearingRun.IsComplete && f.ForestRun.SeedsCollected == 0 && f.Progress.Data.BankCoins == 30,
                "early exit changed existing completed run or granted coins");
            f.Enter(); f.Stats.Health = 0f; f.Update(); f.Position(ThornwoodLayout.Entry); f.Update(KeyCode.E);
            Expect(f.Forest.Active && f.ForestRun.Combat.IsDead && !f.Controls, "dead actor bypassed its required retry");
            f.Update(KeyCode.R); f.Leave();
            Expect(!f.Forest.Active && f.ClearingRun.IsComplete && Near(f.Stats.Health, 10f), "death R then south exit has no recovery path");
        });
        test("failed cache save remains unbanked until actual E retries same identity once", () =>
        {
            var f = new Fixture().Entered(); f.Store.AcceptWrites = false; ClearingProgressData ledger = f.Progress.Data;
            f.CompleteForest(); string id = f.ForestRewardId;
            Expect(object.ReferenceEquals(ledger, f.Progress.Data) && f.Progress.SaveFailed && !Get<bool>(f.Forest, "rewardBanked")
                && f.ForestRun.Combat.IsComplete, "failed cache save changed live ledger or acceptance latch");
            f.Update(KeyCode.E); Expect(f.Store.Writes == 1 && f.ForestRewardId == id, "failed retry invented reward");
            f.Leave(); f.Enter(); f.Store.AcceptWrites = true; f.Position(ThornwoodLayout.Cache); f.Update(KeyCode.E);
            Expect(f.Store.Writes == 2 && f.Progress.Data.BankCoins == 60 && f.ForestRewardId == id && Get<bool>(f.Forest, "rewardBanked"),
                "leave/reenter lost recoverable failed transaction or duplicated identity");
            f.Update(KeyCode.E); Expect(f.Store.Writes == 2 && f.Progress.Data.BankCoins == 60, "saved forest E duplicated reward");
        });
        test("A clearing then B forest reward stays sixty through alternating E reentry and purchases", () =>
        {
            var f = new Fixture().Entered(); string clearingId = f.ClearingRewardId; f.CompleteForest(); string forestId = f.ForestRewardId;
            Expect(f.Progress.Data.BankCoins == 60 && f.Store.Writes == 2 && f.Progress.Data.LastRewardId == forestId, "setup did not move LastRewardId from A to B");
            f.Leave(); f.Update(KeyCode.E); f.Update(KeyCode.E);
            Expect(f.Progress.Data.BankCoins == 60 && f.Store.Writes == 2 && f.ClearingRewardId == clearingId,
                "returning to older A identity replayed thirty coins");
            f.Enter(); f.Position(ThornwoodLayout.Cache); f.Update(KeyCode.E); f.Leave(); f.Update();
            Expect(f.Progress.Data.BankCoins == 60 && f.Store.Writes == 2 && Get<bool>(f.Forest, "rewardBanked"),
                "alternating older B identity replayed thirty coins");
            string result = Get<Text>(f.Hud, "terminal").text.ToLowerInvariant();
            Expect(result.Contains("banked") && !result.Contains("unbanked"), "older clearing receipt became unbanked HUD after forest save");
            f.Update(KeyCode.Alpha1);
            Expect(f.Progress.Data.BankCoins == 30 && f.Progress.Data.VitalityRank == 1 && f.Store.Writes == 3,
                "older clearing receipt blocked upgrade purchase");
            f.Update(KeyCode.E); Expect(f.Progress.Data.BankCoins == 30 && f.Store.Writes == 3, "purchase then E replayed A reward");
        });
        test("forest completed movement stays enabled and repeated reentry never restocks or banks", () =>
        {
            var f = new Fixture().Entered(); f.CompleteForest(); PlayerStats stats = f.Stats;
            f.Stats.Health = 3f; f.Stats.Mana = 4f; string id = f.ForestRewardId;
            Expect(f.Controls, "completed cache locked exit movement");
            for (int i = 0; i < 3; i++) { f.Leave(); f.Enter(); f.Position(ThornwoodLayout.Cache); f.Update(KeyCode.E); }
            Expect(object.ReferenceEquals(stats, f.Stats) && Near(f.Stats.Health, 3f) && Near(f.Stats.Mana, 4f)
                && f.ForestRun.SeedsCollected == 3 && f.ForestRun.Combat.DefeatedCount == 3
                && f.ForestRewardId == id && f.Store.Writes == 2 && f.Progress.Data.BankCoins == 60,
                "completed reentry replayed rewards or refilled resources/content");
        });
        test("failed cache R explicitly loses only unsaved forest reward and creates a fresh attempt", () =>
        {
            var f = new Fixture().Entered(); f.Store.AcceptWrites = false; f.CompleteForest(); string id = f.ForestRewardId;
            string keys = Get<Text>(Get<ClearingHud>(f.Forest, "hud"), "skills").text.ToLowerInvariant();
            Expect(keys.Contains("r - new expedition") && keys.Contains("loses unbanked reward"),
                "failed cache HUD omitted the actionable restart key or unsaved reward loss");
            f.Update(KeyCode.R);
            Expect(f.Forest.Active && !f.ForestRun.Combat.IsComplete && f.ForestRun.SeedsCollected == 0
                && f.ForestRewardId != id && f.Progress.Data.BankCoins == 30 && f.Store.Writes == 1,
                "R carried unsaved rewards or retained old attempt");
            Expect(Get<string>(f.Forest, "feedback").ToLowerInvariant().Contains("not saved"), "unsaved reward loss is hidden");
        });
        test("new forest retry applies stored upgrades once without stacking or template mutation", () =>
        {
            var f = new Fixture().Entered(); f.Leave(); f.Update(KeyCode.Alpha1); f.Enter();
            Expect(Near(f.Stats.MaxHealth, 10f), "purchase modified already-started forest maxima");
            f.Stats.Health = 0f; f.Update(); f.Update(KeyCode.R);
            Expect(Near(f.Stats.MaxHealth, 12f) && Near(f.Stats.Health, 12f), "new forest attempt did not apply saved bonus");
            f.Stats.Health = 0f; f.Update(); f.Update(KeyCode.R);
            Expect(Near(f.Stats.MaxHealth, 12f) && Near(f.Stats.Health, 12f), "forest retries stacked saved bonus"); f.AssertTemplate();
        });
        test("completed forest R starts a new legitimate cache while older clearing stays banked", () =>
        {
            var f = new Fixture().Entered(); f.CompleteForest(); string firstForest = f.ForestRewardId;
            string keys = Get<Text>(Get<ClearingHud>(f.Forest, "hud"), "skills").text.ToLowerInvariant();
            Expect(keys.Contains("r - new expedition") && !keys.Contains("loses unbanked reward"),
                "saved cache HUD hid legitimate replay or advertised loss of an accepted reward");
            string clearing = f.ClearingRewardId; f.Update(KeyCode.R, KeyCode.J, KeyCode.K, KeyCode.E);
            Expect(f.Forest.Active && f.ForestRewardId != firstForest && !f.ForestRun.Combat.IsComplete
                && f.ForestRun.SeedsCollected == 0 && !f.ForestRun.Combat.PlayerAttackActive
                && f.Progress.Data.BankCoins == 60 && f.Store.Writes == 2,
                "terminal R reused a reward identity, paid early or processed the same-frame combat input");
            f.CompleteForest();
            Expect(f.Progress.Data.BankCoins == 90 && f.Progress.Data.ClearedRuns == 3 && f.Store.Writes == 3,
                "a genuinely completed new forest expedition was not banked exactly once");
            f.Leave(); f.Update(KeyCode.E); f.Update(KeyCode.E);
            Expect(f.ClearingRewardId == clearing && f.Progress.Data.BankCoins == 90 && f.Store.Writes == 3,
                "a later legitimate forest completion erased the original clearing acceptance latch");
        });
        test("clearing R discards unfinished forest and next verified clearing opens a fresh expedition", () =>
        {
            var f = new Fixture().Entered(); f.Position(ThornwoodLayout.SeedPosition(0)); f.Update(KeyCode.E);
            string firstForest = f.ForestRewardId, firstClearing = f.ClearingRewardId;
            f.Stats.Health = 3f; f.Stats.Mana = 2f; f.Leave(); f.Update(KeyCode.R, KeyCode.F);
            Expect(!f.Forest.Active && !f.ClearingRun.IsComplete && f.ForestRun.SeedsCollected == 0
                && f.ClearingRewardId != firstClearing && f.ForestRewardId == ""
                && f.Controls && f.Progress.Data.BankCoins == 30 && f.Store.Writes == 1,
                "a new clearing retained an old expedition, admitted F or deposited an unfinished forest");
            f.Position(ThornwoodLayout.ReturnToClearing); f.Update(KeyCode.F);
            Expect(!f.Forest.Active, "previous saved clearing unlocked the next unfinished clearing");
            f.FinishClearing(); f.Enter();
            Expect(f.ForestRewardId != firstForest && f.ForestRun.SeedsCollected == 0
                && Near(f.Stats.Health, 10f) && Near(f.Stats.Mana, 12f)
                && f.Progress.Data.BankCoins == 60 && f.Store.Writes == 2,
                "fresh verified clearing did not create exactly one independent expedition"); f.AssertTemplate();
        });
        test("leaving failed cache then clearing R discloses loss without mutating protected ledger", () =>
        {
            var f = new Fixture().Entered(); f.Store.AcceptWrites = false; f.CompleteForest();
            ClearingProgressData ledger = f.Progress.Data;
            Expect(f.Forest.HasUnbankedReward, "failed cache was not exposed to the clearing reset owner");
            f.Leave(); f.Update(KeyCode.R);
            Expect(!f.Forest.HasUnbankedReward && !f.Forest.Active && !f.ClearingRun.IsComplete
                && object.ReferenceEquals(ledger, f.Progress.Data) && f.Progress.Data.BankCoins == 30 && f.Store.Writes == 1,
                "discarding failed cache reset acceptance incorrectly or mutated the saved ledger");
            Expect(Get<string>(f.Clearing, "feedback").ToLowerInvariant().Contains("forest reward was not saved"),
                "clearing reset did not disclose the retained forest reward it discarded");
        });
        test("simultaneous active stalker contacts share the actor immunity across enemy iteration", () =>
        {
            var f = new Fixture().Entered(); Vector2 feet = new Vector2(22f, -3f); f.Position(feet);
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
            {
                Vector2 origin = feet + Vector2.down * .7f; f.EnemyBody(i).position = origin;
                Expect(f.ForestRun.TryBeginStalkerAttack(i, origin.x, origin.y, feet.x, feet.y, true),
                    "simultaneous pounce setup was rejected");
            }
            f.ForestRun.Advance(ClearingRun.WindupDuration(SentinelAttackKind.Lance)); f.Tick(); f.Update();
            Expect(Near(f.Stats.Health, 8f) && Near(Get<float>(f.Number(3), "Amount"), 2f)
                && f.ForestRun.Combat.PlayerInvulnerabilityRemaining > 0f,
                "three same-tick stalkers bypassed shared actor immunity or summed rejected damage");
        });
        test("completed forest permits exit movement but neither new attacks nor escaping the bounds", () =>
        {
            var f = new Fixture().Entered(); f.CompleteForest(); f.Tick();
            Vector2 safe = f.Body.position + ClearingVisuals.PlayerFootOffset;
            float health = f.Stats.Health, mana = f.Stats.Mana; f.Update(KeyCode.J, KeyCode.K);
            f.Position(new Vector2(31f, 0f)); f.Tick();
            Expect(f.Controls && !f.ForestRun.Combat.PlayerAttackActive && Near(f.Stats.Health, health)
                && Near(f.Stats.Mana, mana) && Near(f.Body.position + ClearingVisuals.PlayerFootOffset, safe),
                "completed forest admitted combat, spent resources or disabled post-objective bounds validation");
            f.Leave(); Expect(!f.Forest.Active, "completed forest cannot use the south exit");
        });
        test("actual runtime rejects bounds bypass and restores last accepted full-foot position", () =>
        {
            var f = new Fixture().Entered(); Vector2 safe = f.Body.position + ClearingVisuals.PlayerFootOffset;
            foreach (Vector2 escape in new[] { new Vector2(31f, 0f), new Vector2(13f, -4f), new Vector2(22f, 6f), new Vector2(22f, -6f) })
            {
                f.Position(escape); f.Tick();
                Expect(Near(f.Body.position + ClearingVisuals.PlayerFootOffset, safe), "outside boundary was accepted: " + escape.x + "," + escape.y);
            }
            f.Position(new Vector2(22f, -3f)); f.Tick(); safe = f.Body.position + ClearingVisuals.PlayerFootOffset;
            f.Position(new Vector2(26f, -3f)); f.Tick();
            Expect(Near(f.Body.position + ClearingVisuals.PlayerFootOffset, safe), "large step tunneled through full root wall");
        });
        test("forest actual boundary and obstacle colliders consume the shared layout exactly", () =>
        {
            var f = new Fixture(); ThornwoodVisuals visuals = Get<ThornwoodVisuals>(f.Forest, "visuals");
            int boundaries = 0, obstacles = 0;
            foreach (Transform child in visuals.Root.transform.Children)
            {
                string name = child.gameObject.name; BoxCollider2D collider = child.gameObject.GetComponent<BoxCollider2D>();
                if (name.StartsWith("Thornwood continuous boundary "))
                {
                    int id = int.Parse(name.Substring("Thornwood continuous boundary ".Length)); var rect = ThornwoodLayout.GetBoundary(id);
                    Expect(collider != null && child.gameObject.layer == 0 && !collider.isTrigger
                        && Near(child.position.x, rect.X) && Near(child.position.y, rect.Y)
                        && Near(collider.size, new Vector2(rect.Width, rect.Height)), "boundary collider diverges from layout"); boundaries++;
                }
                else if (name.StartsWith("Thornwood blocking roots "))
                {
                    int id = int.Parse(name.Substring("Thornwood blocking roots ".Length)); var rect = ThornwoodLayout.GetObstacle(id);
                    Expect(collider != null && child.gameObject.layer == 0 && Near(child.position.x, rect.X)
                        && Near(child.position.y, rect.Y) && Near(collider.size, new Vector2(rect.Width, rect.Height)), "root collider diverges from layout"); obstacles++;
                }
            }
            Expect(boundaries == 4 && boundaries == ThornwoodLayout.BoundaryCount && obstacles == ThornwoodLayout.ObstacleCount,
                "forest omitted expected continuous boundary or root colliders");
        });
        test("forest cached pixel poses warnings and owned objects allocate only at construction", () =>
        {
            var f = new Fixture().Entered(); f.StageSingleEnemy(); int objects = GameObject.CreatedCount;
            int textures = Texture2D.CreatedCount, sprites = Sprite.CreatedCount;
            f.Update(KeyCode.J); f.Tick(4); f.Update(); f.Update(KeyCode.Escape); f.Tick(6); f.Update(); f.Update(KeyCode.Escape);
            f.Leave(); f.Enter(); f.Update();
            Expect(GameObject.CreatedCount == objects && Texture2D.CreatedCount == textures && Sprite.CreatedCount == sprites,
                "combat pause or reentry allocated new feedback/pose resources");
            Sprite[] poses = Get<Sprite[]>(Get<ThornwoodVisuals>(f.Forest, "visuals"), "poses");
            Expect(poses.Length >= 6 && poses[0] != poses[3] && poses[3] != poses[4] && poses[0] != poses[5],
                "stalker lacks cached walk/windup/pounce/hurt poses");
            var art = Get<ThornwoodVisuals>(f.Forest, "visuals"); int trees = 0;
            foreach (Transform obstacle in art.Root.transform.Children)
                foreach (Transform decoration in obstacle.Children)
                {
                    if (!decoration.gameObject.name.StartsWith("Thornwood thorn tree ")) continue;
                    Sprite sprite = decoration.gameObject.GetComponent<SpriteRenderer>().sprite;
                    Expect(Near(sprite.RecordedPixelsPerUnit, 30f) && sprite.RecordedTexture.filterMode == FilterMode.Point
                        && Near(sprite.RecordedPivot.x * sprite.RecordedRect.width,
                            (float)Math.Round(sprite.RecordedPivot.x * sprite.RecordedRect.width))
                        && Near(sprite.RecordedPivot.y * sprite.RecordedRect.height,
                            (float)Math.Round(sprite.RecordedPivot.y * sprite.RecordedRect.height))
                        && Near(decoration.localPosition.x * 30f, (float)Math.Round(decoration.localPosition.x * 30f))
                        && Near(decoration.localPosition.y * 30f, (float)Math.Round(decoration.localPosition.y * 30f)),
                        "recorded static tree placement or raster pivot falls between authored PPU30 pixels");
                    trees++;
                }
            Expect(trees > 0, "pixel-grid checks inspected no owned forest trees");
        });
        test("forest disposal destroys owned assets once and retains clearing borrowed square", () =>
        {
            var f = new Fixture().Entered(); var shared = Get<ClearingVisuals>(f.Clearing, "visuals"); Sprite pixel = Get<Sprite>(shared, "pixel");
            ThornwoodVisuals visuals = Get<ThornwoodVisuals>(f.Forest, "visuals"); Sprite[] poses = Get<Sprite[]>(visuals, "poses");
            GameObject root = visuals.Root; f.Forest.Dispose(); f.Forest.Dispose();
            Expect(root.Destroyed && !pixel.Destroyed && poses[0].Destroyed && !f.Forest.Active,
                "forest disposal leaked owned assets or destroyed shared clearing art");
            Expect(!f.Forest.Enter(), "disposed forest can reenter");
        });
    }
}
