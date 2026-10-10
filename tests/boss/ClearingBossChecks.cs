using System;
using Rpg.Gameplay;

internal static class ClearingBossChecks
{
    private static int passed, failed;
    private static void Check(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Equal(float expected, float actual, string message)
    {
        Require(!float.IsNaN(actual) && Math.Abs(expected - actual) < 0.0001f,
            message + " expected=" + expected + " actual=" + actual);
    }
    private static long PlayerAttack(ClearingRun run, PlayerAttackKind kind)
    {
        long token;
        Require(run.BeginPlayerAttack(kind, 20f, out token), "player attack rejected");
        return token;
    }
    private static void DefeatGuards(ClearingRun run)
    {
        for (int round = 0; round < 2; round++)
        {
            run.Advance(ClearingRun.BurstCooldown + 0.001f);
            long token = PlayerAttack(run, PlayerAttackKind.Burst);
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
                Require(run.TryHitSentinel(token, i), "guard hit rejected");
        }
    }
    private static ClearingRun AwakeRun()
    {
        var run = new ClearingRun(true);
        DefeatGuards(run);
        Require(run.TryAwakenBoss(), "awakening rejected");
        return run;
    }
    private static long HitBoss(ClearingRun run, PlayerAttackKind kind)
    {
        run.Advance((kind == PlayerAttackKind.Light ? ClearingRun.LightCooldown : ClearingRun.BurstCooldown) + 0.001f);
        long token = PlayerAttack(run, kind);
        Require(run.TryHitBoss(token), "boss contact rejected");
        return token;
    }
    private static bool BossAttack(ClearingRun run)
    {
        return run.TryBeginBossAttack(0f, 0f, 2f, 0f, true);
    }
    private static void FinishBossAttack(ClearingRun run)
    {
        run.Advance(run.Boss.WindupDuration + run.Boss.ActiveDuration + run.Boss.RecoveryDuration + 0.001f);
    }
    private static int Main()
    {
        Check("optional boss constructor preserves the existing three-guard completion contract", delegate {
            var run = new ClearingRun(); DefeatGuards(run);
            Require(!run.RequiresBoss && run.GateUnlocked && run.TryCompleteObjective(), "legacy completion changed");
            Require(!run.TryAwakenBoss() && !BossAttack(run), "optional boss silently appeared");
            Equal(108f, run.Boss.Health, "dormant health changed");
        });
        Check("boss required runs start dormant and cannot bypass guards or the guardian", delegate {
            var run = new ClearingRun(true);
            Require(run.RequiresBoss && !run.Boss.IsAwake && !run.Boss.IsDead && !run.Boss.Enraged, "initial boss state");
            Require(!run.TryAwakenBoss() && !run.GateUnlocked && !BossAttack(run), "guard prerequisite bypassed");
            long token = PlayerAttack(run, PlayerAttackKind.Light);
            Require(!run.TryHitBoss(token), "dormant boss damage");
            DefeatGuards(run);
            Require(run.DefeatedCount == 3 && run.RewardCoins == 30 && !run.GateUnlocked
                && !run.TryCompleteObjective(), "guard clear bypassed final encounter");
        });
        Check("awakening is explicit idempotent and cancels old contacts without refunding cost deadlines", delegate {
            var run = new ClearingRun(true); DefeatGuards(run);
            float cooldown = run.BurstCooldownRemaining;
            Require(run.PlayerAttackActive && run.TryAwakenBoss(), "active awakening setup");
            Require(run.Boss.IsAwake && !run.PlayerAttackActive && !run.TryAwakenBoss(), "awakening duplicated");
            Equal(cooldown, run.BurstCooldownRemaining, "awakening refunded cooldown");
            Require(run.RewardCoins == 30 && !run.GateUnlocked, "awakening changed rewards/gate");
        });
        Check("boss attack admission rejects blocked invalid and out of range positions without advancing mode", delegate {
            var run = AwakeRun();
            Require(!run.TryBeginBossAttack(0f, 0f, 4.001f, 0f, true)
                && !run.TryBeginBossAttack(0f, 0f, 2f, 0f, false), "range or wall ignored");
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Require(!run.TryBeginBossAttack(invalid, 0f, 1f, 0f, true)
                    && !run.TryBeginBossAttack(0f, invalid, 1f, 0f, true)
                    && !run.TryBeginBossAttack(0f, 0f, invalid, 0f, true)
                    && !run.TryBeginBossAttack(0f, 0f, 0f, invalid, true), "nonfinite admission");
            Require(!run.TryBeginBossAttack(-float.MaxValue, 0f, float.MaxValue, 0f, true), "overflow range admission");
            Require(run.Boss.AttackSequence == 0 && run.Boss.NextAttackKind == SentinelAttackKind.Lance, "rejection advanced mode");
            Require(run.TryBeginBossAttack(0f, 0f, 4f, 0f, true), "exact lance range rejected");
        });
        Check("lance locks its forward lane and original caster position including rotated boundaries", delegate {
            var run = AwakeRun(); Require(run.TryBeginBossAttack(1f, 2f, 4f, 2f, true), "lance rejected");
            var footprint = run.Boss.AttackFootprint;
            Equal(1f, footprint.OriginX, "caster origin"); Equal(2f, footprint.OriginY, "caster origin Y");
            Equal(3.1f, footprint.CenterX, "lane center"); Equal(4.2f, footprint.Width, "lane length");
            Equal(0.9f, footprint.Height, "lane width");
            Require(footprint.Contains(5.2f, 2f) && !footprint.Contains(0.98f, 2f)
                && !footprint.Contains(3f, 2.46f), "lane contacts inconsistent");
            var rotated = BossTactics.CreateFootprint(SentinelAttackKind.Lance, 0f, 0f, 2f, 2f);
            Require(rotated.Contains(2f, 2f) && !rotated.Contains(-1f, -1f), "rotated lane incorrect");
            Require(!BossAttack(run) && Object.ReferenceEquals(footprint, run.Boss.AttackFootprint), "active warning retargeted");
        });
        Check("lance and sigil alternate with distinct ranges and a stationary target square", delegate {
            var run = AwakeRun(); Require(BossAttack(run), "first lance rejected"); FinishBossAttack(run);
            Require(run.Boss.AttackFootprint == null && run.Boss.NextAttackKind == SentinelAttackKind.Sigil, "lance did not recover");
            Require(!run.TryBeginBossAttack(0f, 0f, 6.001f, 0f, true)
                && run.TryBeginBossAttack(0f, 0f, 6f, 0f, true), "sigil range");
            var footprint = run.Boss.AttackFootprint;
            Equal(6f, footprint.CenterX, "sigil target"); Equal(1.8f, footprint.Width, "sigil width");
            Equal(1.8f, footprint.Height, "sigil height");
            Require(footprint.Contains(6.9f, 0.9f) && !footprint.Contains(6.91f, 0f), "sigil edge");
            Require(run.Boss.AttackKind == SentinelAttackKind.Sigil
                && run.Boss.NextAttackKind == SentinelAttackKind.Lance, "mode order");
            FinishBossAttack(run); Require(BossAttack(run), "second lance rejected");
            Require(run.Boss.AttackKind == SentinelAttackKind.Lance, "did not return to lance");
        });
        Check("invalid boss footprint kinds and coordinates cannot produce contact geometry", delegate {
            Require(BossTactics.CreateFootprint(SentinelAttackKind.Sweep, 0f, 0f, 0f, 0f) == null
                && BossTactics.CreateFootprint(SentinelAttackKind.Lance, float.NaN, 0f, 0f, 0f) == null, "invalid geometry accepted");
            bool threw = false;
            try { BossTactics.RangeForKind(SentinelAttackKind.Sweep); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Require(threw, "invalid range kind accepted");
            var zeroAim = BossTactics.CreateFootprint(SentinelAttackKind.Lance, 0f, 0f, 0f, 0f);
            Require(zeroAim.Contains(0f, -2f) && !zeroAim.Contains(0f, 1f), "zero aim has no stable fallback");
        });
        Check("normal timing requires complete windup then active contact once and recovery", delegate {
            var run = AwakeRun(); Require(BossAttack(run), "attack rejected");
            Equal(1f, run.Boss.WindupDuration, "normal warning"); Equal(0.16f, run.Boss.ActiveDuration, "normal active");
            Equal(1.15f, run.Boss.RecoveryDuration, "normal recovery");
            Require(!run.TryResolveBossHit() && !BossAttack(run), "immediate contact or reentry");
            run.Advance(0.999f);
            Require(run.Boss.AttackPhase == SentinelAttackPhase.Telegraph && !run.TryResolveBossHit(), "early contact");
            run.Advance(0.0011f);
            Require(run.Boss.AttackPhase == SentinelAttackPhase.Active && run.TryResolveBossHit(), "active contact rejected");
            Require(!run.TryResolveBossHit(), "duplicate contact");
            Equal(ClearingRun.PlayerInvulnerabilityDuration, run.PlayerInvulnerabilityRemaining, "missing shared immunity");
            run.Advance(0.16f);
            Require(run.Boss.AttackPhase == SentinelAttackPhase.Recovery && !run.TryResolveBossHit()
                && !BossAttack(run), "recovery contact or reentry");
            run.Advance(1.151f); Require(BossAttack(run), "fresh attack unavailable");
        });
        Check("half health accelerates only future attacks and never cuts the current telegraph", delegate {
            var run = AwakeRun(); HitBoss(run, PlayerAttackKind.Light); HitBoss(run, PlayerAttackKind.Light);
            run.Advance(ClearingRun.LightCooldown + 0.001f);
            Require(BossAttack(run), "calm attack rejected");
            Require(run.TryHitBoss(PlayerAttack(run, PlayerAttackKind.Light)), "threshold contact rejected");
            Require(run.Boss.Enraged, "half health did not enrage"); Equal(54f, run.Boss.Health, "threshold HP");
            Equal(1f, run.Boss.WindupDuration, "current warning shortened");
            run.Advance(0.81f);
            Require(run.Boss.AttackPhase == SentinelAttackPhase.Telegraph && !run.TryResolveBossHit(), "phase change delivered early hit");
            run.Advance(1.501f); Require(BossAttack(run), "enraged attack rejected");
            Equal(0.8f, run.Boss.WindupDuration, "enraged warning"); Equal(0.9f, run.Boss.RecoveryDuration, "enraged recovery");
            run.Advance(0.8001f); Require(run.Boss.AttackPhase == SentinelAttackPhase.Active, "enraged timing not used");
        });
        Check("player light and burst damage boss once per current action with real overkill clamping", delegate {
            var run = AwakeRun(); long first = HitBoss(run, PlayerAttackKind.Light);
            Equal(90f, run.Boss.Health, "light damage");
            Require(!run.TryHitBoss(first) && !run.TryHitBoss(0) && !run.TryHitBoss(first + 1), "duplicate or false token damage");
            long burst = HitBoss(run, PlayerAttackKind.Burst); Equal(60f, run.Boss.Health, "burst damage");
            Require(!run.TryHitBoss(first) && !run.TryHitBoss(burst), "old contact reused");
            HitBoss(run, PlayerAttackKind.Burst); HitBoss(run, PlayerAttackKind.Light); HitBoss(run, PlayerAttackKind.Burst);
            Equal(0f, run.Boss.Health, "overkill did not clamp");
            Require(run.Boss.IsDead && !run.Boss.Enraged && run.Boss.AttackPhase == SentinelAttackPhase.Dead
                && run.Boss.AttackFootprint == null && run.RewardCoins == 30, "death/reward state");
        });
        Check("expired and awakening-preceding player tokens cannot hit the guardian", delegate {
            var run = new ClearingRun(true); DefeatGuards(run);
            run.Advance(ClearingRun.BurstCooldown + 0.001f);
            long beforeAwaken = PlayerAttack(run, PlayerAttackKind.Burst);
            Require(run.TryAwakenBoss(), "awakening setup");
            Require(!run.TryHitBoss(beforeAwaken), "pre-awakening contact carried into encounter");
            Equal(108f, run.Boss.Health, "pre-awakening damage");
            long token = HitBoss(run, PlayerAttackKind.Light); run.Advance(ClearingRun.LightAttackWindow + 0.001f);
            Require(!run.TryHitBoss(token), "expired contact accepted");
            Equal(90f, run.Boss.Health, "expired damage");
        });
        Check("boss death cancels even a live contact and unlocks existing once-only beacon reward", delegate {
            var run = AwakeRun();
            for (int i = 0; i < 3; i++) HitBoss(run, PlayerAttackKind.Burst);
            run.Advance(ClearingRun.BurstCooldown + 0.001f); Require(BossAttack(run), "last warning setup");
            run.Advance(0.8001f); Require(run.Boss.AttackPhase == SentinelAttackPhase.Active, "last active setup");
            Require(run.TryHitBoss(PlayerAttack(run, PlayerAttackKind.Burst)), "finishing contact rejected");
            Require(!run.TryResolveBossHit() && !BossAttack(run) && run.GateUnlocked, "dead boss retaliated or gate stayed shut");
            Require(run.TryCompleteObjective() && !run.TryCompleteObjective() && run.RewardCoins == 30, "completion/reward repeated");
            double time = run.Time; run.Advance(5f); Require(run.Time == time, "completed clock advanced");
        });
        Check("long simulation frames skip expired contact without delayed damage", delegate {
            foreach (float delta in new[] { 1.2f, 5f })
            {
                var run = AwakeRun(); Require(BossAttack(run), "hitch setup"); run.Advance(delta);
                Require(!run.TryResolveBossHit(), "expired contact delivered after hitch");
                Require(run.Boss.AttackPhase == (delta < 2f ? SentinelAttackPhase.Recovery : SentinelAttackPhase.Ready), "hitch phase");
            }
        });
        Check("simulation invalid deltas and a paused clock preserve warning progress", delegate {
            var run = AwakeRun(); Require(BossAttack(run), "warning setup"); run.Advance(0.2f);
            double time = run.Time; float progress = run.Boss.PhaseProgress;
            foreach (float delta in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) run.Advance(delta);
            Require(run.Time == time, "invalid clock advanced"); Equal(progress, run.Boss.PhaseProgress, "warning advanced");
            Require(!run.TryResolveBossHit(), "paused warning hit");
        });
        Check("interrupt preserves boss progress mode and original recovery deadline without invisible resumed contact", delegate {
            var run = AwakeRun(); HitBoss(run, PlayerAttackKind.Light); Require(BossAttack(run), "interrupt setup");
            long sequence = run.Boss.AttackSequence;
            run.Advance(0.2f); float health = run.Boss.Health;
            run.CancelTransientActions(); run.CancelTransientActions();
            Require(run.Boss.IsAwake && run.Boss.AttackPhase == SentinelAttackPhase.Recovery
                && run.Boss.AttackFootprint == null && !BossAttack(run), "interrupt allowed fresh or invisible attack");
            Equal(health, run.Boss.Health, "interrupt erased health");
            Require(run.Boss.NextAttackKind == SentinelAttackKind.Sigil, "interrupt erased mode");
            run.Advance(0.8f);
            Require(run.Boss.AttackPhase == SentinelAttackPhase.Recovery && !run.TryResolveBossHit(), "interrupted attack resumed active");
            run.Advance(1.2f); Require(!BossAttack(run), "interrupt refunded recovery");
            run.Advance(0.111f); Require(BossAttack(run) && run.Boss.AttackSequence != sequence, "original ready deadline not retained");
            Require(run.Boss.AttackKind == SentinelAttackKind.Sigil && !run.TryResolveBossHit(), "resume skipped next warning");
        });
        Check("shared immunity blocks guard then boss and boss then guard in isolated contact states", delegate {
            foreach (bool bossFirst in new[] { false, true })
            {
                var run = new ClearingRun(true);
                // Isolate contact arbitration without bypassing the public encounter
                // gate: the same-assembly fixture awakens only the state for this test.
                run.Boss.Awaken(); Require(BossAttack(run), "boss arbitration setup");
                run.Advance(0.35f); Require(run.BeginSentinelAttack(0), "guard arbitration setup");
                run.Advance(0.6501f);
                Require(run.Boss.AttackPhase == SentinelAttackPhase.Active
                    && run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Active, "overlap setup");
                Require(bossFirst ? run.TryResolveBossHit() : run.TryResolveSentinelHit(0), "first contact rejected");
                Require(bossFirst ? !run.TryResolveSentinelHit(0) : !run.TryResolveBossHit(), "shared immunity bypassed");
                run.Advance(0.01f);
                Require(!run.TryResolveSentinelHit(0) && !run.TryResolveBossHit(), "blocked contact retried");
            }
        });
        Check("death cancels boss contacts freezes run and rejects awakening or combat", delegate {
            var run = AwakeRun(); Require(BossAttack(run), "death setup"); run.Advance(1.0001f);
            run.NotifyPlayerDeath(); double time = run.Time; run.Advance(10f);
            Require(run.IsDead && run.Time == time && !run.TryResolveBossHit() && !BossAttack(run)
                && !run.TryAwakenBoss() && !run.TryCompleteObjective(), "dead state admitted activity");
            Require(run.Boss.AttackFootprint == null, "dead run retained geometry");
        });
        Check("retry resets guardian and objective while rejecting old action identities", delegate {
            var run = AwakeRun(); long oldPlayer = HitBoss(run, PlayerAttackKind.Burst);
            Require(BossAttack(run), "reset setup"); long oldBoss = run.Boss.AttackSequence;
            run.ResetRun();
            Require(!run.Boss.IsAwake && !run.Boss.IsDead && !run.GateUnlocked && run.Boss.AttackFootprint == null
                && run.Boss.AttackSequence == 0 && run.Boss.NextAttackKind == SentinelAttackKind.Lance, "retry boss stale");
            Equal(108f, run.Boss.Health, "retry health");
            Require(run.RequiresBoss && run.DefeatedCount == 0 && run.RewardCoins == 0 && run.Time == 0d, "retry progress stale");
            DefeatGuards(run); Require(run.TryAwakenBoss(), "retry awakening rejected");
            Require(!run.TryHitBoss(oldPlayer), "retry retained player contact");
            long fresh = HitBoss(run, PlayerAttackKind.Light);
            Require(fresh != oldPlayer && !run.TryHitBoss(oldPlayer), "retry reused old token");
            Require(BossAttack(run) && run.Boss.AttackSequence != oldBoss, "retry reused boss identity");
        });
        Check("two complete boss attempts retain thirty-coin schema and replay without stale phases", delegate {
            var run = new ClearingRun(true);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                DefeatGuards(run); Require(run.TryAwakenBoss(), "loop awakening");
                for (int hit = 0; hit < 4; hit++) HitBoss(run, PlayerAttackKind.Burst);
                Require(run.Boss.IsDead && run.GateUnlocked && run.TryCompleteObjective() && run.RewardCoins == 30, "loop completion");
                run.ResetRun();
            }
        });
        Console.WriteLine("Boss encounter: " + passed + " passed, " + failed + " failed; pure production C#, no Unity doubles.");
        return failed == 0 ? 0 : 1;
    }
}
