using System;
using Rpg.Gameplay;

internal static class SentinelTacticsChecks
{
    private static int passed;
    private static int failed;
    private static readonly SentinelAttackKind[] Kinds = {
        SentinelAttackKind.Sweep, SentinelAttackKind.Lance, SentinelAttackKind.Sigil
    };

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
        Require(!float.IsNaN(actual) && !float.IsInfinity(actual) && Math.Abs(expected - actual) < .0001f,
                message + " expected=" + expected + " actual=" + actual);
    }

    private static void Throws(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new Exception("invalid argument did not throw ArgumentOutOfRangeException");
    }

    private static void Decision(SentinelAttackKind kind, float distance, SentinelDecision expected,
                                 bool clear = true, bool canRetreat = true)
    {
        Require(SentinelTactics.ChooseAction(kind, distance, clear, canRetreat) == expected,
                kind + " distance=" + distance + " decision mismatch");
    }

    private static SentinelFootprint Footprint(SentinelAttackKind kind, float originX, float originY,
                                               float targetX, float targetY)
    {
        SentinelFootprint result = SentinelTactics.CreateFootprint(kind, originX, originY, targetX, targetY);
        Require(result != null, "valid footprint rejected");
        return result;
    }

    private static long PlayerAttack(ClearingRun run, PlayerAttackKind kind = PlayerAttackKind.Burst)
    {
        long token;
        Require(run.BeginPlayerAttack(kind, 20f, out token) && token > 0, "player attack rejected");
        return token;
    }

    private static void Defeat(ClearingRun run, int id)
    {
        for (int hit = 0; hit < 2; hit++)
        {
            run.Advance(ClearingRun.BurstCooldown);
            Require(run.TryHitSentinel(PlayerAttack(run), id), "kill contact rejected");
        }
    }

    private static int Main()
    {
        Check("three spawn indices select three distinct fixed sentinel roles", delegate {
            for (int i = 0; i < Kinds.Length; i++)
                Require(SentinelTactics.RoleForIndex(i) == Kinds[i], "spawn role mismatch");
            Throws(delegate { SentinelTactics.RoleForIndex(-1); });
            Throws(delegate { SentinelTactics.RoleForIndex(3); });
            Throws(delegate { SentinelTactics.RoleForIndex(int.MaxValue); });
        });
        Check("warden attacks at melee boundary and approaches only within its sight range", delegate {
            Decision(SentinelAttackKind.Sweep, 0f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Sweep, .85f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Sweep, .851f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Sweep, 3.699f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Sweep, 3.7f, SentinelDecision.Hold);
        });
        Check("Lancer commits at lance range and otherwise approaches within its longer sight range", delegate {
            Decision(SentinelAttackKind.Lance, 0f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Lance, 2.8f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Lance, 2.801f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Lance, 4.999f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Lance, 5f, SentinelDecision.Hold);
        });
        Check("seer retreats when crowded and casts throughout its intended spacing band", delegate {
            Decision(SentinelAttackKind.Sigil, 0f, SentinelDecision.Retreat);
            Decision(SentinelAttackKind.Sigil, 2.199f, SentinelDecision.Retreat);
            Decision(SentinelAttackKind.Sigil, 2.2f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Sigil, 3.4f, SentinelDecision.Attack);
            Decision(SentinelAttackKind.Sigil, 3.401f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Sigil, 5.999f, SentinelDecision.Approach);
            Decision(SentinelAttackKind.Sigil, 6f, SentinelDecision.Hold);
        });
        Check("seer can still attack at close range when retreat movement is blocked", delegate {
            Decision(SentinelAttackKind.Sigil, 0f, SentinelDecision.Attack, true, false);
            Decision(SentinelAttackKind.Sigil, 2.199f, SentinelDecision.Attack, true, false);
            Decision(SentinelAttackKind.Sigil, 2.5f, SentinelDecision.Attack, true, false);
        });
        Check("occluded enemies hold regardless of range or retreat availability", delegate {
            foreach (SentinelAttackKind kind in Kinds)
                foreach (float distance in new[] { 0f, .85f, 2.2f, 2.8f, 3.4f, 4f })
                {
                    Decision(kind, distance, SentinelDecision.Hold, false);
                    Decision(kind, distance, SentinelDecision.Hold, false, false);
                }
        });
        Check("invalid distances and unknown roles fail closed without attacks or movement", delegate {
            foreach (SentinelAttackKind kind in Kinds)
                foreach (float distance in new[] { -.001f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                    Decision(kind, distance, SentinelDecision.Hold);
            Decision((SentinelAttackKind)(-1), 0f, SentinelDecision.Hold);
            Decision((SentinelAttackKind)33, 2f, SentinelDecision.Hold);
        });
        Check("default attack and legacy timing constants retain the original melee contract", delegate {
            var run = new ClearingRun();
            Require(run.BeginSentinelAttack(0), "default attack rejected");
            Require(run.GetSentinel(0).AttackKind == SentinelAttackKind.Sweep, "default role changed");
            Equal(ClearingRun.TelegraphDuration, ClearingRun.WindupDuration(SentinelAttackKind.Sweep), "legacy windup");
            Equal(ClearingRun.SentinelActiveDuration, ClearingRun.ActiveDuration(SentinelAttackKind.Sweep), "legacy active");
            Equal(ClearingRun.SentinelRecoveryDuration, ClearingRun.RecoveryDuration(SentinelAttackKind.Sweep), "legacy recovery");
        });
        Check("three attack profiles expose readable windup and recovery budgets", delegate {
            float[] windups = { .65f, .8f, 1f }, active = { .12f, .16f, .18f }, recovery = { .7f, 1f, 1f };
            for (int i = 0; i < Kinds.Length; i++)
            {
                Equal(windups[i], ClearingRun.WindupDuration(Kinds[i]), "windup");
                Equal(active[i], ClearingRun.ActiveDuration(Kinds[i]), "active");
                Equal(recovery[i], ClearingRun.RecoveryDuration(Kinds[i]), "recovery");
            }
            Throws(delegate { ClearingRun.WindupDuration((SentinelAttackKind)99); });
            Throws(delegate { ClearingRun.ActiveDuration((SentinelAttackKind)99); });
            Throws(delegate { ClearingRun.RecoveryDuration((SentinelAttackKind)99); });
        });
        Check("unknown attack kind is rejected without starting or replacing a sentinel action", delegate {
            var run = new ClearingRun();
            Require(!run.BeginSentinelAttack(0, (SentinelAttackKind)99), "unknown attack admitted");
            Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Ready && run.GetSentinel(0).AttackSequence == 0,
                    "rejected action mutated ready state");
            Require(!run.BeginSentinelAttack(-1, SentinelAttackKind.Lance)
                    && !run.BeginSentinelAttack(3, SentinelAttackKind.Sigil), "invalid sentinel admitted");
        });
        foreach (SentinelAttackKind kind in Kinds)
        {
            SentinelAttackKind selected = kind;
            Check(selected + " requires its full warning and expires after the bounded contact window", delegate {
                var run = new ClearingRun();
                Require(run.BeginSentinelAttack(0, selected), "start rejected");
                SentinelState state = run.GetSentinel(0);
                Require(state.AttackKind == selected && state.AttackPhase == SentinelAttackPhase.Telegraph, "wrong starting profile");
                run.Advance(ClearingRun.WindupDuration(selected) - .001f);
                Require(state.AttackPhase == SentinelAttackPhase.Telegraph && state.PhaseProgress > .99f
                        && !run.TryResolveSentinelHit(0), "early contact");
                run.Advance(.002f);
                Require(state.AttackPhase == SentinelAttackPhase.Active && run.TryResolveSentinelHit(0), "active contact missing");
                Require(!run.TryResolveSentinelHit(0), "one action hit twice");
                run.Advance(ClearingRun.ActiveDuration(selected));
                Require(state.AttackPhase == SentinelAttackPhase.Recovery && !run.TryResolveSentinelHit(0), "late contact accepted");
                run.Advance(ClearingRun.RecoveryDuration(selected));
                Require(state.AttackPhase == SentinelAttackPhase.Ready && run.BeginSentinelAttack(0, selected), "recovery never finished");
            });
            Check(selected + " cannot be replaced by another profile during any committed phase", delegate {
                var run = new ClearingRun();
                Require(run.BeginSentinelAttack(0, selected), "start rejected");
                long token = run.GetSentinel(0).AttackSequence;
                foreach (SentinelAttackPhase phase in new[] { SentinelAttackPhase.Telegraph, SentinelAttackPhase.Active, SentinelAttackPhase.Recovery })
                {
                    Require(run.GetSentinel(0).AttackPhase == phase, "wrong phase setup");
                    foreach (SentinelAttackKind other in Kinds)
                        Require(!run.BeginSentinelAttack(0, other), "busy role change accepted");
                    Require(run.GetSentinel(0).AttackSequence == token && run.GetSentinel(0).AttackKind == selected,
                            "busy rejection changed snapshot");
                    run.Advance(phase == SentinelAttackPhase.Telegraph ? ClearingRun.WindupDuration(selected) + .001f
                               : ClearingRun.ActiveDuration(selected));
                }
            });
            Check(selected + " timing hitch skips contact instead of producing a delayed hit", delegate {
                var run = new ClearingRun();
                run.BeginSentinelAttack(0, selected);
                run.Advance(ClearingRun.WindupDuration(selected) + ClearingRun.ActiveDuration(selected) + .05f);
                Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Recovery
                        && !run.TryResolveSentinelHit(0), "missed active window dealt delayed damage");
                run.Advance(20f);
                Require(run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Ready
                        && !run.TryResolveSentinelHit(0), "large hitch left a stale contact");
            });
            Check(selected + " pause by withholding simulation time preserves its pending warning", delegate {
                var run = new ClearingRun();
                run.BeginSentinelAttack(0, selected);
                run.Advance(.1f);
                float progress = run.GetSentinel(0).PhaseProgress;
                double time = run.Time;
                foreach (float invalid in new[] { 0f, -.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) run.Advance(invalid);
                Require(run.Time == time && run.GetSentinel(0).AttackPhase == SentinelAttackPhase.Telegraph,
                        "invalid or absent tick advanced warning");
                Equal(progress, run.GetSentinel(0).PhaseProgress, "paused warning progress");
                run.Advance(ClearingRun.WindupDuration(selected) - .1f + .001f);
                Require(run.TryResolveSentinelHit(0), "resumed warning lost its contact");
            });
        }
        Check("mixed roles share player immunity and consume blocked attacks only once", delegate {
            var run = new ClearingRun();
            for (int i = 0; i < Kinds.Length; i++) run.BeginSentinelAttack(i, Kinds[i]);
            run.Advance(.651f);
            Require(run.TryResolveSentinelHit(0), "sweep contact rejected");
            run.Advance(.15f);
            Require(!run.TryResolveSentinelHit(1), "lance bypassed shared immunity");
            run.Advance(.2f);
            Require(!run.TryResolveSentinelHit(2), "sigil bypassed shared immunity");
            run.Advance(.3f);
            Require(!run.TryResolveSentinelHit(1) && !run.TryResolveSentinelHit(2), "blocked swing gained a deferred hit");
        });
        Check("disable cancellation removes every role contact while preserving player cooldown and earned coins", delegate {
            var run = new ClearingRun();
            Defeat(run, 0);
            long token = PlayerAttackAfterCooldown(run);
            run.BeginSentinelAttack(1, SentinelAttackKind.Lance);
            run.BeginSentinelAttack(2, SentinelAttackKind.Sigil);
            float cooldown = run.BurstCooldownRemaining;
            double time = run.Time;
            run.CancelTransientActions();
            Require(!run.TryHitSentinel(token, 1) && !run.TryResolveSentinelHit(1) && !run.TryResolveSentinelHit(2), "stale disable contact survived");
            Require(run.GetSentinel(1).AttackPhase == SentinelAttackPhase.Ready && run.GetSentinel(2).AttackPhase == SentinelAttackPhase.Ready,
                    "pending role action survived disable");
            Require(run.GetSentinel(0).IsDead && run.RewardCoins == 10 && run.DefeatedCount == 1 && run.Time == time,
                    "disable erased earned run state");
            Equal(cooldown, run.BurstCooldownRemaining, "disable refunded player cooldown");
            run.Advance(1.2f);
            Require(!run.TryResolveSentinelHit(1) && !run.TryResolveSentinelHit(2), "resumed old role action");
        });
        Check("player death freezes all role timers and rejects further admission or contacts", delegate {
            var run = new ClearingRun();
            for (int i = 0; i < Kinds.Length; i++) run.BeginSentinelAttack(i, Kinds[i]);
            run.Advance(.3f);
            double time = run.Time;
            run.NotifyPlayerDeath();
            run.Advance(10f);
            Require(run.IsDead && run.Time == time, "dead run kept simulating");
            for (int i = 0; i < Kinds.Length; i++)
                Require(!run.BeginSentinelAttack(i, Kinds[i]) && !run.TryResolveSentinelHit(i), "dead player accepted enemy action");
        });
        Check("killing committed roles cancels retaliation and grants the unchanged unique clear reward", delegate {
            var run = new ClearingRun();
            for (int i = 0; i < Kinds.Length; i++)
            {
                Require(run.BeginSentinelAttack(i, Kinds[i]), "role start rejected");
                Defeat(run, i);
                Require(run.GetSentinel(i).IsDead && !run.TryResolveSentinelHit(i), "dead enemy retaliated");
            }
            Require(run.GateUnlocked && run.RewardCoins == 30 && run.DefeatedCount == 3, "roles altered clear economy");
            Require(run.TryCompleteObjective() && !run.TryCompleteObjective(), "beacon completion changed");
            foreach (SentinelAttackKind kind in Kinds)
                Require(!run.BeginSentinelAttack(0, kind), "complete run reopened combat");
        });
        Check("retry resets all role state and never aliases a previous attempt attack token", delegate {
            var run = new ClearingRun();
            for (int i = 0; i < Kinds.Length; i++) run.BeginSentinelAttack(i, Kinds[i]);
            long old = run.GetSentinel(2).AttackSequence;
            long oldPlayer = PlayerAttack(run);
            run.ResetRun();
            for (int i = 0; i < Kinds.Length; i++)
            {
                SentinelState state = run.GetSentinel(i);
                Require(state.AttackKind == SentinelAttackKind.Sweep && state.AttackPhase == SentinelAttackPhase.Ready
                        && state.AttackSequence == 0 && !state.IsDead, "retry retained committed role state");
                Equal(54f, state.Health, "retry health");
            }
            Require(run.BeginSentinelAttack(2, SentinelAttackKind.Sigil) && run.GetSentinel(2).AttackSequence > old,
                    "retry reused enemy token");
            long fresh = PlayerAttack(run);
            Require(fresh > oldPlayer && !run.TryHitSentinel(oldPlayer, 2) && run.TryHitSentinel(fresh, 2), "retry aliased player token");
        });
        Check("warden footprint stays on its warning origin and includes the fixed square boundary", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Sweep, 3f, -2f, 20f, 40f);
            Equal(3f, shape.OriginX, "origin x"); Equal(-2f, shape.OriginY, "origin y");
            Equal(3f, shape.CenterX, "center x"); Equal(-2f, shape.CenterY, "center y");
            Equal(1.9f, shape.Width, "sweep width"); Equal(1.9f, shape.Height, "sweep height");
            Require(shape.Contains(3f, -2f) && shape.Contains(3.95f, -2f) && shape.Contains(3f, -2.95f), "valid melee boundary rejected");
            Require(!shape.Contains(3.951f, -2f) && !shape.Contains(3f, -2.951f) && !shape.Contains(20f, 40f),
                    "sweep expanded to movement or target");
        });
        Check("Lancer horizontal warning hits its stationary forward strip and excludes targets outside either edge", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Lance, 0f, 0f, 2f, 0f);
            Equal(1.6f, shape.CenterX, "lane midpoint x"); Equal(0f, shape.CenterY, "lane midpoint y");
            Equal(3.2f, shape.Width, "lane length"); Equal(.7f, shape.Height, "lane width");
            Equal(0f, shape.AngleDegrees, "east lane angle");
            Require(shape.Contains(0f, 0f) && shape.Contains(3.2f, 0f) && shape.Contains(1.6f, .35f), "inclusive cardinal lane edge rejected");
            Require(!shape.Contains(-.001f, 0f) && !shape.Contains(3.201f, 0f) && !shape.Contains(1.6f, .351f), "lane edge overreach");
        });
        Check("Lancer vertical warning rotates the same stationary narrow contact strip", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Lance, 0f, 0f, 0f, 5f);
            Equal(0f, shape.CenterX, "vertical lane x"); Equal(1.6f, shape.CenterY, "vertical lane y");
            Equal(90f, shape.AngleDegrees, "north lane angle");
            Require(shape.Contains(0f, 1f) && shape.Contains(.34f, 2f), "vertical lane missed inside contact");
            Require(!shape.Contains(.36f, 2f) && !shape.Contains(0f, 3.21f) && !shape.Contains(0f, -.01f), "vertical lane bounds wrong");
        });
        Check("Lancer diagonal warning uses rotated containment rather than its axis-aligned bounding box", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Lance, 0f, 0f, 5f, 5f);
            Equal(45f, shape.AngleDegrees, "diagonal angle");
            Require(shape.Contains(1f, 1f) && shape.Contains(2f, 2f), "diagonal contact missed");
            Require(!shape.Contains(1f, 0f) && !shape.Contains(0f, 1f) && !shape.Contains(2.5f, 2.5f), "diagonal lane became broad square");
        });
        Check("Lancer west and south aiming remain forward relative to the original warning", delegate {
            SentinelFootprint west = Footprint(SentinelAttackKind.Lance, 4f, 3f, -4f, 3f);
            SentinelFootprint south = Footprint(SentinelAttackKind.Lance, 4f, 3f, 4f, -3f);
            Equal(2.4f, west.CenterX, "west midpoint"); Equal(1.4f, south.CenterY, "south midpoint");
            Require(west.Contains(2f, 3f) && !west.Contains(5f, 3f), "west lane points backward");
            Require(south.Contains(4f, 1f) && !south.Contains(4f, 4f), "south lane points backward");
        });
        Check("zero-distance Lancer aim has a finite downward fallback", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Lance, 2f, 3f, 2f, 3f);
            Equal(2f, shape.CenterX, "fallback midpoint x"); Equal(1.4f, shape.CenterY, "fallback midpoint y");
            Equal(-90f, shape.AngleDegrees, "fallback angle");
            Require(shape.Contains(2f, 2f) && !shape.Contains(2f, 4f), "fallback containment wrong");
        });
        Check("finite extreme and subnormal Lancer aims normalize without overflow or a false zero fallback", delegate {
            SentinelFootprint huge = Footprint(SentinelAttackKind.Lance, -float.MaxValue, -float.MaxValue,
                                               float.MaxValue, float.MaxValue);
            Equal(45f, huge.AngleDegrees, "overflow-safe diagonal aim");
            Require(!float.IsInfinity(huge.CenterX) && !float.IsInfinity(huge.CenterY)
                    && huge.Contains(huge.CenterX, huge.CenterY) && !huge.Contains(0f, 0f), "extreme lane became nonfinite or unbounded");
            SentinelFootprint tiny = Footprint(SentinelAttackKind.Lance, 0f, 0f, float.Epsilon, 0f);
            Equal(0f, tiny.AngleDegrees, "small finite aim mistaken for zero");
            Equal(1.6f, tiny.CenterX, "subnormal forward lane midpoint");
            Require(tiny.Contains(1f, 0f) && !tiny.Contains(0f, -1f), "subnormal lane used zero-aim fallback");
        });
        Check("seer pulse centers on the captured target and leaves the caster outside a distant mark", delegate {
            SentinelFootprint shape = Footprint(SentinelAttackKind.Sigil, -4f, 5f, 2f, 3f);
            Equal(-4f, shape.OriginX, "caster x"); Equal(5f, shape.OriginY, "caster y");
            Equal(2f, shape.CenterX, "pulse target x"); Equal(3f, shape.CenterY, "pulse target y");
            Equal(1.4f, shape.Width, "pulse width"); Equal(1.4f, shape.Height, "pulse height");
            Require(shape.Contains(2f, 3f) && shape.Contains(2.699f, 3.699f), "pulse missed target region");
            Require(!shape.Contains(2.701f, 3f) && !shape.Contains(2f, 3.701f) && !shape.Contains(-4f, 5f), "pulse expanded toward caster");
        });
        Check("locked attack geometry cannot home after target or caster coordinates change", delegate {
            float originX = 0f, originY = 0f, targetX = 2f, targetY = 0f;
            SentinelFootprint lane = Footprint(SentinelAttackKind.Lance, originX, originY, targetX, targetY);
            SentinelFootprint pulse = Footprint(SentinelAttackKind.Sigil, originX, originY, targetX, targetY);
            originX = 20f; originY = 20f; targetX = -5f; targetY = -5f;
            Require(lane.Contains(2f, 0f) && pulse.Contains(2f, 0f), "locked target contact moved");
            Require(!lane.Contains(targetX, targetY) && !pulse.Contains(targetX, targetY)
                    && !lane.Contains(originX, originY), "snapshot followed later coordinates");
            Equal(0f, lane.OriginX, "locked caster x"); Equal(2f, pulse.CenterX, "locked target x");
        });
        Check("invalid geometry inputs are rejected for every role and nonfinite contacts never hit", delegate {
            foreach (SentinelAttackKind kind in Kinds)
            {
                foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    Require(SentinelTactics.CreateFootprint(kind, invalid, 0f, 1f, 1f) == null
                            && SentinelTactics.CreateFootprint(kind, 0f, invalid, 1f, 1f) == null
                            && SentinelTactics.CreateFootprint(kind, 0f, 0f, invalid, 1f) == null
                            && SentinelTactics.CreateFootprint(kind, 0f, 0f, 1f, invalid) == null, "nonfinite footprint admitted");
                    SentinelFootprint shape = Footprint(kind, 0f, 0f, 1f, 1f);
                    Require(!shape.Contains(invalid, 0f) && !shape.Contains(0f, invalid), "nonfinite contact admitted");
                }
            }
            Require(SentinelTactics.CreateFootprint((SentinelAttackKind)99, 0f, 0f, 1f, 1f) == null, "unknown footprint admitted");
        });
        Console.WriteLine("Sentinel tactics: " + passed + " passed, " + failed + " failed; pure production C#, no Unity stubs.");
        return failed == 0 ? 0 : 1;
    }

    private static long PlayerAttackAfterCooldown(ClearingRun run)
    {
        run.Advance(ClearingRun.BurstCooldown);
        return PlayerAttack(run);
    }
}
