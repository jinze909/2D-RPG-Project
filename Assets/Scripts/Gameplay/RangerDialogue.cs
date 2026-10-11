using System;

namespace Rpg.Gameplay
{
    public enum RangerDialoguePage { Intro, FieldReport, Training }
    public enum RangerDialogueAction { None, Accepted, Reported, Advanced, Closed }

    /// <summary>
    /// Read-only view of the existing expedition authority. This is not a save
    /// record and cannot complete a cache, accept a reward or create coins.
    /// </summary>
    public sealed class RangerExpeditionSnapshot
    {
        public string ExpeditionId { get; private set; }
        public int SeedsCollected { get; private set; }
        public int StalkersDefeated { get; private set; }
        public bool CacheComplete { get; private set; }
        public bool RewardBanked { get; private set; }
        public bool HasExpedition { get { return ExpeditionId.Length != 0; } }

        public static readonly RangerExpeditionSnapshot Empty =
            new RangerExpeditionSnapshot(null, 0, 0, false, false);

        public RangerExpeditionSnapshot(string expeditionId, int seedsCollected,
            int stalkersDefeated, bool cacheComplete, bool rewardBanked)
        {
            ExpeditionId = string.IsNullOrWhiteSpace(expeditionId) ? "" : expeditionId;
            SeedsCollected = HasExpedition ? Math.Max(0, Math.Min(3, seedsCollected)) : 0;
            StalkersDefeated = HasExpedition ? Math.Max(0, Math.Min(3, stalkersDefeated)) : 0;
            CacheComplete = HasExpedition && cacheComplete;
            RewardBanked = CacheComplete && rewardBanked;
        }
    }

    /// <summary>
    /// Optional, session-local forest guidance. The runtime owns reach/alive/input
    /// admission and real upgrade transactions; this model owns dialogue only.
    /// </summary>
    public sealed class RangerDialogue
    {
        private RangerExpeditionSnapshot expedition = RangerExpeditionSnapshot.Empty;

        public bool IsOpen { get; private set; }
        public bool Accepted { get; private set; }
        public bool Reported { get; private set; }
        public RangerDialoguePage Page { get; private set; }
        public string Speaker { get { return "Mira - Forest keeper"; } }
        public bool CanPurchaseTraining { get { return IsOpen && Page == RangerDialoguePage.Training; } }

        public string Body
        {
            get
            {
                if (Page == RangerDialoguePage.Intro)
                    return "I am Mira. Thornwood's roots have stirred again.\n" +
                        "Three seeds reveal where they grow. Clear the Briar Stalkers,\n" +
                        "open the north cache, then return and tell me what you found.";
                if (Page == RangerDialoguePage.Training)
                    return (Reported ? "I've logged your findings. Let's plan your training.\n" :
                        "You can prepare before finishing the survey.\n") +
                        "1: Health training   2: Mana training\n" +
                        "Spend saved coins only. Upgrades apply on your next new run.";
                return FieldReportBody();
            }
        }

        public string Prompt
        {
            get
            {
                if (Page == RangerDialoguePage.Intro) return "E: Accept optional survey   T / Esc: Close";
                if (Page == RangerDialoguePage.Training) return "1 / 2: Train   E / T / Esc: Close";
                return CanReport ? "E: Report survey   T / Esc: Close" :
                    "E: Training   T / Esc: Close";
            }
        }

        private bool CanReport
        {
            get
            {
                return Accepted && !Reported && expedition.HasExpedition &&
                    expedition.SeedsCollected == 3 && expedition.StalkersDefeated == 3 &&
                    expedition.CacheComplete && expedition.RewardBanked;
            }
        }

        public void Update(RangerExpeditionSnapshot snapshot)
        {
            snapshot = snapshot ?? RangerExpeditionSnapshot.Empty;
            if (!string.Equals(expedition.ExpeditionId, snapshot.ExpeditionId, StringComparison.Ordinal))
                Reported = false;
            expedition = snapshot;
        }

        public bool Open(RangerExpeditionSnapshot snapshot)
        {
            Update(snapshot);
            if (IsOpen) return false;
            IsOpen = true;
            Page = Accepted ? RangerDialoguePage.FieldReport : RangerDialoguePage.Intro;
            return true;
        }

        public RangerDialogueAction Advance(RangerExpeditionSnapshot snapshot)
        {
            Update(snapshot);
            if (!IsOpen) return RangerDialogueAction.None;
            if (Page == RangerDialoguePage.Intro)
            {
                Accepted = true;
                Page = RangerDialoguePage.FieldReport;
                return RangerDialogueAction.Accepted;
            }
            if (Page == RangerDialoguePage.FieldReport)
            {
                bool report = CanReport;
                if (report) Reported = true;
                Page = RangerDialoguePage.Training;
                return report ? RangerDialogueAction.Reported : RangerDialogueAction.Advanced;
            }
            Close();
            return RangerDialogueAction.Closed;
        }

        public void Close() { IsOpen = false; }

        /// <summary>Only a genuine clearing retry discards this conversation.</summary>
        public void Reset()
        {
            Close();
            Accepted = false;
            Reported = false;
            Page = RangerDialoguePage.Intro;
            expedition = RangerExpeditionSnapshot.Empty;
        }

        private string FieldReportBody()
        {
            if (!expedition.HasExpedition)
                return "F at the saved beacon enters Thornwood.\n" +
                    "Gather three seeds; defeat three Briar Stalkers; open the north cache.\n" +
                    "E at the south trail returns anytime. Returning preserves this trip.";
            string counts = "Thorn seeds: " + expedition.SeedsCollected + "/3   Briar Stalkers: " +
                expedition.StalkersDefeated + "/3\n";
            if (Reported)
                return counts + "I've already logged your findings for this expedition.\n" +
                    "The cache coins can pay for your training.";
            if (expedition.CacheComplete)
                return counts + (expedition.RewardBanked ?
                    "North cache saved. Your survey is ready to report.\n" +
                    "The cache coins can pay for your training." :
                    "North cache opened, but its reward is NOT SAVED.\n" +
                    "Return to Thornwood and press E to retry saving before reporting.");
            return counts + (expedition.SeedsCollected == 3 && expedition.StalkersDefeated == 3 ?
                "North cache ready: press E near it to open and save its reward.\n" :
                "Finish both objectives, then open the north cache with E.\n") +
                "E at the south trail returns anytime; reentry preserves this trip.";
        }
    }
}
