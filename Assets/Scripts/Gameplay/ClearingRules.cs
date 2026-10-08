using System;

namespace Rpg.Gameplay
{
    public enum PlayerAttackKind { Light, Burst }
    public enum SentinelAttackPhase { Ready, Telegraph, Active, Recovery, Dead }

    /// <summary>Read-only state for one sentinel; positions and collision stay in Unity.</summary>
    public sealed class SentinelState
    {
        public float Health { get; private set; }
        public bool IsDead { get { return Health <= 0f; } }
        public SentinelAttackPhase AttackPhase { get; private set; }
        public float PhaseProgress { get; private set; }
        public long AttackSequence { get; private set; }

        internal double AttackStartedAt;
        internal bool HitResolved;

        internal SentinelState() { Reset(); }

        internal void Reset()
        {
            Health = ClearingRun.SentinelMaxHealth;
            AttackPhase = SentinelAttackPhase.Ready;
            PhaseProgress = 0f;
            AttackStartedAt = 0d;
            AttackSequence = 0;
            HitResolved = false;
        }

        internal void StartAttack(double time, long sequence)
        {
            AttackStartedAt = time;
            AttackSequence = sequence;
            HitResolved = false;
            SetPhase(SentinelAttackPhase.Telegraph, 0d);
        }

        internal void SetPhase(SentinelAttackPhase phase, double progress)
        {
            AttackPhase = phase;
            PhaseProgress = (float)Math.Max(0d, Math.Min(1d, progress));
        }

        internal void ApplyDamage(float amount)
        {
            Health = Math.Max(0f, Health - amount);
            if (IsDead) SetPhase(SentinelAttackPhase.Dead, 1d);
        }

        internal void CancelAttack()
        {
            if (!IsDead) SetPhase(SentinelAttackPhase.Ready, 0d);
            HitResolved = true;
        }
    }

    /// <summary>
    /// Engine-independent clearing combat and objective rules.
    /// PlayerStats/PlayerHealth/PlayerMana own player resources. The caller supplies
    /// current mana before spending it, applies SentinelDamage only after an accepted
    /// contact, and calls NotifyPlayerDeath when runtime health reaches zero.
    /// Advance receives simulation time, not wall-clock time; paused frames do not tick.
    /// </summary>
    public sealed class ClearingRun
    {
        public const int SentinelCount = 3;
        public const float SentinelMaxHealth = 54f;
        public const float LightDamage = 18f;
        public const float BurstDamage = 30f;
        public const float SentinelDamage = 2f;
        public const float LightCooldown = 0.45f;
        public const float BurstCooldown = 1.2f;
        public const float BurstManaCost = 6f;
        public const float ManaRegenerationRate = 0.8f;
        public const float LightAttackWindow = 0.18f;
        public const float BurstAttackWindow = 0.24f;
        public const float TelegraphDuration = 0.65f;
        public const float SentinelActiveDuration = 0.12f;
        public const float SentinelRecoveryDuration = 0.7f;
        public const float PlayerInvulnerabilityDuration = 0.55f;
        public const int CoinsPerSentinel = 10;

        private readonly SentinelState[] sentinels = new SentinelState[SentinelCount];
        private readonly bool[] playerHitTargets = new bool[SentinelCount];
        private long nextActionId;
        private long currentPlayerActionId;
        private PlayerAttackKind currentPlayerAttackKind;
        private double playerAttackExpiresAt;
        private double nextLightAt;
        private double nextBurstAt;
        private double playerInvulnerableUntil;

        public double Time { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsComplete { get; private set; }
        public int DefeatedCount { get; private set; }
        public int RewardCoins { get; private set; }
        public bool GateUnlocked { get { return DefeatedCount == SentinelCount; } }
        public float LightCooldownRemaining { get { return Remaining(nextLightAt); } }
        public float BurstCooldownRemaining { get { return Remaining(nextBurstAt); } }
        public float PlayerInvulnerabilityRemaining { get { return Remaining(playerInvulnerableUntil); } }
        public bool PlayerAttackActive { get { return Running && currentPlayerActionId != 0 && Time < playerAttackExpiresAt; } }

        private bool Running { get { return !IsDead && !IsComplete; } }

        public ClearingRun()
        {
            for (int i = 0; i < SentinelCount; i++) sentinels[i] = new SentinelState();
            ResetRun();
        }

        public SentinelState GetSentinel(int id)
        {
            if (!ValidSentinel(id)) throw new ArgumentOutOfRangeException("id");
            return sentinels[id];
        }

        /// <summary>Advance only with finite, positive simulation deltas.</summary>
        public void Advance(float deltaTime)
        {
            if (!Running || !PositiveFinite(deltaTime)) return;
            Time += deltaTime;
            for (int i = 0; i < SentinelCount; i++)
            {
                SentinelState sentinel = sentinels[i];
                if (sentinel.IsDead || sentinel.AttackPhase == SentinelAttackPhase.Ready) continue;
                double elapsed = Time - sentinel.AttackStartedAt;
                double activeAt = TelegraphDuration;
                double recoveryAt = activeAt + SentinelActiveDuration;
                double readyAt = recoveryAt + SentinelRecoveryDuration;
                if (elapsed < activeAt)
                    sentinel.SetPhase(SentinelAttackPhase.Telegraph, elapsed / TelegraphDuration);
                else if (elapsed < recoveryAt)
                    sentinel.SetPhase(SentinelAttackPhase.Active, (elapsed - activeAt) / SentinelActiveDuration);
                else if (elapsed < readyAt)
                    sentinel.SetPhase(SentinelAttackPhase.Recovery, (elapsed - recoveryAt) / SentinelRecoveryDuration);
                else
                    sentinel.SetPhase(SentinelAttackPhase.Ready, 0d);
            }
        }

        public bool CanPlayerAttack(PlayerAttackKind kind, float availableMana)
        {
            if (!Running || PlayerAttackActive || !NonnegativeFinite(availableMana)) return false;
            if (kind == PlayerAttackKind.Light) return Time >= nextLightAt;
            if (kind == PlayerAttackKind.Burst) return Time >= nextBurstAt && availableMana >= BurstManaCost;
            return false;
        }

        /// <summary>
        /// On acceptance, the Unity caller spends BurstManaCost for a burst using the
        /// same pre-spend balance passed here. Rejection never spends mana or starts cooldown.
        /// One action may hit each distinct sentinel once during its contact window.
        /// </summary>
        public bool BeginPlayerAttack(PlayerAttackKind kind, float availableMana, out long attackId)
        {
            attackId = 0;
            if (!CanPlayerAttack(kind, availableMana)) return false;
            attackId = ++nextActionId;
            currentPlayerActionId = attackId;
            currentPlayerAttackKind = kind;
            Array.Clear(playerHitTargets, 0, playerHitTargets.Length);
            if (kind == PlayerAttackKind.Light)
            {
                nextLightAt = Time + LightCooldown;
                playerAttackExpiresAt = Time + LightAttackWindow;
            }
            else
            {
                nextBurstAt = Time + BurstCooldown;
                playerAttackExpiresAt = Time + BurstAttackWindow;
            }
            return true;
        }

        public bool TryHitSentinel(long attackId, int sentinelId)
        {
            if (!PlayerAttackActive || attackId != currentPlayerActionId || !ValidSentinel(sentinelId)) return false;
            SentinelState sentinel = sentinels[sentinelId];
            if (sentinel.IsDead || playerHitTargets[sentinelId]) return false;
            playerHitTargets[sentinelId] = true;
            sentinel.ApplyDamage(currentPlayerAttackKind == PlayerAttackKind.Light ? LightDamage : BurstDamage);
            if (sentinel.IsDead)
            {
                // A sentinel can transition to dead only once. Contact callbacks cannot
                // duplicate rewards or advance the gate by repeatedly hitting a corpse.
                DefeatedCount++;
                RewardCoins += CoinsPerSentinel;
            }
            return true;
        }

        public bool BeginSentinelAttack(int sentinelId)
        {
            if (!Running || !ValidSentinel(sentinelId)) return false;
            SentinelState sentinel = sentinels[sentinelId];
            if (sentinel.IsDead || sentinel.AttackPhase != SentinelAttackPhase.Ready) return false;
            sentinel.StartAttack(Time, ++nextActionId);
            return true;
        }

        /// <summary>
        /// The bridge checks range against the direction locked at telegraph start.
        /// An accepted contact applies SentinelDamage once and starts shared player
        /// immunity. A contact blocked by immunity still consumes that enemy swing.
        /// Hitches which skip the active window never deal delayed, untelegraphed damage.
        /// </summary>
        public bool TryResolveSentinelHit(int sentinelId)
        {
            if (!Running || !ValidSentinel(sentinelId)) return false;
            SentinelState sentinel = sentinels[sentinelId];
            if (sentinel.AttackPhase != SentinelAttackPhase.Active || sentinel.HitResolved) return false;
            sentinel.HitResolved = true;
            if (Time < playerInvulnerableUntil) return false;
            playerInvulnerableUntil = Time + PlayerInvulnerabilityDuration;
            return true;
        }

        public void NotifyPlayerDeath()
        {
            if (!Running) return;
            IsDead = true;
            CancelActions();
        }

        public bool TryCompleteObjective()
        {
            if (!Running || !GateUnlocked) return false;
            IsComplete = true;
            CancelActions();
            return true;
        }

        /// <summary>Retry resets the run; the bridge separately resets runtime player resources and positions.</summary>
        public void ResetRun()
        {
            Time = 0d;
            IsDead = false;
            IsComplete = false;
            DefeatedCount = 0;
            RewardCoins = 0;
            nextLightAt = 0d;
            nextBurstAt = 0d;
            playerInvulnerableUntil = 0d;
            currentPlayerActionId = 0;
            playerAttackExpiresAt = 0d;
            currentPlayerAttackKind = PlayerAttackKind.Light;
            Array.Clear(playerHitTargets, 0, playerHitTargets.Length);
            for (int i = 0; i < SentinelCount; i++) sentinels[i].Reset();
            // nextActionId is intentionally retained so pre-retry contact tokens
            // cannot match a newly created attack in the next run.
        }

        private void CancelActions()
        {
            currentPlayerActionId = 0;
            playerAttackExpiresAt = Time;
            for (int i = 0; i < SentinelCount; i++) sentinels[i].CancelAttack();
        }

        private float Remaining(double until) { return (float)Math.Max(0d, until - Time); }
        private static bool ValidSentinel(int id) { return id >= 0 && id < SentinelCount; }
        private static bool PositiveFinite(float value) { return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool NonnegativeFinite(float value) { return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
