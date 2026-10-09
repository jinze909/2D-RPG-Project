using System;

namespace Rpg.Gameplay
{
    internal enum SupplyClaimResult
    {
        Restored, Undiscovered, OutOfRange, Blocked, Spent, Inactive, Full, Invalid
    }

    // Attempt-local admission only. Runtime remains the owner of actor HP and MP.
    internal sealed class ClearingSupplies
    {
        internal const int Count = 2;
        internal const float DiscoveryRadius = 2.4f;
        internal const float UseRadius = .8f;
        internal const float HerbHealth = 4f;
        internal const float RuneMana = 6f;
        private readonly bool[] discovered = new bool[Count];
        private readonly bool[] claimed = new bool[Count];

        internal static float PositionX(int index)
        {
            if (index == 0) return -5.6f;
            if (index == 1) return 5.6f;
            throw new ArgumentOutOfRangeException("index");
        }

        internal static float PositionY(int index)
        {
            if (index == 0) return -2.35f;
            if (index == 1) return 1.15f;
            throw new ArgumentOutOfRangeException("index");
        }

        internal bool IsDiscovered(int index) { return ValidIndex(index) && discovered[index]; }
        internal bool IsClaimed(int index) { return ValidIndex(index) && claimed[index]; }

        internal bool TryDiscover(int index, float distance, bool clear, bool playable)
        {
            if (!ValidIndex(index) || !Finite(distance) || distance < 0f ||
                distance > DiscoveryRadius || !clear || !playable || discovered[index]) return false;
            discovered[index] = true;
            return true;
        }

        internal SupplyClaimResult TryClaim(int index, float distance, bool clear, bool playable,
            float currentHealth, float maxHealth, float currentMana, float maxMana, out float gain)
        {
            gain = 0f;
            if (!ValidIndex(index) || !Finite(distance) || distance < 0f ||
                !ValidResources(currentHealth, maxHealth, currentMana, maxMana)) return SupplyClaimResult.Invalid;
            if (!playable) return SupplyClaimResult.Inactive;
            if (!discovered[index]) return SupplyClaimResult.Undiscovered;
            if (claimed[index]) return SupplyClaimResult.Spent;
            if (distance > UseRadius) return SupplyClaimResult.OutOfRange;
            if (!clear) return SupplyClaimResult.Blocked;
            float current = index == 0 ? currentHealth : currentMana;
            float maximum = index == 0 ? maxHealth : maxMana;
            float budget = index == 0 ? HerbHealth : RuneMana;
            if (current >= maximum) return SupplyClaimResult.Full;
            // Avoid overflowing float addition; report actual representable after-minus-before.
            float restored = (float)Math.Min((double)maximum, (double)current + budget);
            float actualGain = restored - current;
            if (!(actualGain > 0f)) return SupplyClaimResult.Full;
            // Coarse float rounding cannot turn a four/six-unit reserve into eight.
            if (!Finite(actualGain) || actualGain > budget) return SupplyClaimResult.Invalid;
            gain = actualGain;
            claimed[index] = true;
            return SupplyClaimResult.Restored;
        }

        internal void Reset()
        {
            for (int index = 0; index < Count; index++)
            {
                discovered[index] = false;
                claimed[index] = false;
            }
        }

        private static bool ValidIndex(int index) { return index >= 0 && index < Count; }
        private static bool ValidResources(float health, float maxHealth, float mana, float maxMana)
        {
            return Finite(health) && Finite(maxHealth) && Finite(mana) && Finite(maxMana) &&
                maxHealth > 0f && health > 0f && health <= maxHealth &&
                maxMana >= 0f && mana >= 0f && mana <= maxMana;
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
