using System;

namespace Rpg.Gameplay
{
    /// <summary>
    /// One attempt-local forest expedition. Player resources remain actor-owned;
    /// the existing combat and v1 reward ledger are reused without a save migration.
    /// Seed collection grants no coins. Only the opened cache completes Combat,
    /// allowing the existing save-before-mutate BankCompletion to bank its 30 coins.
    /// </summary>
    public sealed class ThornwoodRun
    {
        public const int SeedCount = ThornwoodLayout.SeedCount;
        public const float UseRadius = .75f;
        public const float StalkerSpeed = 1.55f;
        public const float ChaseRadius = 6f;
        public const float PounceRange = 2.4f;
        private readonly bool[] seeds = new bool[SeedCount];
        private readonly SentinelFootprint[] footprints = new SentinelFootprint[ClearingRun.SentinelCount];

        public ClearingRun Combat { get; private set; }
        public bool Entered { get; private set; }
        public bool Paused { get; private set; }
        public int SeedsCollected { get; private set; }
        public bool IsPlayable { get { return Entered && !Paused && !Combat.IsDead && !Combat.IsComplete; } }
        public bool CacheReady { get { return SeedsCollected == SeedCount && Combat.GateUnlocked; } }

        public ThornwoodRun() { Combat = new ClearingRun(false); }

        /// <summary>The live clearing must finish and save before its waystone admits a trip.</summary>
        public static bool IsUnlocked(bool clearingComplete, bool clearingRewardBanked)
        { return clearingComplete && clearingRewardBanked; }

        public bool TryEnter(bool clearingComplete, bool clearingRewardBanked, bool playerAlive, bool paused)
        {
            if (Entered || !IsUnlocked(clearingComplete, clearingRewardBanked) || !playerAlive || paused || Combat.IsDead)
                return false;
            Entered = true;
            Paused = false;
            return true;
        }

        /// <summary>No objective, reward-save, health or enemy gate can trap the actor in the forest.</summary>
        public bool TryLeave()
        {
            if (!Entered) return false;
            Suspend();
            Entered = false;
            Paused = false;
            return true;
        }

        /// <summary>Pause preserves warning snapshots and simulation deadlines; it is not Suspend.</summary>
        public void SetPaused(bool value) { Paused = value; }

        public void Advance(float deltaTime)
        {
            if (!IsPlayable) return;
            Combat.Advance(deltaTime);
            for (int id = 0; id < footprints.Length; id++)
            {
                SentinelState state = Combat.GetSentinel(id);
                if (state.IsDead || state.AttackPhase == SentinelAttackPhase.Ready) footprints[id] = null;
            }
        }

        public bool IsSeedCollected(int id) { return id >= 0 && id < SeedCount && seeds[id]; }

        public bool TryCollectSeed(int id, float playerX, float playerY, bool clear)
        {
            if (!IsPlayable || id < 0 || id >= SeedCount || seeds[id] || !clear ||
                !Near(playerX, playerY, ThornwoodLayout.SeedPosition(id))) return false;
            seeds[id] = true;
            SeedsCollected++;
            return true;
        }

        public bool TryComplete(float playerX, float playerY, bool clear)
        {
            if (!IsPlayable || !CacheReady || !clear || !Near(playerX, playerY, ThornwoodLayout.Cache)) return false;
            if (!Combat.TryCompleteObjective()) return false;
            Array.Clear(footprints, 0, footprints.Length);
            return true;
        }

        public bool TryBeginPlayerAttack(PlayerAttackKind kind, float availableMana, out long attackId)
        {
            attackId = 0;
            return IsPlayable && Combat.BeginPlayerAttack(kind, availableMana, out attackId);
        }

        public bool TryHitStalker(long attackId, int id)
        {
            if (!IsPlayable || !Combat.TryHitSentinel(attackId, id)) return false;
            if (Combat.GetSentinel(id).IsDead) footprints[id] = null;
            return true;
        }

        public SentinelDecision ChooseStalkerAction(int id, float distance, bool clear)
        {
            if (!ValidStalker(id) || !IsPlayable || !SentinelTactics.Finite(distance) || distance < 0f || !clear)
                return SentinelDecision.Hold;
            SentinelState state = Combat.GetSentinel(id);
            if (state.IsDead || state.AttackPhase != SentinelAttackPhase.Ready) return SentinelDecision.Hold;
            if (distance <= PounceRange) return SentinelDecision.Attack;
            return distance < ChaseRadius ? SentinelDecision.Approach : SentinelDecision.Hold;
        }

        public bool TryBeginStalkerAttack(int id, float originX, float originY, float targetX, float targetY, bool clear)
        {
            if (!ValidStalker(id) || !IsPlayable || !clear ||
                !ThornwoodLayout.CanOccupy(originX, originY, 0f) || !ThornwoodLayout.CanOccupy(targetX, targetY, 0f) ||
                !ThornwoodLayout.HasLineOfSight(originX, originY, targetX, targetY)) return false;
            double dx = (double)targetX - originX, dy = (double)targetY - originY;
            if (dx * dx + dy * dy > (double)PounceRange * PounceRange) return false;
            SentinelFootprint snapshot = SentinelTactics.CreateFootprint(SentinelAttackKind.Lance, originX, originY, targetX, targetY);
            if (snapshot == null || !Combat.BeginSentinelAttack(id, SentinelAttackKind.Lance)) return false;
            footprints[id] = snapshot;
            return true;
        }

        public SentinelFootprint GetStalkerFootprint(int id)
        {
            if (!ValidStalker(id)) return null;
            SentinelState state = Combat.GetSentinel(id);
            return state.IsDead || state.AttackPhase == SentinelAttackPhase.Ready ? null : footprints[id];
        }

        public bool TryResolveStalkerHit(int id, float playerX, float playerY, bool clear)
        {
            SentinelFootprint snapshot = GetStalkerFootprint(id);
            return IsPlayable && clear && snapshot != null && snapshot.Contains(playerX, playerY) &&
                ThornwoodLayout.HasLineOfSight(snapshot.OriginX, snapshot.OriginY, playerX, playerY) &&
                Combat.TryResolveSentinelHit(id);
        }

        public void NotifyPlayerDeath()
        {
            Combat.NotifyPlayerDeath();
            Array.Clear(footprints, 0, footprints.Length);
        }

        /// <summary>Disable/leave cancel contacts while preserving HP, collected seeds, cooldowns and reward state.</summary>
        public void Suspend()
        {
            Combat.CancelTransientActions();
            Array.Clear(footprints, 0, footprints.Length);
        }

        /// <summary>Only a genuine retry/new expedition resets attempt-local progress.</summary>
        public void Reset()
        {
            Combat.ResetRun();
            Array.Clear(seeds, 0, seeds.Length);
            Array.Clear(footprints, 0, footprints.Length);
            SeedsCollected = 0;
            Entered = false;
            Paused = false;
        }

        private static bool Near(float x, float y, ThornwoodPoint point)
        {
            if (!ThornwoodLayout.CanOccupy(x, y, 0f) || !ThornwoodLayout.HasLineOfSight(x, y, point.X, point.Y)) return false;
            double dx = (double)x - point.X, dy = (double)y - point.Y;
            return dx * dx + dy * dy <= (double)UseRadius * UseRadius;
        }

        private static bool ValidStalker(int id) { return id >= 0 && id < ClearingRun.SentinelCount; }
    }
}
