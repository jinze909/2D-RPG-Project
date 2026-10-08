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
            help = Label("Movement keys", new Vector2(1, 0), new Vector2(-14, 12), new Vector2(350, 43), TextAnchor.LowerRight, 14);
            help.text = "WASD / arrows: move   E: beacon\nEsc: pause   M: mute   R: retry after run";
            message = Label("Feedback", new Vector2(.5f, 0), new Vector2(0, 63), new Vector2(610, 23), TextAnchor.MiddleCenter, 16);
            terminal = Label("Run result", new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 135), TextAnchor.MiddleCenter, 24);
            terminal.color = new Color(1f, .91f, .69f);
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
            label.color = new Color(.95f, .97f, .87f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            Outline outline = item.GetComponent<Outline>();
            outline.effectColor = new Color(.05f, .09f, .09f, .95f);
            outline.effectDistance = new Vector2(1, -1);
            return label;
        }

        internal void Refresh(PlayerStats stats, ClearingRun run, bool paused, bool muted, bool nearBeacon, string feedback)
        {
            // Keep two edge columns from intersecting on square/narrow viewports.
            float width = ((RectTransform)root).rect.width;
            if (width < 1f) width = 960f; // The canvas can be pending its first layout pass.
            float column = Mathf.Max(100f, width * .5f - 28f);
            bool narrow = width < 810f;
            resources.rectTransform.sizeDelta = new Vector2(Mathf.Min(260f, column), 35f);
            objective.rectTransform.sizeDelta = new Vector2(Mathf.Min(350f, column), 67f);
            skills.rectTransform.sizeDelta = new Vector2(Mathf.Min(400f, column), narrow ? 66f : 43f);
            help.rectTransform.sizeDelta = new Vector2(Mathf.Min(350f, column), narrow ? 66f : 43f);
            message.rectTransform.sizeDelta = new Vector2(Mathf.Max(100f, Mathf.Min(610f, width - 28f)), 43f);
            message.rectTransform.anchoredPosition = new Vector2(0f, narrow ? 85f : 63f);
            terminal.rectTransform.sizeDelta = new Vector2(Mathf.Max(100f, Mathf.Min(600f, width - 28f)), 160f);
            resources.text = string.Format("HP {0:0.#}/{1:0.#}    MP {2:0.#}/{3:0.#}", stats.Health, stats.MaxHealth, stats.Mana, stats.MaxMana);
            objective.text = run.IsComplete ? "CLEARING RESTORED" : run.GateUnlocked ? "Gate open: approach the north beacon" :
                string.Format("RESTORE THE BEACON\nSentinels defeated: {0}/{1}", run.DefeatedCount, ClearingRun.SentinelCount);
            string light = run.LightCooldownRemaining > 0 ? run.LightCooldownRemaining.ToString("0.0") + "s" : "ready";
            string burst = run.BurstCooldownRemaining > 0 ? run.BurstCooldownRemaining.ToString("0.0") + "s" :
                stats.Mana < ClearingRun.BurstManaCost ? "need MP" : "ready";
            if (run.PlayerAttackActive) light = burst = "striking";
            skills.text = "J / Space: light [" + light + "]\nK: mana burst (6 MP) [" + burst + "]";
            message.text = nearBeacon && run.GateUnlocked && !run.IsComplete ? "E - restore the beacon" : feedback;
            terminal.text = paused ? "PAUSED\nEsc - resume" : run.IsDead ? "DEFEATED\nR - retry the clearing" :
                run.IsComplete ? "BEACON RESTORED\n" + run.RewardCoins + " coins earned this run\nR - play again" : "";
            if (muted && !paused && !run.IsDead && !run.IsComplete) objective.text += "\nSound muted";
        }
    }
}
