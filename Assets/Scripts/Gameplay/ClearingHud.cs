using UnityEngine;
using UnityEngine.UI;

namespace Rpg.Gameplay
{
    // No full-screen panel during play: combat stays visible between the edge labels.
    internal sealed class ClearingHud
    {
        private readonly Transform root;
        private readonly Font font;
        private readonly Text resources;
        private readonly Text objective;
        private readonly Text skills;
        private readonly Text help;
        private readonly Text message;
        private readonly Text terminal;
        private bool completionBankedOverride;
        private bool forestTrailAvailable;

        internal void SetVisible(bool visible) { root.gameObject.SetActive(visible); }
        internal void SetCompletionContext(bool banked, bool trailAvailable)
        {
            completionBankedOverride = banked;
            forestTrailAvailable = trailAvailable;
        }

        internal ClearingHud(Transform parent)
        {
            GameObject canvasObject = new GameObject("Compact clearing HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            root = canvasObject.transform;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resources = Label("Resources", new Vector2(0, 1), new Vector2(14, -12), new Vector2(260, 35), TextAnchor.UpperLeft, 17);
            objective = Label("Objective", new Vector2(1, 1), new Vector2(-14, -12), new Vector2(350, 50), TextAnchor.UpperRight, 16);
            skills = Label("Combat keys", new Vector2(0, 0), new Vector2(14, 12), new Vector2(400, 43), TextAnchor.LowerLeft, 15);
            help = Label("Movement keys", new Vector2(1, 0), new Vector2(-14, 12), new Vector2(350, 66), TextAnchor.LowerRight, 14);
            help.text = MovementGuide(false);
            // Feedback replaces the secondary help column; never place a persistent
            // message across the south combat lane or draw two labels in this slot.
            message = Label("Feedback", new Vector2(1, 0), new Vector2(-14, 12), new Vector2(350, 66), TextAnchor.LowerRight, 14);
            terminal = Label("Run result", new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 135), TextAnchor.MiddleCenter, 24);
            terminal.color = ClearingPalette.Cream;
        }

        private Text Label(string name, Vector2 anchor, Vector2 offset, Vector2 size, TextAnchor alignment, int fontSize)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            item.transform.SetParent(root, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            Text label = item.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = ClearingPalette.Cream;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            Outline outline = item.GetComponent<Outline>();
            outline.effectColor = ClearingPalette.Ink;
            outline.effectDistance = new Vector2(1, -1);
            return label;
        }

        internal void Refresh(PlayerStats stats, ClearingRun run, bool paused, bool muted, bool nearBeacon, string feedback,
            ClearingProgress progress = null, string rewardId = "", string progressNotice = "", string supplyHint = "", string bossHint = "",
            string bossNotice = "")
        {
            // Keep two edge columns from intersecting on square/narrow viewports.
            float width = ((RectTransform)root).rect.width;
            if (width < 1f) width = 960f; // The canvas can be pending its first layout pass.
            float height = ((RectTransform)root).rect.height;
            if (height < 1f) height = 540f;
            float column = Mathf.Max(100f, width * .5f - 28f);
            bool narrow = width < 810f;
            resources.rectTransform.sizeDelta = new Vector2(Mathf.Min(260f, column), progress == null ? 35f : 77f);
            objective.rectTransform.sizeDelta = new Vector2(Mathf.Min(350f, column), 67f);
            skills.rectTransform.sizeDelta = new Vector2(Mathf.Min(400f, column), narrow ? 66f : 43f);
            help.rectTransform.sizeDelta = new Vector2(Mathf.Min(350f, column), 66f);
            help.fontSize = narrow ? 11 : 14;
            message.rectTransform.sizeDelta = help.rectTransform.sizeDelta;
            message.fontSize = help.fontSize;
            bool showShop = progress != null && run.IsComplete && !paused;
            terminal.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, Mathf.Min(600f, width - 28f)),
                Mathf.Min(showShop ? 350f : 160f, Mathf.Max(1f, height - 28f)));
            terminal.resizeTextForBestFit = showShop;
            terminal.resizeTextMinSize = 10;
            terminal.resizeTextMaxSize = 18;
            resources.text = string.Format("HP {0:0.#}/{1:0.#}    MP {2:0.#}/{3:0.#}", stats.Health, stats.MaxHealth, stats.Mana, stats.MaxMana);
            if (progress != null)
            {
                resources.text += "\nBank " + progress.Data.BankCoins + " coins\nVitality " + progress.Data.VitalityRank + "/3  Focus " + progress.Data.FocusRank + "/3";
                if (!progress.CanWrite) resources.text += "\nSaving unavailable";
                else if (progress.LoadKind == ProgressLoadKind.Recovered) resources.text += "\nEarlier backup recovered";
            }
            objective.text = run.IsComplete ? "CLEARING RESTORED" : run.GateUnlocked ? "Gate open: approach the north beacon" :
                run.RequiresBoss && run.Boss.IsAwake ? string.Format("MOSS GUARDIAN  HP {0:0.#}/{1:0.#}\nPhase {2}: {3}",
                    run.Boss.Health, ClearingBossState.MaxHealth, run.Boss.Enraged ? 2 : 1,
                    paused ? "PAUSED" : run.IsDead ? "DEFEATED" : BossInstruction(run.Boss)) :
                run.RequiresBoss && run.DefeatedCount == ClearingRun.SentinelCount ? "SENTINELS CLEARED - SEAL HOLDS\nAwaken the Guardian at the central altar" :
                string.Format("RESTORE THE BEACON\nSentinels defeated: {0}/{1}", run.DefeatedCount, ClearingRun.SentinelCount);
            string light = run.LightCooldownRemaining > 0 ? run.LightCooldownRemaining.ToString("0.0") + "s" : "ready";
            string burst = run.BurstCooldownRemaining > 0 ? run.BurstCooldownRemaining.ToString("0.0") + "s" :
                stats.Mana < ClearingRun.BurstManaCost ? "need MP" : "ready";
            if (run.PlayerAttackActive) light = burst = "striking";
            bool playable = !paused && !run.IsDead && !run.IsComplete;
            skills.text = playable ? "J / Space: light [" + light + "]\nK: burst 6 MP [" + burst + "]" : "";
            string interaction = nearBeacon && run.GateUnlocked ? "E - restore the beacon" :
                !string.IsNullOrEmpty(bossHint) ? bossHint : supplyHint;
            message.text = !playable ? "" : !string.IsNullOrEmpty(interaction) ? interaction : feedback;
            // Phase changes have their own simulation-time lifetime. Keep the current
            // interaction below the notice instead of letting proximity erase it.
            if (playable && !string.IsNullOrEmpty(bossNotice))
                message.text = bossNotice + (string.IsNullOrEmpty(interaction) ? "" : "\n" + interaction);
            help.text = !playable || !string.IsNullOrEmpty(message.text) ? "" : MovementGuide(narrow);
            terminal.text = paused ? "PAUSED\nEsc - resume" : run.IsDead ? "DEFEATED\nR - retry the clearing" :
                run.IsComplete ? "BEACON RESTORED\n" + run.RewardCoins + " coins earned this run\nR - play again" : "";
            if (showShop)
            {
                // The result already contains bank/ranks. On short/ultrawide
                // canvases a large result shares the corners, so hide duplicates.
                resources.text = "";
                objective.text = "";
                bool banked = completionBankedOverride || progress.IsCompletionBanked(rewardId);
                terminal.text = "BEACON RESTORED\n" + run.RewardCoins + " run coins | Bank " + progress.Data.BankCoins + " coins\n" +
                    (banked ? "Run reward banked\n" : "Reward NOT SAVED. E - retry saving\n") +
                    UpgradeLine(progress, ClearingUpgrade.Vitality, banked ? "1 Vitality" : "Vitality (bank first)", "+2 HP") + "\n" +
                    UpgradeLine(progress, ClearingUpgrade.Focus, banked ? "2 Focus" : "Focus (bank first)", "+2 MP") + "\n" +
                    "Bonuses apply next run. R - play again" +
                    (banked ? "" : "\nR loses this unbanked run reward") +
                    (string.IsNullOrEmpty(progressNotice) ? "" : "\n" + progressNotice);
                if (forestTrailAvailable)
                    terminal.text += banked ? "\nF - enter Thornwood trail" : "\nThornwood locked - save the beacon reward first";
            }
            if (muted && !paused && !run.IsDead && !run.IsComplete) objective.text += "\nSound muted";
        }

        internal void RefreshThornwood(PlayerStats stats, ThornwoodRun expedition, bool paused, bool muted,
            string feedback, ClearingProgress progress, string rewardId, string progressNotice,
            string interactionHint, bool completionBanked = false)
        {
            // Reuse the existing edge layout, but do not borrow the clearing's
            // full-screen shop: returning is always a physical trail interaction.
            Refresh(stats, expedition.Combat, paused, muted, false, feedback);
            resources.text = string.Format("HP {0:0.#}/{1:0.#}    MP {2:0.#}/{3:0.#}",
                stats.Health, stats.MaxHealth, stats.Mana, stats.MaxMana) +
                (progress == null ? "" : "\nBank " + progress.Data.BankCoins + " coins");
            bool dead = expedition.Combat.IsDead;
            bool complete = expedition.Combat.IsComplete;
            objective.text = "THORNWOOD\nSeeds " + expedition.SeedsCollected + "/3 | Stalkers " +
                expedition.Combat.DefeatedCount + "/3";
            if (complete) objective.text += completionBanked ? "\nCache reward saved" : "\nCache reward NOT SAVED";
            else if (expedition.CacheReady) objective.text += "\nE at the north cache";
            else objective.text += "\nExplore all three glades";
            bool playable = !paused && !dead;
            if (playable && complete) skills.text = "Cache complete - return via the south trail\nR - new expedition" +
                (completionBanked ? "" : " (loses unbanked reward)");
            message.text = !playable ? "" : !string.IsNullOrEmpty(interactionHint) ? interactionHint : feedback;
            if (playable && complete && !string.IsNullOrEmpty(progressNotice) && string.IsNullOrEmpty(interactionHint))
                message.text = progressNotice;
            help.text = playable && string.IsNullOrEmpty(message.text) ?
                "WASD / arrows: move   E: interact\nSouth trail: return any time\nStalker: sidestep its locked pounce\nEsc: pause   M: mute" : "";
            terminal.text = paused ? "PAUSED\nEsc - resume" : dead ? "DEFEATED IN THORNWOOD\nR - retry the expedition" : "";
            if (muted) objective.text += "\nSound muted";
        }

        private static string BossInstruction(ClearingBossState boss)
        {
            if (boss.AttackPhase == SentinelAttackPhase.Recovery) return "RECOVERY - strike now";
            SentinelAttackKind kind = boss.AttackPhase == SentinelAttackPhase.Ready ? boss.NextAttackKind : boss.AttackKind;
            if (boss.AttackPhase == SentinelAttackPhase.Ready)
                return kind == SentinelAttackKind.Sigil ? "preparing a ground rune" : "preparing a lance";
            if (boss.AttackPhase == SentinelAttackPhase.Active)
                return kind == SentinelAttackKind.Sigil ? "rune active - keep clear" : "lane active - keep clear";
            return kind == SentinelAttackKind.Sigil ? "leave the marked ground" : "sidestep the locked lane";
        }

        private static string MovementGuide(bool narrow)
        {
            // Keep tactical hints in the existing edge column; feedback takes
            // priority and terminal states suppress the entire playable guide.
            return narrow ? "Move: WASD / arrows\nE: interact   Esc: pause\nM: mute   R: retry after run\nLancer: sidestep lane\nSeer: leave mark" :
                "WASD / arrows: move   E: interact\nEsc: pause   M: mute   R: retry after run\nLancer: sidestep lane\nSeer: leave mark";
        }

        private static string UpgradeLine(ClearingProgress progress, ClearingUpgrade upgrade, string title, string bonus)
        {
            int rank = upgrade == ClearingUpgrade.Vitality ? progress.Data.VitalityRank : progress.Data.FocusRank;
            return title + " " + rank + "/3: " + (rank == ClearingProgressData.MaxUpgradeRank ? "MAX" :
                bonus + ", cost " + progress.Price(upgrade) + " coins");
        }
    }
}
