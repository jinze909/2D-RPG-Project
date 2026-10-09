using System;
using Rpg.Gameplay;

internal static class ClearingRulesChecks
{
    private static int passed;
    private static int failed;

    private static void Check(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void Equal(float expected, float actual, string message)
    {
        Require(!float.IsNaN(actual) && Math.Abs(expected - actual) < 0.0001f,
            message + " expected=" + expected + " actual=" + actual);
    }

    private static long Attack(ClearingRun run, PlayerAttackKind kind)
    {
        long token;
        Require(run.BeginPlayerAttack(kind, 20f, out token), "attack rejected");
        Require(token > 0, "no valid token");
        return token;
    }

    private static void Defeat(ClearingRun run, int sentinel)
    {
        run.Advance(ClearingRun.BurstCooldown);
        long first = Attack(run, PlayerAttackKind.Burst);
        Require(run.TryHitSentinel(first, sentinel), "first hit rejected");
        run.Advance(ClearingRun.BurstCooldown);
        long second = Attack(run, PlayerAttackKind.Burst);
        Require(run.TryHitSentinel(second, sentinel), "second hit rejected");
    }

    private static void DefeatAll(ClearingRun run)
    {
        for (int i = 0; i < ClearingRun.SentinelCount; i++) Defeat(run, i);
    }

    private static int Main()
    {
        Check("new run has three living sentinels and a locked objective", delegate {
            var run = new ClearingRun();
            Require(ClearingRun.SentinelCount == 3 && !run.GateUnlocked && !run.IsDead && !run.IsComplete,
                "incorrect initial run");
            Require(run.DefeatedCount == 0 && run.RewardCoins == 0 && run.Time == 0d, "stale progression");
            for (int i = 0; i < 3; i++)
            {
                Equal(54f, run.GetSentinel(i).Health, "sentinel health");
                Require(run.GetSentinel(i).AttackPhase == SentinelAttackPhase.Ready, "not ready");
            }
        });
        Check("invalid sentinel contacts cannot mutate the run", delegate {
            var run = new ClearingRun();
            long action = Attack(run, PlayerAttackKind.Light);
            foreach (int id in new[] { -1, 3, int.MinValue, int.MaxValue })
                Require(!run.TryHitSentinel(action, id) && !run.BeginSentinelAttack(id) && !run.TryResolveSentinelHit(id),
                    "invalid index accepted");
            Require(run.DefeatedCount == 0 && run.RewardCoins == 0, "progress mutated");
        });
        Check("invalid sentinel getters report a clear argument error", delegate {
            var run = new ClearingRun();
            bool threw = false;
            try { run.GetSentinel(3); } catch (ArgumentOutOfRangeException) { threw = true; }
            Require(threw, "invalid getter did not reject");
        });
        Check("invalid mana and attack kinds never start actions or cooldowns", delegate {
            var run = new ClearingRun();
            long token;
            foreach (float mana in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Require(!run.CanPlayerAttack(PlayerAttackKind.Burst, mana), "invalid mana readiness");
                Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, mana, out token) && token == 0,
                    "invalid mana accepted");
            }
            Require(!run.BeginPlayerAttack((PlayerAttackKind)99, 20f, out token), "unknown attack accepted");
            Equal(0f, run.BurstCooldownRemaining, "rejected action consumed cooldown");
            Require(!run.PlayerAttackActive, "rejected action became active");
        });
        Check("insufficient mana is rejected and an exact six mana balance is accepted", delegate {
            var run = new ClearingRun();
            long token;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, 5.99f, out token), "insufficient accepted");
            Require(run.BeginPlayerAttack(PlayerAttackKind.Burst, 6f, out token), "exact mana rejected");
            Equal(1.2f, run.BurstCooldownRemaining, "burst cooldown");
        });
        Check("light attack does not require mana", delegate {
            var run = new ClearingRun();
            long token;
            Require(run.BeginPlayerAttack(PlayerAttackKind.Light, 0f, out token), "zero mana light rejected");
        });
        Check("light cooldown rejects held input until 0.45 seconds", delegate {
            var run = new ClearingRun();
            Attack(run, PlayerAttackKind.Light);
            run.Advance(0.449f);
            long ignored;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Light, 20f, out ignored), "light too early");
            run.Advance(0.0011f);
            Attack(run, PlayerAttackKind.Light);
        });
        Check("burst cooldown rejects held input until 1.2 seconds", delegate {
            var run = new ClearingRun();
            Attack(run, PlayerAttackKind.Burst);
            run.Advance(1.19f);
            long ignored;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out ignored), "burst too early");
            run.Advance(0.0101f);
            Attack(run, PlayerAttackKind.Burst);
        });
        Check("one contact window cannot overlap another player action", delegate {
            var run = new ClearingRun();
            Attack(run, PlayerAttackKind.Light);
            long ignored;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out ignored), "overlap accepted");
            run.Advance(ClearingRun.LightAttackWindow);
            Attack(run, PlayerAttackKind.Burst);
        });
        Check("each player attack damages each sentinel at most once", delegate {
            var run = new ClearingRun();
            long token = Attack(run, PlayerAttackKind.Light);
            Require(run.TryHitSentinel(token, 0), "initial contact rejected");
            for (int i = 0; i < 20; i++) Require(!run.TryHitSentinel(token, 0), "repeat contact accepted");
            Equal(36f, run.GetSentinel(0).Health, "duplicate damage");
        });
        Check("a burst may hit all three distinct enemies once", delegate {
            var run = new ClearingRun();
            long token = Attack(run, PlayerAttackKind.Burst);
            for (int i = 0; i < 3; i++)
            {
                Require(run.TryHitSentinel(token, i), "distinct contact rejected");
                Equal(24f, run.GetSentinel(i).Health, "burst damage");
            }
        });
        Check("expired attack contacts cannot damage enemies", delegate {
            var run = new ClearingRun();
            long token = Attack(run, PlayerAttackKind.Light);
            run.Advance(ClearingRun.LightAttackWindow);
            Require(!run.TryHitSentinel(token, 0), "expired contact accepted");
            Equal(54f, run.GetSentinel(0).Health, "expired damage");
        });
        Check("wrong and superseded player tokens cannot deal damage", delegate {
            var run = new ClearingRun();
            long first = Attack(run, PlayerAttackKind.Light);
            Require(!run.TryHitSentinel(0, 0) && !run.TryHitSentinel(first + 1, 0), "fabricated token accepted");
            run.Advance(ClearingRun.LightCooldown);
            long second = Attack(run, PlayerAttackKind.Light);
            Require(second != first && !run.TryHitSentinel(first, 0), "superseded token accepted");
            Require(run.TryHitSentinel(second, 0), "new token rejected");
        });
        Check("overkill clamps sentinel health at zero and rewards once", delegate {
            var run = new ClearingRun();
            Defeat(run, 0);
            Equal(0f, run.GetSentinel(0).Health, "overkill health");
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Dead, "not dead");
            Require(run.DefeatedCount == 1 && run.RewardCoins == 10, "reward mismatch");
            run.Advance(ClearingRun.LightCooldown);
            long token = Attack(run, PlayerAttackKind.Light);
            for (int i = 0; i < 20; i++) Require(!run.TryHitSentinel(token, 0), "corpse hit accepted");
            Require(run.DefeatedCount == 1 && run.RewardCoins == 10, "duplicate reward");
        });
        Check("gate unlock requires three unique defeats", delegate {
            var run = new ClearingRun();
            Defeat(run, 0);
            Require(!run.GateUnlocked, "unlocked after one");
            Defeat(run, 1);
            Require(!run.GateUnlocked && run.DefeatedCount == 2, "unlocked after two");
            Defeat(run, 2);
            Require(run.GateUnlocked && run.DefeatedCount == 3 && run.RewardCoins == 30, "not unlocked after three");
        });
        Check("objective cannot complete before the gate is unlocked", delegate {
            var run = new ClearingRun();
            Require(!run.TryCompleteObjective() && !run.IsComplete, "locked objective accepted");
            Defeat(run, 0);
            Require(!run.TryCompleteObjective(), "partially cleared objective accepted");
        });
        Check("objective completion is idempotent and stops combat", delegate {
            var run = new ClearingRun();
            DefeatAll(run);
            Require(run.TryCompleteObjective() && run.IsComplete, "unlocked objective rejected");
            double time = run.Time;
            Require(!run.TryCompleteObjective(), "completion duplicated");
            long token;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Light, 20f, out token), "victory combat accepted");
            run.Advance(1f);
            Require(run.Time == time && run.RewardCoins == 30, "completed run mutated");
        });
        Check("enemy damage waits for the full telegraph", delegate {
            var run = new ClearingRun();
            Require(run.BeginSentinelAttack(0), "enemy start rejected");
            Require(!run.TryResolveSentinelHit(0), "immediate enemy hit");
            run.Advance(0.649f);
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Telegraph, "telegraph cut short");
            Require(!run.TryResolveSentinelHit(0), "early enemy hit");
            run.Advance(0.0011f);
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Active, "no active phase");
            Require(run.TryResolveSentinelHit(0), "active contact rejected");
        });
        Check("enemy active phase allows at most one contact", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0);
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(0), "initial hit rejected");
            Require(!run.TryResolveSentinelHit(0), "repeated active hit");
            Equal(ClearingRun.PlayerInvulnerabilityDuration, run.PlayerInvulnerabilityRemaining, "no immunity");
        });
        Check("enemy cannot begin again during telegraph or active or recovery", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0);
            long first = run.GetSentinel(0).AttackSequence;
            Require(!run.BeginSentinelAttack(0), "telegraph reentry");
            run.Advance(ClearingRun.TelegraphDuration);
            Require(!run.BeginSentinelAttack(0), "active reentry");
            run.Advance(ClearingRun.SentinelActiveDuration);
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Recovery, "missing recovery");
            Require(!run.BeginSentinelAttack(0), "recovery reentry");
            run.Advance(ClearingRun.SentinelRecoveryDuration);
            Require(run.BeginSentinelAttack(0) && run.GetSentinel(0).AttackSequence != first, "next swing unavailable");
        });
        Check("recovery contacts never deal damage", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0);
            run.Advance(ClearingRun.TelegraphDuration);
            run.Advance(ClearingRun.SentinelActiveDuration);
            Require(!run.TryResolveSentinelHit(0), "recovery damage accepted");
        });
        Check("a long frame skips expired contact instead of delivering delayed damage", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0);
            run.Advance(5f);
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Ready, "stuck after hitch");
            Require(!run.TryResolveSentinelHit(0), "delayed damage after hitch");
        });
        Check("shared immunity prevents simultaneous sentinel pileup damage", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0); run.BeginSentinelAttack(1);
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(0), "first contact rejected");
            Require(!run.TryResolveSentinelHit(1), "simultaneous contact bypassed immunity");
            run.Advance(0.01f);
            Require(!run.TryResolveSentinelHit(1), "blocked swing retried");
        });
        Check("immunity expires and a later fresh swing can hit", delegate {
            var run = new ClearingRun();
            run.BeginSentinelAttack(0);
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(0), "initial contact rejected");
            run.Advance(ClearingRun.PlayerInvulnerabilityDuration);
            Equal(0f, run.PlayerInvulnerabilityRemaining, "immunity did not expire");
            run.BeginSentinelAttack(1);
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(1), "fresh contact rejected");
        });
        Check("killing a winding-up sentinel cancels its hit", delegate {
            var run = new ClearingRun();
            long first = Attack(run, PlayerAttackKind.Burst);
            run.TryHitSentinel(first, 0);
            run.Advance(ClearingRun.BurstCooldown);
            run.BeginSentinelAttack(0);
            long second = Attack(run, PlayerAttackKind.Burst);
            Require(run.TryHitSentinel(second, 0), "finishing hit rejected");
            run.Advance(ClearingRun.TelegraphDuration);
            Require(!run.TryResolveSentinelHit(0) && !run.BeginSentinelAttack(0), "dead sentinel still attacks");
        });
        Check("death cancels contacts and freezes all gameplay until retry", delegate {
            var run = new ClearingRun();
            long token = Attack(run, PlayerAttackKind.Light);
            run.BeginSentinelAttack(0);
            run.NotifyPlayerDeath();
            double time = run.Time;
            Require(run.IsDead && !run.TryHitSentinel(token, 0), "dead player contact accepted");
            Require(!run.BeginSentinelAttack(1), "enemy starts during death");
            run.Advance(5f);
            Require(run.Time == time && !run.TryResolveSentinelHit(0), "dead run progressed");
            long ignored;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out ignored), "dead player attack");
        });
        Check("a dead player cannot claim an unlocked objective", delegate {
            var run = new ClearingRun();
            DefeatAll(run);
            run.NotifyPlayerDeath();
            Require(!run.TryCompleteObjective() && !run.IsComplete, "death claimed victory");
        });
        Check("retry resets progress enemies time cooldowns immunity and contact state", delegate {
            var run = new ClearingRun();
            Defeat(run, 0);
            run.BeginSentinelAttack(1);
            run.Advance(ClearingRun.TelegraphDuration);
            run.TryResolveSentinelHit(1);
            run.NotifyPlayerDeath();
            run.ResetRun();
            Require(!run.IsDead && !run.IsComplete && !run.GateUnlocked && run.Time == 0d, "retry state stale");
            Require(run.DefeatedCount == 0 && run.RewardCoins == 0 && !run.PlayerAttackActive, "retry progression stale");
            Equal(0f, run.LightCooldownRemaining, "retry light cooldown");
            Equal(0f, run.BurstCooldownRemaining, "retry burst cooldown");
            Equal(0f, run.PlayerInvulnerabilityRemaining, "retry immunity");
            for (int i = 0; i < 3; i++)
            {
                Equal(54f, run.GetSentinel(i).Health, "retry health");
                Require(run.GetSentinel(i).AttackPhase == SentinelAttackPhase.Ready, "retry attack stale");
            }
            long token = Attack(run, PlayerAttackKind.Burst);
            Require(run.TryHitSentinel(token, 0), "retry attack contact stale");
        });
        Check("retry invalidates earlier attack tokens even at identical run time", delegate {
            var run = new ClearingRun();
            long oldToken = Attack(run, PlayerAttackKind.Light);
            run.ResetRun();
            long newToken = Attack(run, PlayerAttackKind.Light);
            Require(oldToken != newToken && !run.TryHitSentinel(oldToken, 0), "retry reused stale token");
            Require(run.TryHitSentinel(newToken, 0), "new retry token rejected");
        });
        Check("invalid simulation deltas do not advance timers or enemy phases", delegate {
            var run = new ClearingRun();
            Attack(run, PlayerAttackKind.Burst);
            run.BeginSentinelAttack(0);
            foreach (float delta in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                run.Advance(delta);
            Require(run.Time == 0d && run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Telegraph, "invalid clock accepted");
            Equal(1.2f, run.BurstCooldownRemaining, "invalid delta changed cooldown");
        });
        Check("cancel transient actions invalidates player contacts and every live enemy phase", delegate {
            var run = new ClearingRun();
            Require(run.BeginSentinelAttack(0), "first enemy setup rejected");
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.BeginSentinelAttack(1), "second enemy setup rejected");
            run.Advance(ClearingRun.SentinelActiveDuration);
            Require(run.BeginSentinelAttack(2), "third enemy setup rejected");
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Recovery
                    && run.GetSentinel(1).AttackPhase == SentinelAttackPhase.Telegraph, "mixed phase setup failed");
            long token = Attack(run, PlayerAttackKind.Burst);
            run.CancelTransientActions();
            Require(!run.PlayerAttackActive, "interrupted player contact remains active");
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
            {
                Require(!run.TryHitSentinel(token, i) && !run.TryResolveSentinelHit(i), "interrupted contact accepted");
                Require(run.GetSentinel(i).AttackPhase == SentinelAttackPhase.Ready, "live enemy phase was retained");
                Equal(0f, run.GetSentinel(i).PhaseProgress, "canceled phase has stale progress");
            }
            run.Advance(ClearingRun.TelegraphDuration);
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
                Require(run.GetSentinel(i).AttackPhase == SentinelAttackPhase.Ready
                        && !run.TryResolveSentinelHit(i), "old enemy swing returned as the clock advanced");
        });
        Check("cancel transient actions preserves progression health clock cooldowns and immunity", delegate {
            var run = new ClearingRun(); Defeat(run, 0);
            run.Advance(ClearingRun.BurstCooldown);
            Require(run.BeginSentinelAttack(1), "immunity setup rejected");
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(1), "immunity setup had no active contact");
            long light = Attack(run, PlayerAttackKind.Light);
            Require(run.TryHitSentinel(light, 2), "light setup contact rejected");
            run.Advance(ClearingRun.LightAttackWindow);
            long burst = Attack(run, PlayerAttackKind.Burst);
            Require(run.TryHitSentinel(burst, 2) && run.BeginSentinelAttack(2), "burst or warning setup rejected");
            double time = run.Time;
            float lightCooldown = run.LightCooldownRemaining, burstCooldown = run.BurstCooldownRemaining;
            float immunity = run.PlayerInvulnerabilityRemaining;
            float[] health = { run.GetSentinel(0).Health, run.GetSentinel(1).Health, run.GetSentinel(2).Health };
            Require(lightCooldown > 0f && burstCooldown > 0f && immunity > 0f, "setup has no active timers");
            run.CancelTransientActions();
            Require(run.Time == time && run.DefeatedCount == 1 && run.RewardCoins == ClearingRun.CoinsPerSentinel
                    && !run.GateUnlocked && !run.IsDead && !run.IsComplete, "cancel reset run progression or status");
            Equal(lightCooldown, run.LightCooldownRemaining, "cancel refunded light cooldown");
            Equal(burstCooldown, run.BurstCooldownRemaining, "cancel refunded burst cooldown");
            Equal(immunity, run.PlayerInvulnerabilityRemaining, "cancel removed immunity");
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
                Equal(health[i], run.GetSentinel(i).Health, "cancel reset sentinel health");
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Dead, "cancel revived defeated sentinel");
        });
        Check("canceled tokens stay rejected after fresh actions and fresh enemy swings remain available", delegate {
            var run = new ClearingRun();
            long canceled = Attack(run, PlayerAttackKind.Burst);
            Require(run.BeginSentinelAttack(0), "old swing setup rejected");
            long oldSequence = run.GetSentinel(0).AttackSequence;
            run.CancelTransientActions();
            long blocked;
            Require(!run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out blocked), "cancel refunded burst admission");
            run.Advance(ClearingRun.BurstCooldown);
            long fresh = Attack(run, PlayerAttackKind.Burst);
            Require(fresh != canceled && !run.TryHitSentinel(canceled, 0), "fresh action reused interrupted token");
            Require(run.TryHitSentinel(fresh, 0), "fresh contact rejected after cancellation");
            Require(run.BeginSentinelAttack(0) && run.GetSentinel(0).AttackSequence != oldSequence,
                    "fresh enemy telegraph unavailable or reused old sequence");
            Require(!run.TryResolveSentinelHit(0), "fresh swing skipped its telegraph");
            run.Advance(ClearingRun.TelegraphDuration);
            Require(run.TryResolveSentinelHit(0), "fresh telegraphed swing could not contact");
        });
        Check("cancel transient actions is idempotent and preserves death and objective completion", delegate {
            foreach (bool complete in new[] { false, true })
            {
                var run = new ClearingRun();
                if (complete) { DefeatAll(run); Require(run.TryCompleteObjective(), "completion setup rejected"); }
                else { Attack(run, PlayerAttackKind.Light); run.NotifyPlayerDeath(); }
                double time = run.Time;
                int rewards = run.RewardCoins, defeats = run.DefeatedCount;
                run.CancelTransientActions(); run.CancelTransientActions();
                Require(complete ? run.IsComplete && !run.IsDead : run.IsDead && !run.IsComplete,
                        "cancel changed terminal state");
                Require(run.Time == time && run.RewardCoins == rewards && run.DefeatedCount == defeats,
                        "repeated cancel changed terminal progression");
                long token;
                Require(!run.BeginPlayerAttack(PlayerAttackKind.Light, 20f, out token)
                        && !run.BeginSentinelAttack(0) && !run.TryResolveSentinelHit(0), "cancel reopened terminal combat");
            }
        });
        Check("full clear complete retry complete loop is replayable", delegate {
            var run = new ClearingRun();
            for (int round = 0; round < 3; round++)
            {
                DefeatAll(run);
                Require(run.GateUnlocked && run.TryCompleteObjective(), "loop did not complete");
                Require(run.RewardCoins == 30, "reward differs between attempts");
                run.ResetRun();
            }
        });
        Check("deterministic mixed inputs preserve health progression and timer invariants", delegate {
            var run = new ClearingRun();
            var random = new Random(719);
            for (int step = 0; step < 5000; step++)
            {
                int id = random.Next(3);
                if (step % 367 == 0) run.ResetRun();
                if (random.Next(25) == 0) run.NotifyPlayerDeath();
                run.Advance((float)(random.NextDouble() * 0.08));
                run.BeginSentinelAttack(id);
                run.TryResolveSentinelHit(id);
                long token;
                if (run.BeginPlayerAttack((PlayerAttackKind)random.Next(2), 20f, out token))
                    for (int i = 0; i < 3; i++) if (random.Next(2) == 0) run.TryHitSentinel(token, i);
                run.TryCompleteObjective();
                int dead = 0;
                for (int i = 0; i < 3; i++)
                {
                    SentinelState sentinel = run.GetSentinel(i);
                    Require(sentinel.Health >= 0 && sentinel.Health <= 54 && !float.IsNaN(sentinel.Health), "health out of bounds");
                    Require(sentinel.PhaseProgress >= 0 && sentinel.PhaseProgress <= 1, "phase progress out of bounds");
                    if (sentinel.IsDead) dead++;
                }
                Require(run.DefeatedCount == dead && run.RewardCoins == dead * 10, "reward/progress diverged");
                Require(run.GateUnlocked == (dead == 3), "gate diverged");
                Require(!run.IsComplete || run.GateUnlocked, "victory bypassed gate");
                Require(run.LightCooldownRemaining >= 0 && run.BurstCooldownRemaining >= 0, "negative cooldown");
            }
        });
        Console.WriteLine("Clearing rules: " + passed + " passed, " + failed + " failed; pure production C#, no Unity stubs.");
        return failed == 0 ? 0 : 1;
    }
}
