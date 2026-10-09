using System;

namespace Rpg.Gameplay
{
    public enum ClearingUpgrade { Vitality, Focus }
    public enum ProgressActionResult
    {
        Saved, AlreadyBanked, RunIncomplete, InsufficientCoins, MaxRank,
        Unavailable, SaveFailed, CapacityReached
    }
    public enum ProgressLoadKind { New, Loaded, Recovered, Unavailable, Unsupported }

    /// <summary>A bounded, immutable v1 ledger. It never owns a PlayerStats asset.</summary>
    public sealed class ClearingProgressData
    {
        public const int MaxClearedRuns = 1000000;
        public const int MaxUpgradeRank = 3;
        public const int CompletionCoins = 30;
        public static readonly ClearingProgressData Fresh = new ClearingProgressData(0, 0, 0, 0, 0, "");

        public int Revision { get; private set; }
        public int BankCoins { get; private set; }
        public int ClearedRuns { get; private set; }
        public int VitalityRank { get; private set; }
        public int FocusRank { get; private set; }
        public string LastRewardId { get; private set; }

        public ClearingProgressData(int revision, int bankCoins, int clearedRuns,
            int vitalityRank, int focusRank, string lastRewardId)
        {
            if (clearedRuns < 0 || clearedRuns > MaxClearedRuns || bankCoins < 0
                || vitalityRank < 0 || vitalityRank > MaxUpgradeRank
                || focusRank < 0 || focusRank > MaxUpgradeRank)
                throw new ArgumentOutOfRangeException("clearedRuns", "Progression values exceed the v1 bounds.");
            if (revision != clearedRuns + vitalityRank + focusRank
                || (long)bankCoins + Spent(vitalityRank) + Spent(focusRank) != (long)clearedRuns * CompletionCoins)
                throw new ArgumentException("Progression revision and coin ledger must agree.");
            if (lastRewardId == null || (clearedRuns == 0 ? lastRewardId.Length != 0 : !ValidRewardId(lastRewardId)))
                throw new ArgumentException("Completed runs require a 32-character hexadecimal reward ID.", "lastRewardId");
            Revision = revision;
            BankCoins = bankCoins;
            ClearedRuns = clearedRuns;
            VitalityRank = vitalityRank;
            FocusRank = focusRank;
            LastRewardId = lastRewardId.ToLowerInvariant();
        }

        internal static bool ValidRewardId(string value)
        {
            if (value == null || value.Length != 32) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            }
            return true;
        }

        internal static int Spent(int rank) { return rank == 0 ? 0 : rank == 1 ? 30 : rank == 2 ? 75 : 135; }
        internal bool SameAs(ClearingProgressData other)
        {
            return other != null && Revision == other.Revision && BankCoins == other.BankCoins
                && ClearedRuns == other.ClearedRuns && VitalityRank == other.VitalityRank
                && FocusRank == other.FocusRank && LastRewardId == other.LastRewardId;
        }
    }

    public sealed class ProgressLoadResult
    {
        public ClearingProgressData Data { get; private set; }
        public ProgressLoadKind Kind { get; private set; }
        public bool CanWrite { get; private set; }
        public ProgressLoadResult(ClearingProgressData data, ProgressLoadKind kind, bool canWrite)
        {
            if (data == null) throw new ArgumentNullException("data");
            Data = data;
            Kind = kind;
            CanWrite = canWrite;
        }
    }

    public interface IClearingProgressStore
    {
        ProgressLoadResult Load();
        bool TrySave(ClearingProgressData expected, ClearingProgressData next);
    }

    /// <summary>Commit-before-feedback progression: a failed save changes no live reward or rank.</summary>
    public sealed class ClearingProgress
    {
        private readonly IClearingProgressStore store;
        public ClearingProgressData Data { get; private set; }
        public ProgressLoadKind LoadKind { get; private set; }
        public bool CanWrite { get; private set; }
        public bool SaveFailed { get; private set; }
        public int BonusHealth { get { return Data.VitalityRank * 2; } }
        public int BonusMana { get { return Data.FocusRank * 2; } }

        public ClearingProgress(IClearingProgressStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            this.store = store;
            ProgressLoadResult loaded = store.Load();
            Data = loaded.Data;
            LoadKind = loaded.Kind;
            CanWrite = loaded.CanWrite;
        }

        public int Price(ClearingUpgrade upgrade)
        {
            if (upgrade != ClearingUpgrade.Vitality && upgrade != ClearingUpgrade.Focus) return 0;
            int rank = upgrade == ClearingUpgrade.Vitality ? Data.VitalityRank : Data.FocusRank;
            return rank == 0 ? 30 : rank == 1 ? 45 : rank == 2 ? 60 : 0;
        }

        public bool IsCompletionBanked(string rewardId)
        {
            return ClearingProgressData.ValidRewardId(rewardId)
                && string.Equals(Data.LastRewardId, rewardId, StringComparison.OrdinalIgnoreCase);
        }

        public ProgressActionResult BankCompletion(ClearingRun run, string rewardId)
        {
            if (run == null || !run.IsComplete || run.RewardCoins != ClearingProgressData.CompletionCoins)
                return ProgressActionResult.RunIncomplete;
            if (!ClearingProgressData.ValidRewardId(rewardId)) return ProgressActionResult.Unavailable;
            if (IsCompletionBanked(rewardId)) return ProgressActionResult.AlreadyBanked;
            if (!CanWrite) return ProgressActionResult.Unavailable;
            if (Data.ClearedRuns == ClearingProgressData.MaxClearedRuns) return ProgressActionResult.CapacityReached;
            return Save(new ClearingProgressData(Data.Revision + 1,
                Data.BankCoins + ClearingProgressData.CompletionCoins, Data.ClearedRuns + 1,
                Data.VitalityRank, Data.FocusRank, rewardId));
        }

        public ProgressActionResult Buy(ClearingUpgrade upgrade)
        {
            if (upgrade != ClearingUpgrade.Vitality && upgrade != ClearingUpgrade.Focus)
                return ProgressActionResult.Unavailable;
            if (!CanWrite) return ProgressActionResult.Unavailable;
            int price = Price(upgrade);
            if (price == 0) return ProgressActionResult.MaxRank;
            if (Data.BankCoins < price) return ProgressActionResult.InsufficientCoins;
            return Save(new ClearingProgressData(Data.Revision + 1, Data.BankCoins - price, Data.ClearedRuns,
                Data.VitalityRank + (upgrade == ClearingUpgrade.Vitality ? 1 : 0),
                Data.FocusRank + (upgrade == ClearingUpgrade.Focus ? 1 : 0), Data.LastRewardId));
        }

        private ProgressActionResult Save(ClearingProgressData next)
        {
            if (!store.TrySave(Data, next))
            {
                SaveFailed = true;
                return ProgressActionResult.SaveFailed;
            }
            Data = next;
            SaveFailed = false;
            return ProgressActionResult.Saved;
        }
    }
}
