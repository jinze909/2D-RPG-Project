using System;
using System.Collections.Generic;
using Rpg.Gameplay;

// Actual production C# rules and collision geometry; no Unity or physics doubles.
internal static class ThornwoodChecks
{
    private const string ClearingReward = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ForestReward = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static int passed, failed;
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Equal(double a, double b, string message) { Require(Math.Abs(a - b) < .00001d, message); }
    private static void Check(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static ThornwoodRun Entered()
    {
        var run = new ThornwoodRun();
        Require(run.TryEnter(true, true, true, false), "forest setup entry rejected");
        return run;
    }
    private static void CollectAll(ThornwoodRun run)
    {
        for (int id = 0; id < ThornwoodRun.SeedCount; id++)
        {
            ThornwoodPoint point = ThornwoodLayout.SeedPosition(id);
            Require(run.TryCollectSeed(id, point.X, point.Y, true), "seed setup rejected");
        }
    }
    private static void DefeatAll(ThornwoodRun run)
    {
        for (int strike = 0; strike < 3; strike++)
        {
            run.Advance(ClearingRun.LightCooldown);
            long token;
            Require(run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token), "attack setup rejected");
            for (int id = 0; id < ClearingRun.SentinelCount; id++)
                Require(run.TryHitStalker(token, id), "stalker setup contact rejected");
        }
    }
    private static ThornwoodRun Completed()
    {
        var run = Entered(); CollectAll(run); DefeatAll(run);
        Require(run.TryComplete(ThornwoodLayout.Cache.X, ThornwoodLayout.Cache.Y, true), "cache setup rejected");
        return run;
    }
    private static bool StartPounce(ThornwoodRun run, int id = 0)
    { return run.TryBeginStalkerAttack(id, 22, -2, 22, -1, true); }
    private static void ThrowsArgument(Action body)
    {
        bool threw = false; try { body(); } catch (ArgumentOutOfRangeException) { threw = true; }
        Require(threw, "invalid layout index did not reject");
    }
    private sealed class RecordingStore : IClearingProgressStore
    {
        internal ClearingProgressData Stored = ClearingProgressData.Fresh;
        internal bool Fail;
        internal int Saves;
        public ProgressLoadResult Load() { return new ProgressLoadResult(Stored, ProgressLoadKind.New, true); }
        public bool TrySave(ClearingProgressData expected, ClearingProgressData next)
        {
            Saves++;
            if (Fail || expected != Stored) return false;
            Stored = next; return true;
        }
    }
    public static int Main()
    {
        Check("forest starts locked and empty without a Boss or resource/save owner", () =>
        {
            var run = new ThornwoodRun();
            Require(!run.Entered && !run.Paused && !run.IsPlayable && !run.CacheReady && run.SeedsCollected == 0,
                "fresh expedition state");
            Require(!run.Combat.RequiresBoss && !run.Combat.Boss.IsAwake && !run.Combat.GateUnlocked &&
                run.Combat.RewardCoins == 0 && run.Combat.DefeatedCount == 0, "forest rules changed baseline");
            for (int id = 0; id < 3; id++)
                Require(run.Combat.GetSentinel(id).Health == 54 && !run.IsSeedCollected(id) &&
                    run.GetStalkerFootprint(id) == null, "fresh stalker/seed/snapshot");
        });
        Check("entry requires this clearing completion and its committed reward", () =>
        {
            foreach (bool complete in new[] { false, true }) foreach (bool banked in new[] { false, true })
            {
                Require(ThornwoodRun.IsUnlocked(complete, banked) == (complete && banked), "unlock condition");
                var run = new ThornwoodRun();
                Require(run.TryEnter(complete, banked, true, false) == (complete && banked), "entry condition");
            }
        });
        Check("dead paused duplicate entry and actions outside the forest are rejected", () =>
        {
            var run = new ThornwoodRun(); long token;
            Require(!run.TryEnter(true, true, false, false) && !run.TryEnter(true, true, true, true), "entry state gate");
            Require(!run.TryCollectSeed(0, 18, -.8f, true) && !run.TryComplete(22, 3.5f, true) &&
                !run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token) && token == 0 && !StartPounce(run),
                "outside forest admitted action");
            run.Advance(5); Equal(run.Combat.Time, 0, "outside forest clock advanced");
            Require(run.TryEnter(true, true, true, false) && !run.TryEnter(true, true, true, false), "duplicate entry");
        });
        Check("each seed requires E distance LOS and collection remains once per expedition", () =>
        {
            var run = Entered();
            for (int id = 0; id < 3; id++)
            {
                ThornwoodPoint p = ThornwoodLayout.SeedPosition(id);
                Require(!run.TryCollectSeed(id, p.X + .7501f, p.Y, true) &&
                    !run.TryCollectSeed(id, p.X, p.Y, false), "seed distance/LOS gate");
                Require(run.TryCollectSeed(id, p.X + .75f, p.Y, true) &&
                    !run.TryCollectSeed(id, p.X, p.Y, true) && run.IsSeedCollected(id), "seed edge or idempotence");
                Require(run.SeedsCollected == id + 1 && run.Combat.RewardCoins == 0, "seed created coins");
            }
            Require(!run.CacheReady && !run.Combat.IsComplete, "seeds alone completed expedition");
        });
        Check("seed invalid IDs nonfinite and outside-world positions preserve collection", () =>
        {
            var run = Entered();
            foreach (int id in new[] { -1, 3, int.MinValue, int.MaxValue })
                Require(!run.TryCollectSeed(id, 18, -.8f, true) && !run.IsSeedCollected(id), "invalid seed ID");
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue })
                Require(!run.TryCollectSeed(0, invalid, -.8f, true) && !run.TryCollectSeed(0, 18, invalid, true), "invalid point");
            Require(run.SeedsCollected == 0 && run.Combat.RewardCoins == 0, "invalid seed action mutated state");
        });
        Check("seed admission independently verifies shared-layout LOS", () =>
        {
            var run = Entered();
            // Seed itself is unobstructed. Caller false remains authoritative;
            // arbitrary far or inside-trunk positions cannot bypass its geometry.
            Require(!run.TryCollectSeed(0, 20, -.6f, true) && !run.TryCollectSeed(1, 28, 1.3f, true), "trunk admitted seed");
            Require(run.SeedsCollected == 0, "blocked seed state mutated");
        });
        Check("cache needs both three seeds and all three living enemies defeated", () =>
        {
            var seedsOnly = Entered(); CollectAll(seedsOnly);
            Require(!seedsOnly.CacheReady && !seedsOnly.TryComplete(22, 3.5f, true), "seeds-only opened cache");
            var enemiesOnly = Entered(); DefeatAll(enemiesOnly);
            Require(enemiesOnly.Combat.GateUnlocked && enemiesOnly.Combat.RewardCoins == 30 &&
                !enemiesOnly.CacheReady && !enemiesOnly.TryComplete(22, 3.5f, true), "enemies-only opened cache");
            CollectAll(enemiesOnly);
            Require(enemiesOnly.CacheReady && !enemiesOnly.Combat.IsComplete, "cache-ready completed without E");
        });
        Check("cache E distance LOS finite gates and once-only completion", () =>
        {
            var run = Entered(); CollectAll(run); DefeatAll(run);
            Require(!run.TryComplete(22, 4.2501f, true) && !run.TryComplete(22, 3.5f, false) &&
                !run.TryComplete(float.NaN, 3.5f, true) && !run.TryComplete(22, float.PositiveInfinity, true), "cache admission gate");
            Require(run.TryComplete(22, 4.25f, true) && !run.TryComplete(22, 3.5f, true) &&
                run.Combat.IsComplete && run.Combat.RewardCoins == 30 && !run.IsPlayable, "cache edge/completion");
            double time = run.Combat.Time; run.Advance(20); Equal(time, run.Combat.Time, "completed clock advanced");
        });
        Check("completed forest banks exactly existing v1 thirty coins once", () =>
        {
            var run = Entered(); var store = new RecordingStore(); var progress = new ClearingProgress(store);
            CollectAll(run);
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.RunIncomplete &&
                store.Saves == 0 && progress.Data.BankCoins == 0, "seeds banked reward");
            DefeatAll(run);
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.RunIncomplete && store.Saves == 0,
                "defeats banked before cache");
            Require(run.TryComplete(22, 3.5f, true), "cache rejected");
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.Saved &&
                progress.Data.BankCoins == 30 && progress.Data.ClearedRuns == 1 && progress.Data.Revision == 1 &&
                progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.AlreadyBanked && store.Saves == 1,
                "v1 reward or duplicate save");
        });
        Check("failed forest reward save changes no balance and does not block leaving", () =>
        {
            var run = Completed(); var store = new RecordingStore { Fail = true }; var progress = new ClearingProgress(store);
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.SaveFailed &&
                progress.Data.BankCoins == 0 && !progress.IsCompletionBanked(ForestReward), "failed save mutated balance");
            Require(run.TryLeave() && run.TryEnter(true, true, true, false), "save failure trapped return/reentry");
            store.Fail = false;
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.Saved &&
                progress.Data.BankCoins == 30, "existing completed reward could not retry save");
        });
        Check("forest reward naturally buys next-run growth without changing v1 invariant", () =>
        {
            var store = new RecordingStore(); var progress = new ClearingProgress(store); var run = Completed();
            Require(progress.BankCompletion(run.Combat, ForestReward) == ProgressActionResult.Saved &&
                progress.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.Saved && progress.BonusHealth == 2 &&
                progress.Data.BankCoins == 0 && progress.Data.ClearedRuns == 1 && progress.Data.Revision == 2,
                "forest reward growth bridge");
            Require(run.Combat.RewardCoins == 30 && run.Combat.IsComplete, "purchase rewrote attempt");
        });
        Check("clearing completion and forest reward use distinct ledger identities", () =>
        {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            var clearing = new ClearingRun();
            for (int strike = 0; strike < 3; strike++)
            {
                clearing.Advance(.46f); long token;
                Require(clearing.BeginPlayerAttack(PlayerAttackKind.Light, 0, out token), "clear setup attack");
                for (int id = 0; id < 3; id++) Require(clearing.TryHitSentinel(token, id), "clear setup hit");
            }
            Require(clearing.TryCompleteObjective() && progress.BankCompletion(clearing, ClearingReward) == ProgressActionResult.Saved,
                "clearing baseline save");
            var forest = Completed();
            Require(progress.BankCompletion(forest.Combat, ForestReward) == ProgressActionResult.Saved &&
                progress.Data.BankCoins == 60 && progress.Data.ClearedRuns == 2 && store.Saves == 2,
                "forest reward collided with clearing");
        });
        Check("stalkers pursue only while ready and close enough with clear LOS", () =>
        {
            var run = Entered();
            Require(run.ChooseStalkerAction(0, 2.4f, true) == SentinelDecision.Attack &&
                run.ChooseStalkerAction(0, 2.4001f, true) == SentinelDecision.Approach &&
                run.ChooseStalkerAction(0, 5.99f, true) == SentinelDecision.Approach &&
                run.ChooseStalkerAction(0, 6f, true) == SentinelDecision.Hold &&
                run.ChooseStalkerAction(0, 1f, false) == SentinelDecision.Hold, "chase/pounce gates");
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
                Require(run.ChooseStalkerAction(0, invalid, true) == SentinelDecision.Hold, "invalid decision distance");
            Require(run.ChooseStalkerAction(-1, 0, true) == SentinelDecision.Hold, "invalid decision id");
        });
        Check("pounce captures immutable aim and uses existing Lance warning timing", () =>
        {
            var run = Entered(); Require(StartPounce(run), "pounce rejected");
            var footprint = run.GetStalkerFootprint(0); var enemy = run.Combat.GetSentinel(0);
            Require(footprint != null && enemy.AttackKind == SentinelAttackKind.Lance &&
                enemy.AttackPhase == SentinelAttackPhase.Telegraph && footprint.Contains(22, -1), "captured warning");
            Require(!run.TryBeginStalkerAttack(0, 22, -2, 23, -2, true) &&
                object.ReferenceEquals(footprint, run.GetStalkerFootprint(0)), "active aim replaced");
            run.Advance(.4f);
            Require(enemy.AttackPhase == SentinelAttackPhase.Telegraph && enemy.PhaseProgress > .49f &&
                run.ChooseStalkerAction(0, 4, true) == SentinelDecision.Hold, "telegraph moved enemy");
            run.Advance(.4f); Require(enemy.AttackPhase == SentinelAttackPhase.Active, ".8s warning changed");
            run.Advance(.17f); Require(enemy.AttackPhase == SentinelAttackPhase.Recovery, ".16s contact changed");
            Require(run.ChooseStalkerAction(0, 4, true) == SentinelDecision.Hold, "recovery admitted motion");
            run.Advance(1f);
            Require(enemy.AttackPhase == SentinelAttackPhase.Ready && run.GetStalkerFootprint(0) == null,
                "one-second recovery or footprint cleanup changed");
        });
        Check("pounce origin target range nonfinite ID and real trunk LOS reject safely", () =>
        {
            var run = Entered();
            Require(!run.TryBeginStalkerAttack(-1, 22, -2, 22, -1, true) &&
                !run.TryBeginStalkerAttack(3, 22, -2, 22, -1, true) &&
                !run.TryBeginStalkerAttack(0, 22, -2, 22, .401f, true) &&
                !run.TryBeginStalkerAttack(0, 22, -2, 22, -1, false) &&
                !run.TryBeginStalkerAttack(0, 19.2f, -.6f, 20.8f, -.6f, true), "pounce admission");
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue })
                Require(!run.TryBeginStalkerAttack(0, bad, -2, 22, -1, true) &&
                    !run.TryBeginStalkerAttack(0, 22, -2, 22, bad, true), "nonfinite pounce admitted");
            Require(run.GetStalkerFootprint(0) == null && run.Combat.GetSentinel(0).AttackSequence == 0,
                "rejected pounce mutated snapshot/token");
        });
        Check("pounce contact requires active captured area LOS and resolves once", () =>
        {
            var run = Entered(); Require(StartPounce(run), "pounce setup");
            Require(!run.TryResolveStalkerHit(0, 22, -1, true), "telegraph dealt damage");
            run.Advance(.8f);
            Require(!run.TryResolveStalkerHit(0, 24, -1, true) && !run.TryResolveStalkerHit(0, 22, -1, false) &&
                !run.TryResolveStalkerHit(0, float.NaN, -1, true), "unwarned/blocked contact");
            Require(run.TryResolveStalkerHit(0, 22, -1, true) && !run.TryResolveStalkerHit(0, 22, -1, true) &&
                run.Combat.PlayerInvulnerabilityRemaining > .54f, "once contact/shared immunity");
        });
        Check("simultaneous stalkers share player immunity and skipped active frames do not hit", () =>
        {
            var run = Entered(); Require(StartPounce(run, 0) && StartPounce(run, 1), "pounce setup");
            run.Advance(.8f);
            Require(run.TryResolveStalkerHit(0, 22, -1, true) && !run.TryResolveStalkerHit(1, 22, -1, true),
                "simultaneous immunity bypass");
            run.Advance(.6f); Require(!run.TryResolveStalkerHit(1, 22, -1, true), "delayed contact after immunity");
            var hitch = Entered(); Require(StartPounce(hitch), "hitch setup"); hitch.Advance(1f);
            Require(!hitch.TryResolveStalkerHit(0, 22, -1, true), "skipped active hit");
        });
        Check("light burst HP contact windows costs and cooldowns remain existing authority", () =>
        {
            var run = Entered(); long token = 0, rejected;
            Require(!run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 5.999f, out rejected) && rejected == 0 &&
                run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token), "attack MP/light admission");
            Require(run.TryHitStalker(token, 0) && !run.TryHitStalker(token, 0) &&
                run.Combat.GetSentinel(0).Health == 36 && run.Combat.LightCooldownRemaining == .45f,
                "light actual damage/token/CD");
            Require(!run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 6, out rejected), "overlapping player action");
            run.Advance(.2f);
            Require(!run.TryHitStalker(token, 1) && run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 6, out token) &&
                run.TryHitStalker(token, 0) && run.Combat.GetSentinel(0).Health == 6 &&
                run.Combat.BurstCooldownRemaining > 1.19f, "burst actual damage/window/CD");
            Equal(ClearingRun.BurstManaCost, 6, "burst cost changed");
        });
        Check("overkill death cleans warning and awards each stalker only once", () =>
        {
            var run = Entered(); Require(StartPounce(run), "enemy setup");
            for (int strike = 0; strike < 2; strike++)
            {
                run.Advance(1.21f); long token;
                Require(run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 6, out token) && run.TryHitStalker(token, 0), "burst setup");
            }
            Require(run.Combat.GetSentinel(0).Health == 0 && run.Combat.GetSentinel(0).IsDead &&
                run.GetStalkerFootprint(0) == null && run.Combat.DefeatedCount == 1 && run.Combat.RewardCoins == 10 &&
                !StartPounce(run) && run.ChooseStalkerAction(0, 0, true) == SentinelDecision.Hold, "dead lifecycle");
            run.Advance(1.21f); long later;
            Require(run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 6, out later) && !run.TryHitStalker(later, 0) &&
                run.Combat.RewardCoins == 10, "corpse reward repeated");
        });
        Check("pause freezes warning cooldown seed cache and contact admission", () =>
        {
            var run = Entered(); Require(StartPounce(run), "pounce setup");
            long token = 0; Require(run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token), "light setup");
            run.Advance(.4f); double time = run.Combat.Time; var footprint = run.GetStalkerFootprint(0);
            float progress = run.Combat.GetSentinel(0).PhaseProgress, cooldown = run.Combat.LightCooldownRemaining;
            run.SetPaused(true); run.Advance(100f);
            Require(!run.IsPlayable && !run.TryCollectSeed(0, 18, -.8f, true) && !run.TryComplete(22, 3.5f, true) &&
                !run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token) && !run.TryHitStalker(token, 0) &&
                !run.TryResolveStalkerHit(0, 22, -1, true), "paused action admitted");
            Equal(time, run.Combat.Time, "pause clock advanced"); Equal(progress, run.Combat.GetSentinel(0).PhaseProgress, "pause progress");
            Equal(cooldown, run.Combat.LightCooldownRemaining, "pause refunded cooldown");
            Require(object.ReferenceEquals(footprint, run.GetStalkerFootprint(0)), "pause canceled snapshot");
            run.SetPaused(false); run.Advance(.4f); Require(run.TryResolveStalkerHit(0, 22, -1, true), "resume contact failed");
        });
        Check("leave is always available and reentry retains collection health and cooldowns", () =>
        {
            var run = Entered(); Require(run.TryCollectSeed(0, 18, -.8f, true), "seed setup");
            Require(StartPounce(run), "enemy setup"); long token;
            Require(run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token) && run.TryHitStalker(token, 0), "HP setup");
            float cooldown = run.Combat.LightCooldownRemaining;
            Require(run.TryLeave() && !run.TryLeave() && !run.Entered && run.SeedsCollected == 1 &&
                run.Combat.GetSentinel(0).Health == 36 && run.GetStalkerFootprint(0) == null &&
                !run.Combat.PlayerAttackActive, "leave reset persistent attempt or retained transient");
            Equal(cooldown, run.Combat.LightCooldownRemaining, "leave refunded CD");
            double time = run.Combat.Time; run.Advance(100); Equal(time, run.Combat.Time, "outside expedition advanced");
            Require(run.TryEnter(true, true, true, false) && !run.TryHitStalker(token, 1) &&
                run.SeedsCollected == 1 && run.Combat.GetSentinel(0).Health == 36, "reentry reset or revived old contact");
        });
        Check("leave remains available while paused dead or complete", () =>
        {
            var paused = Entered(); paused.SetPaused(true); Require(paused.TryLeave(), "pause trapped exit");
            var dead = Entered(); dead.NotifyPlayerDeath(); Require(dead.TryLeave(), "defeat trapped exit");
            var complete = Completed(); Require(complete.TryLeave() && complete.TryEnter(true, true, true, false) &&
                complete.Combat.IsComplete && !complete.IsPlayable && complete.TryLeave(), "complete trapped exit/reentry");
        });
        Check("disable Suspend clears contacts but retains attempt progress and timing", () =>
        {
            var run = Entered(); Require(run.TryCollectSeed(1, 26, .5f, true) && StartPounce(run), "disable setup");
            long token; Require(run.TryBeginPlayerAttack(PlayerAttackKind.Burst, 6, out token) && run.TryHitStalker(token, 1), "attack setup");
            run.Suspend();
            Require(run.Entered && run.SeedsCollected == 1 && run.Combat.GetSentinel(1).Health == 24 &&
                run.GetStalkerFootprint(0) == null && !run.Combat.PlayerAttackActive && !run.TryHitStalker(token, 0),
                "Suspend forgot state or revived action");
            Equal(run.Combat.BurstCooldownRemaining, 1.2f, "Suspend refunded burst");
        });
        Check("death freezes expedition and retry resets every actor objective and warning", () =>
        {
            var run = Entered(); CollectAll(run); Require(StartPounce(run), "pounce setup");
            long token; Require(run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out token) && run.TryHitStalker(token, 0), "attack setup");
            run.NotifyPlayerDeath(); double time = run.Combat.Time; run.Advance(20);
            Require(run.Combat.IsDead && !run.IsPlayable && run.GetStalkerFootprint(0) == null &&
                !run.TryCollectSeed(0, 18, -.8f, true) && !run.TryComplete(22, 3.5f, true), "death actions");
            Equal(time, run.Combat.Time, "dead clock advanced");
            run.Reset();
            Require(!run.Entered && !run.Paused && !run.Combat.IsDead && !run.Combat.IsComplete && run.SeedsCollected == 0 &&
                run.Combat.RewardCoins == 0 && run.Combat.DefeatedCount == 0 && run.Combat.Time == 0 &&
                run.Combat.GetSentinel(0).Health == 54 && !run.IsSeedCollected(0), "retry incomplete");
            Require(run.TryEnter(true, true, true, false), "retry entry"); long newToken;
            Require(run.TryBeginPlayerAttack(PlayerAttackKind.Light, 0, out newToken) && newToken != token &&
                !run.TryHitStalker(token, 0) && run.TryHitStalker(newToken, 0), "retry reused stale token");
        });
        Check("nonpositive and nonfinite simulation deltas preserve all phase clocks", () =>
        {
            var run = Entered(); Require(StartPounce(run), "setup");
            foreach (float dt in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) run.Advance(dt);
            Require(run.Combat.Time == 0 && run.Combat.GetSentinel(0).PhaseProgress == 0 &&
                run.Combat.GetSentinel(0).AttackPhase == SentinelAttackPhase.Telegraph, "invalid delta advanced warning");
        });
        Check("shared layout positions rectangles and returned values are immutable copies", () =>
        {
            Require(ThornwoodLayout.MinX == 14 && ThornwoodLayout.MaxX == 30 && ThornwoodLayout.MinY == -5 &&
                ThornwoodLayout.MaxY == 5 && ThornwoodLayout.CenterX == 22 && ThornwoodLayout.FootRadius == .23f &&
                ThornwoodLayout.ObstacleCount == 6 && ThornwoodLayout.BoundaryCount == 4, "layout contract");
            var entry = ThornwoodLayout.Entry; var cache = ThornwoodLayout.Cache;
            Require(entry.X == 22 && entry.Y == -3.8f && cache.X == 22 && cache.Y == 3.5f, "entry/cache contract");
            var returning = ThornwoodLayout.ReturnToClearing;
            Require(returning.X == 0 && returning.Y == 3.35f,
                "return must reach the saved clearing beacon because completed clearing movement is locked");
            var wall = ThornwoodLayout.GetObstacle(0); wall = new ThornwoodRect(0, 0, 100, 100);
            Require(ThornwoodLayout.GetObstacle(0).X == 16, "returned copy mutated shared layout");
            ThrowsArgument(() => ThornwoodLayout.GetObstacle(-1)); ThrowsArgument(() => ThornwoodLayout.GetBoundary(4));
            ThrowsArgument(() => ThornwoodLayout.SeedPosition(3)); ThrowsArgument(() => ThornwoodLayout.EnemySpawn(-1));
        });
        Check("foot circle rejects outside perimeter and trunk overlap including invalid geometry", () =>
        {
            Require(ThornwoodLayout.CanOccupy(22, 0) && !ThornwoodLayout.CanOccupy(13.99f, 0) &&
                !ThornwoodLayout.CanOccupy(30.01f, 0) && !ThornwoodLayout.CanOccupy(22, -5.01f) &&
                !ThornwoodLayout.CanOccupy(22, 5.01f), "outside perimeter accepted");
            Require(!ThornwoodLayout.CanOccupy(14.1f, 0) && !ThornwoodLayout.CanOccupy(29.9f, 0) &&
                !ThornwoodLayout.CanOccupy(22, -4.9f) && !ThornwoodLayout.CanOccupy(22, 4.9f), "foot-radius boundary gap");
            for (int id = 0; id < ThornwoodLayout.ObstacleCount; id++)
            {
                var wall = ThornwoodLayout.GetObstacle(id);
                Require(!ThornwoodLayout.CanOccupy(wall.X, wall.Y) &&
                    !ThornwoodLayout.CanOccupy(wall.X + wall.Width * .5f + .22f, wall.Y), "trunk/radius overlap accepted");
            }
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Require(!ThornwoodLayout.CanOccupy(bad, 0) && !ThornwoodLayout.CanOccupy(22, bad) &&
                    !ThornwoodLayout.CanOccupy(22, 0, bad), "nonfinite layout point/radius");
            Require(!ThornwoodLayout.CanOccupy(22, 0, -.1f), "negative radius accepted");
        });
        Check("all four runtime collider rectangles seal full perimeter and overlap corners", () =>
        {
            for (int sample = 0; sample <= 160; sample++)
            {
                float x = 14 + sample * .1f;
                Require(CoveredByBoundary(x, -5) && CoveredByBoundary(x, 5), "north/south boundary gap at " + x);
            }
            for (int sample = 0; sample <= 100; sample++)
            {
                float y = -5 + sample * .1f;
                Require(CoveredByBoundary(14, y) && CoveredByBoundary(30, y), "west/east boundary gap at " + y);
            }
            Require(CoveredByBoundary(13.8f, -5.2f) && CoveredByBoundary(30.2f, 5.2f) &&
                CoveredByBoundary(30.2f, -5.2f) && CoveredByBoundary(13.8f, 5.2f), "corner overlap missing");
        });
        Check("swept movement cannot tunnel trunks and LOS uses the same world rectangles", () =>
        {
            Require(ThornwoodLayout.CanOccupy(18.5f, -.6f) && ThornwoodLayout.CanOccupy(21.5f, -.6f) &&
                !ThornwoodLayout.CanTravel(18.5f, -.6f, 21.5f, -.6f) &&
                !ThornwoodLayout.HasLineOfSight(18.5f, -.6f, 21.5f, -.6f), "trunk tunneling/LOS");
            Require(ThornwoodLayout.CanTravel(22, -3.8f, 22, 3.5f) && ThornwoodLayout.HasLineOfSight(22, -3.8f, 22, 3.5f),
                "central exit/cache route blocked");
            Require(!ThornwoodLayout.CanTravel(22, 0, 31, 0) &&
                !ThornwoodLayout.HasLineOfSight(22, 0, float.NaN, 0), "outside/nonfinite sweep admitted");
        });
        Check("radius-safe continuous route segments connect every sampled walkable cell and all objectives", () =>
        {
            const int width = 77, height = 47;
            var reachable = new bool[width, height]; var walkable = new bool[width, height];
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
                walkable[x, y] = ThornwoodLayout.CanOccupy(GridX(x), GridY(y));
            int startX = 38, startY = 4; Require(walkable[startX, startY], "entry grid obstructed");
            var queue = new Queue<int>(); queue.Enqueue(startX * height + startY); reachable[startX, startY] = true;
            int[] directionsX = { 1, -1, 0, 0 }, directionsY = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                int node = queue.Dequeue(), x = node / height, y = node % height;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + directionsX[d], ny = y + directionsY[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || reachable[nx, ny] || !walkable[nx, ny]) continue;
                    if (!ThornwoodLayout.CanTravel(GridX(x), GridY(y), GridX(nx), GridY(ny))) continue;
                    reachable[nx, ny] = true; queue.Enqueue(nx * height + ny);
                }
            }
            int count = 0;
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
                if (walkable[x, y]) { count++; Require(reachable[x, y], "isolated walkable sample " + GridX(x) + "," + GridY(y)); }
            Require(count > 2500, "unexpectedly tiny exploration space");
            ReachablePoint(ThornwoodLayout.Entry, reachable); ReachablePoint(ThornwoodLayout.Cache, reachable);
            for (int id = 0; id < 3; id++)
            { ReachablePoint(ThornwoodLayout.SeedPosition(id), reachable); ReachablePoint(ThornwoodLayout.EnemySpawn(id), reachable); }
            Console.WriteLine("PATH_EVIDENCE " + count + " reachable grid nodes with radius-safe continuous edges; not native physics");
        });
        Check("existing Guardian health damage timing and legacy clearing remain unchanged", () =>
        {
            var legacy = new ClearingRun(); var bossRun = new ClearingRun(true);
            Require(!legacy.RequiresBoss && bossRun.RequiresBoss && bossRun.Boss.Health == 108 &&
                ClearingBossState.MaxHealth == 108 && ClearingBossState.Damage == 2 &&
                bossRun.Boss.WindupDuration == 1 && bossRun.Boss.ActiveDuration == .16f &&
                bossRun.Boss.RecoveryDuration == 1.15f && ClearingRun.SentinelMaxHealth == 54 &&
                ClearingRun.LightDamage == 18 && ClearingRun.BurstDamage == 30 &&
                ClearingRun.CoinsPerSentinel == 10 && ClearingProgressData.CompletionCoins == 30, "mature combat/reward changed");
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
    private static float GridX(int id) { return 14.4f + id * .2f; }
    private static float GridY(int id) { return -4.6f + id * .2f; }
    private static bool CoveredByBoundary(float x, float y)
    {
        for (int id = 0; id < ThornwoodLayout.BoundaryCount; id++)
        {
            ThornwoodRect wall = ThornwoodLayout.GetBoundary(id);
            if (Math.Abs(x - wall.X) <= wall.Width * .5f + .00001f &&
                Math.Abs(y - wall.Y) <= wall.Height * .5f + .00001f) return true;
        }
        return false;
    }
    private static void ReachablePoint(ThornwoodPoint point, bool[,] reachable)
    {
        Require(ThornwoodLayout.CanOccupy(point.X, point.Y), "objective/spawn foot circle obstructed");
        for (int x = 0; x < reachable.GetLength(0); x++) for (int y = 0; y < reachable.GetLength(1); y++)
        {
            if (!reachable[x, y] || Math.Abs(GridX(x) - point.X) > .3f || Math.Abs(GridY(y) - point.Y) > .3f) continue;
            if (ThornwoodLayout.CanTravel(GridX(x), GridY(y), point.X, point.Y)) return;
        }
        throw new Exception("objective/spawn has no radius-safe continuous connection: " + point.X + "," + point.Y);
    }
}
