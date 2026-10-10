using System;

namespace Rpg.Gameplay
{
    /// <summary>Locked geometry and admission ranges for the clearing's final guardian.</summary>
    public static class BossTactics
    {
        public static float RangeForKind(SentinelAttackKind kind)
        {
            if (kind == SentinelAttackKind.Lance) return 4f;
            if (kind == SentinelAttackKind.Sigil) return 6f;
            throw new ArgumentOutOfRangeException("kind");
        }

        public static SentinelFootprint CreateFootprint(SentinelAttackKind kind,
            float originX, float originY, float targetX, float targetY)
        {
            if (!SentinelTactics.Finite(originX) || !SentinelTactics.Finite(originY)
                || !SentinelTactics.Finite(targetX) || !SentinelTactics.Finite(targetY)) return null;
            if (kind == SentinelAttackKind.Sigil)
                return new SentinelFootprint(originX, originY, targetX, targetY, 1.8f, 1.8f, 1d, 0d);
            if (kind != SentinelAttackKind.Lance) return null;
            double aimX = (double)targetX - originX, aimY = (double)targetY - originY;
            double length = Math.Sqrt(aimX * aimX + aimY * aimY);
            if (length > 0d) { aimX /= length; aimY /= length; }
            else { aimX = 0d; aimY = -1d; }
            return new SentinelFootprint(originX, originY,
                (float)(originX + aimX * 2.1d), (float)(originY + aimY * 2.1d),
                4.2f, 0.9f, aimX, aimY);
        }
    }

    /// <summary>
    /// Pure attempt-owned boss state. Attacks capture their timing and footprint
    /// on admission; crossing half health never shortens an existing warning.
    /// </summary>
    public sealed class ClearingBossState
    {
        public const float MaxHealth = 108f;
        public const float Damage = 2f;
        public float Health { get; private set; }
        public bool IsAwake { get; private set; }
        public bool IsDead { get { return Health <= 0f; } }
        public bool Enraged { get { return IsAwake && !IsDead && Health <= MaxHealth * 0.5f; } }
        public SentinelAttackPhase AttackPhase { get; private set; }
        public SentinelAttackKind AttackKind { get; private set; }
        public SentinelAttackKind NextAttackKind { get; private set; }
        public SentinelFootprint AttackFootprint { get; private set; }
        public float PhaseProgress { get; private set; }
        public long AttackSequence { get; private set; }
        public float WindupDuration { get; private set; }
        public float ActiveDuration { get; private set; }
        public float RecoveryDuration { get; private set; }

        internal bool HitResolved;
        private double attackStartedAt;
        private bool interrupted;

        internal ClearingBossState() { Reset(); }

        internal void Reset()
        {
            Health = MaxHealth;
            IsAwake = false;
            AttackKind = SentinelAttackKind.Lance;
            NextAttackKind = SentinelAttackKind.Lance;
            AttackSequence = 0;
            AttackFootprint = null;
            WindupDuration = 1f;
            ActiveDuration = 0.16f;
            RecoveryDuration = 1.15f;
            attackStartedAt = 0d;
            HitResolved = false;
            interrupted = false;
            SetPhase(SentinelAttackPhase.Ready, 0d);
        }

        internal void Awaken() { IsAwake = true; }

        internal void StartAttack(double time, long sequence, SentinelFootprint footprint)
        {
            AttackKind = NextAttackKind;
            NextAttackKind = AttackKind == SentinelAttackKind.Lance
                ? SentinelAttackKind.Sigil : SentinelAttackKind.Lance;
            AttackFootprint = footprint;
            AttackSequence = sequence;
            WindupDuration = Enraged ? 0.8f : 1f;
            ActiveDuration = 0.16f;
            RecoveryDuration = Enraged ? 0.9f : 1.15f;
            attackStartedAt = time;
            HitResolved = false;
            interrupted = false;
            SetPhase(SentinelAttackPhase.Telegraph, 0d);
        }

        internal void Advance(double time)
        {
            if (!IsAwake || IsDead || AttackPhase == SentinelAttackPhase.Ready) return;
            double elapsed = time - attackStartedAt;
            double recoveryAt = (double)WindupDuration + ActiveDuration;
            double readyAt = recoveryAt + RecoveryDuration;
            if (elapsed >= readyAt)
            {
                AttackFootprint = null;
                SetPhase(SentinelAttackPhase.Ready, 0d);
            }
            else if (interrupted || elapsed >= recoveryAt)
                SetPhase(SentinelAttackPhase.Recovery, (elapsed - recoveryAt) / RecoveryDuration);
            else if (elapsed >= WindupDuration)
                SetPhase(SentinelAttackPhase.Active, (elapsed - WindupDuration) / ActiveDuration);
            else
                SetPhase(SentinelAttackPhase.Telegraph, elapsed / WindupDuration);
        }

        internal void ApplyDamage(float damage)
        {
            Health = Math.Max(0f, Health - damage);
            if (IsDead)
            {
                HitResolved = true;
                AttackFootprint = null;
                SetPhase(SentinelAttackPhase.Dead, 1d);
            }
        }

        internal void CancelAttack()
        {
            HitResolved = true;
            AttackFootprint = null;
            if (!IsAwake || IsDead || AttackPhase == SentinelAttackPhase.Ready) return;
            // Retain the original ready deadline. Disabling cannot refund recovery,
            // and the old warning can never resume as an invisible active contact.
            interrupted = true;
            SetPhase(SentinelAttackPhase.Recovery, PhaseProgress);
        }

        private void SetPhase(SentinelAttackPhase phase, double progress)
        {
            AttackPhase = phase;
            PhaseProgress = (float)Math.Max(0d, Math.Min(1d, progress));
        }
    }
}
