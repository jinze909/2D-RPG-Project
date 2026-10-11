using System;
using Rpg.Gameplay;

// Actual production dialogue: no Unity, fake reward writer or invented coins.
internal static class RangerDialogueChecks
{
    private static int passed, failed;
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Contains(string value, string expected, string message)
    { Require(value.IndexOf(expected, StringComparison.Ordinal) >= 0, message); }
    private static void Check(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static RangerExpeditionSnapshot Snapshot(string id = "forest-A", int seeds = 0,
        int kills = 0, bool complete = false, bool banked = false)
    { return new RangerExpeditionSnapshot(id, seeds, kills, complete, banked); }
    private static RangerDialogue Accepted(RangerExpeditionSnapshot snapshot = null)
    {
        var dialogue = new RangerDialogue();
        Require(dialogue.Open(snapshot), "setup open");
        Require(dialogue.Advance(snapshot) == RangerDialogueAction.Accepted, "setup acceptance");
        return dialogue;
    }
    public static int Main()
    {
        Check("fresh NPC is closed and creates no accepted or reported objective", () =>
        {
            var d = new RangerDialogue();
            Require(!d.IsOpen && !d.Accepted && !d.Reported && !d.CanPurchaseTraining &&
                d.Page == RangerDialoguePage.Intro, "fresh state");
            Contains(d.Speaker, "Mira", "NPC identity absent");
            Contains(d.Body, "Thornwood", "forest narrative absent");
        });
        Check("opening introduction never accepts the optional survey", () =>
        {
            var d = new RangerDialogue();
            Require(d.Open(null) && d.IsOpen && !d.Accepted && !d.Reported, "open mutated mission");
            Contains(d.Prompt, "optional", "acceptance must be optional");
            Require(!d.Open(Snapshot()) && d.Page == RangerDialoguePage.Intro && !d.Accepted,
                "duplicate open reset/accepted page");
        });
        Check("E accepts once and progresses one page without creating report", () =>
        {
            var d = Accepted();
            Require(d.Accepted && !d.Reported && d.Page == RangerDialoguePage.FieldReport &&
                !d.CanPurchaseTraining, "accepted state");
            Contains(d.Body, "F at the saved beacon", "first trip guidance");
            Require(d.Advance(null) == RangerDialogueAction.Advanced && d.Accepted && !d.Reported &&
                d.CanPurchaseTraining, "training must be available without making survey mandatory");
            Require(d.Advance(null) == RangerDialogueAction.Closed && !d.IsOpen && d.Accepted,
                "training close destroyed objective");
        });
        Check("closed E cannot accept advance report or purchase", () =>
        {
            var d = new RangerDialogue();
            Require(d.Advance(Snapshot("A", 3, 3, true, true)) == RangerDialogueAction.None &&
                !d.IsOpen && !d.Accepted && !d.Reported && !d.CanPurchaseTraining, "closed input mutated objective");
        });
        Check("T or Esc style close and reopen preserve unaccepted introduction", () =>
        {
            var d = new RangerDialogue(); d.Open(null); d.Close();
            Require(!d.IsOpen && !d.Accepted && !d.Reported && d.Open(null) &&
                d.Page == RangerDialoguePage.Intro, "close accepted or lost introduction");
        });
        Check("close and reopen preserve accepted real progress without reporting", () =>
        {
            var d = Accepted(Snapshot()); d.Close();
            Require(d.Open(Snapshot("forest-A", 2, 1)) && d.Accepted && !d.Reported &&
                d.Page == RangerDialoguePage.FieldReport, "accepted reopen state");
            Contains(d.Body, "Thorn seeds: 2/3", "seed progress");
            Contains(d.Body, "Briar Stalkers: 1/3", "kill progress");
            Contains(d.Body, "south", "unconditional return guidance");
        });
        Check("live readonly refresh updates field text while page remains open", () =>
        {
            var d = Accepted(Snapshot());
            d.Update(Snapshot("forest-A", 1, 2));
            Contains(d.Body, "Thorn seeds: 1/3", "live seeds");
            Contains(d.Body, "Briar Stalkers: 2/3", "live kills");
            Require(d.IsOpen && d.Page == RangerDialoguePage.FieldReport && !d.Reported,
                "refresh advanced page or report");
        });
        Check("all objective completion combinations require actual saved cache before report", () =>
        {
            for (int seeds = 0; seeds <= 3; seeds++) for (int kills = 0; kills <= 3; kills++)
                foreach (bool complete in new[] { false, true }) foreach (bool banked in new[] { false, true })
                {
                    var snapshot = Snapshot("A", seeds, kills, complete, banked);
                    var d = Accepted(snapshot);
                    bool ready = seeds == 3 && kills == 3 && complete && banked;
                    Require(d.Advance(snapshot) == (ready ? RangerDialogueAction.Reported : RangerDialogueAction.Advanced),
                        "invalid completion/saved report for " + seeds + "/" + kills + "/" + complete + "/" + banked);
                    Require(d.Reported == ready && d.CanPurchaseTraining, "report state admission");
                }
        });
        Check("three seeds and kills signal north cache rather than a saved reward", () =>
        {
            var snapshot = Snapshot("A", 3, 3); var d = Accepted(snapshot);
            Contains(d.Body, "North cache ready", "ready cache guidance");
            Require(d.Advance(snapshot) == RangerDialogueAction.Advanced && !d.Reported,
                "ready cache reported without opening or saving");
        });
        Check("failed reward save gets explicit retry instructions and cannot report", () =>
        {
            var snapshot = Snapshot("A", 3, 3, true, false); var d = Accepted(snapshot);
            Contains(d.Body, "NOT SAVED", "save failure hidden");
            Contains(d.Body, "retry saving", "failed save recovery absent");
            Require(d.Prompt.IndexOf("Report survey", StringComparison.Ordinal) < 0 &&
                d.Advance(snapshot) == RangerDialogueAction.Advanced && !d.Reported,
                "failed save offered/reported survey");
        });
        Check("successful save retry becomes reportable without restarting or reaccepting", () =>
        {
            var d = Accepted(Snapshot("A", 3, 3, true, false));
            var saved = Snapshot("A", 3, 3, true, true); d.Update(saved);
            Contains(d.Prompt, "Report survey", "successful retry not recognized");
            Require(d.Advance(saved) == RangerDialogueAction.Reported && d.Reported && d.CanPurchaseTraining,
                "successful save report rejected");
            Contains(d.Body, "logged your findings", "report feedback absent");
        });
        Check("report is once per current expedition across repeated E and reopening", () =>
        {
            var saved = Snapshot("A", 3, 3, true, true); var d = Accepted(saved);
            Require(d.Advance(saved) == RangerDialogueAction.Reported, "first report rejected");
            Require(d.Advance(saved) == RangerDialogueAction.Closed && d.Reported,
                "next E reported twice or erased receipt");
            Require(d.Advance(saved) == RangerDialogueAction.None && d.Reported && !d.CanPurchaseTraining,
                "closed repeated E reported");
            Require(d.Open(saved) && d.Page == RangerDialoguePage.FieldReport, "reported reopen");
            Contains(d.Body, "already logged", "reopen looks unreported");
            Contains(d.Body, "cache coins", "training link absent");
            Require(d.Advance(saved) == RangerDialogueAction.Advanced && d.Reported, "reopen reported twice");
        });
        Check("snapshot refreshed on E rejects stale previously ready cache", () =>
        {
            var d = Accepted(Snapshot("A", 3, 3, true, true));
            Require(d.Advance(Snapshot("A", 3, 3, true, false)) == RangerDialogueAction.Advanced && !d.Reported,
                "E used stale banked UI instead of current runtime authority");
        });
        Check("genuine forest retry resets report while preserving optional acceptance", () =>
        {
            var saved = Snapshot("A", 3, 3, true, true); var d = Accepted(saved);
            d.Advance(saved); Require(d.Reported, "setup report");
            d.Update(Snapshot("B"));
            Require(d.Accepted && !d.Reported, "new forest cleared acceptance or retained old report");
            d.Close(); d.Open(Snapshot("B"));
            Contains(d.Body, "Thorn seeds: 0/3", "new trip shows old progress");
            Require(d.Advance(Snapshot("B")) == RangerDialogueAction.Advanced && !d.Reported,
                "new incomplete trip reported from prior receipt");
        });
        Check("new completed expedition can be reported exactly once without reacceptance", () =>
        {
            var savedA = Snapshot("A", 3, 3, true, true); var d = Accepted(savedA);
            d.Advance(savedA); d.Close();
            var savedB = Snapshot("B", 3, 3, true, true);
            d.Open(savedB);
            Require(!d.Reported && d.Accepted && d.Advance(savedB) == RangerDialogueAction.Reported && d.Reported,
                "new saved expedition could not report independently");
        });
        Check("null empty or whitespace expedition cannot manufacture completed report", () =>
        {
            foreach (string id in new[] { null, "", " \t\n" })
            {
                var snapshot = Snapshot(id, 3, 3, true, true); var d = Accepted(snapshot);
                Require(!snapshot.HasExpedition && snapshot.SeedsCollected == 0 && snapshot.StalkersDefeated == 0 &&
                    !snapshot.CacheComplete && !snapshot.RewardBanked &&
                    d.Advance(snapshot) == RangerDialogueAction.Advanced && !d.Reported, "empty identity manufactured report");
            }
        });
        Check("invalid snapshot counts remain bounded and closed update never opens conversation", () =>
        {
            var snapshot = Snapshot("A", int.MinValue, int.MaxValue, false, true);
            Require(snapshot.SeedsCollected == 0 && snapshot.StalkersDefeated == 3 &&
                !snapshot.RewardBanked, "snapshot normalization or cache/save dependency");
            var d = new RangerDialogue(); d.Update(snapshot);
            Require(!d.IsOpen && !d.Accepted && !d.Reported && !d.CanPurchaseTraining, "update opened or accepted dialogue");
        });
        Check("clearing retry reset clears all session mission identity and page state", () =>
        {
            var saved = Snapshot("A", 3, 3, true, true); var d = Accepted(saved); d.Advance(saved);
            d.Reset();
            Require(!d.IsOpen && !d.Accepted && !d.Reported && !d.CanPurchaseTraining &&
                d.Page == RangerDialoguePage.Intro && d.Advance(saved) == RangerDialogueAction.None, "clearing reset incomplete");
            Require(d.Open(null) && d.Page == RangerDialoguePage.Intro && !d.Accepted,
                "retry reused old accepted survey");
        });
        Check("training instructions preserve next-new-run upgrades and no extra reward", () =>
        {
            var d = Accepted(); d.Advance(null);
            Require(d.CanPurchaseTraining && d.IsOpen && !d.Reported, "optional preparation unavailable");
            Contains(d.Body, "saved coins only", "unbanked spending offered");
            Contains(d.Body, "next new run", "travel applies upgrades claim");
            Contains(d.Prompt, "1 / 2", "original upgrade inputs absent");
            d.Close(); Require(!d.CanPurchaseTraining, "closed training still admits purchase");
        });
        Check("current completed expedition can be surveyed after voluntary acceptance", () =>
        {
            var saved = Snapshot("A", 3, 3, true, true); var d = new RangerDialogue();
            d.Open(saved);
            Require(!d.Accepted && !d.Reported && d.Advance(saved) == RangerDialogueAction.Accepted &&
                !d.Reported, "opening retroactively accepted/reported expedition");
            Require(d.Advance(saved) == RangerDialogueAction.Reported && d.Reported,
                "optional late acceptance incorrectly forced another completed expedition");
        });
        Console.WriteLine("Ranger dialogue checks: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
