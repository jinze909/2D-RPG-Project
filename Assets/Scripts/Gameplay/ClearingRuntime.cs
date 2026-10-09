using System;
using System.IO;
using UnityEngine;

namespace Rpg.Gameplay
{
    /// <summary>
    /// A complete, small combat/objective scene using the original Viola Animator.
    /// Locomotion never changes to an invented attack pose: attack footprints are
    /// separate world objects and share the actual directional contact window.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ClearingRuntime : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private Camera sceneCamera;
        private readonly Vector2[] enemySpawns = { new Vector2(-4f, -.3f), new Vector2(4f, -.3f), new Vector2(0f, 1.15f) };
        private readonly EnemyView[] enemies = new EnemyView[ClearingRun.SentinelCount];
        private ClearingRun run;
        private ClearingProgress progress;
        private string rewardId;
        private float baseMaxHealth;
        private float baseMaxMana;
        private string progressNotice;
        private ClearingVisuals visuals;
        private ClearingHud hud;
        private ClearingAudio sound;
        private ClearingDamageNumbers damageNumbers;
        private readonly ClearingSupplies supplies = new ClearingSupplies();
        private readonly string[] supplyNotices = new string[ClearingSupplies.Count];
        private ClearingSupplyVisuals supplyVisuals;
        private PlayerHealth health;
        private PlayerMana mana;
        private PlayerMovement movement;
        private Rigidbody2D playerBody;
        private Animator animator;
        private SpriteRenderer playerRenderer;
        private SpriteRenderer strike;
        private long strikeId;
        private Vector2 strikeDirection;
        private PlayerAttackKind strikeKind;
        private bool paused;
        private float timeScaleBeforePause;
        private bool gateWasOpened;
        private long lastHitAudioActionId;
        private long lastKillAudioActionId;
        private string feedback = "Defeat the sentinels. Sidestep spear lanes; leave rune marks.";
        private float feedbackUntil = 7f;
        private float nextHudAt;

        private sealed class EnemyView
        {
            internal GameObject Object;
            internal Rigidbody2D Body;
            internal SpriteRenderer Eye;
            internal SpriteRenderer Health;
            internal Transform Warning;
            internal SpriteRenderer[] Renderers;
            internal int[] LocalOrders;
            internal bool[] ActorLayers;
            internal Vector2 AttackCenter;
            internal SentinelAttackKind Kind;
            internal SentinelFootprint Footprint;
            internal float HurtUntil;
            internal SpriteRenderer WarningFill;
            internal SpriteRenderer WarningCrossA;
            internal SpriteRenderer WarningCrossB;
            internal Transform Impact;
            internal SpriteRenderer[] ImpactSprites;
            internal double ImpactStartedAt;
            internal double ImpactUntil;
            internal bool ImpactWasKill;
            internal Color ImpactColor;
        }

        private void Start()
        {
            if (player == null || player.Stats == null || sceneCamera == null)
            {
                Debug.LogError("CombatClearing requires its serialized Player and Camera references.", this);
                enabled = false;
                return;
            }
            health = player.GetComponent<PlayerHealth>();
            mana = player.GetComponent<PlayerMana>();
            movement = player.GetComponent<PlayerMovement>();
            playerBody = player.GetComponent<Rigidbody2D>();
            animator = player.GetComponent<Animator>();
            playerRenderer = player.GetComponent<SpriteRenderer>();
            if (health == null || mana == null || movement == null || playerBody == null || animator == null || playerRenderer == null)
            {
                Debug.LogError("CombatClearing player is missing a required control/resource/visual component.", this);
                enabled = false;
                return;
            }
            health.DebugDamageEnabled = false;
            player.gameObject.layer = 2;
            if (player.GetComponent<Collider2D>() == null)
            {
                CapsuleCollider2D collider = player.gameObject.AddComponent<CapsuleCollider2D>();
                collider.size = new Vector2(.36f, .28f);
                collider.offset = ClearingVisuals.PlayerFootOffset;
                collider.direction = CapsuleDirection2D.Horizontal;
            }
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            run = new ClearingRun();
            visuals = new ClearingVisuals(transform);
            visuals.BuildWorld();
            supplyVisuals = new ClearingSupplyVisuals(transform, visuals);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = new EnemyView();
                enemy.Object = visuals.CreateSentinel(i, enemySpawns[i], out enemy.Eye, out enemy.Health, out enemy.Warning);
                enemy.Object.AddComponent<CircleCollider2D>().radius = .23f;
                enemy.Body = enemy.Object.AddComponent<Rigidbody2D>();
                enemy.Body.gravityScale = 0f;
                enemy.Body.mass = 20f;
                enemy.Body.constraints = RigidbodyConstraints2D.FreezeRotation;
                enemy.Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                enemy.Body.interpolation = RigidbodyInterpolation2D.Interpolate;
                ConfigureSentinelRole(enemy, i);
                PreparePresentation(enemy, i);
                // Cache once; keep local body/head/UI order inside one actor's pixel-depth slot.
                enemy.Renderers = enemy.Object.GetComponentsInChildren<SpriteRenderer>();
                enemy.LocalOrders = new int[enemy.Renderers.Length];
                enemy.ActorLayers = new bool[enemy.Renderers.Length];
                for (int j = 0; j < enemy.Renderers.Length; j++)
                {
                    enemy.LocalOrders[j] = enemy.Renderers[j].sortingOrder;
                    enemy.ActorLayers[j] = enemy.Renderers[j].sortingLayerName == "Player";
                }
                enemies[i] = enemy;
            }
            strike = visuals.Block("Directional player strike", transform, Vector2.zero, Vector2.one,
                ClearingPalette.WithAlpha(ClearingPalette.AmberBright, .5f), 30000, true);
            strike.gameObject.SetActive(false);
            hud = new ClearingHud(transform);
            sound = new ClearingAudio(gameObject);
            damageNumbers = new ClearingDamageNumbers(transform);
            InitializeProgress(CreateProgress());
            ResetRun();
        }

        private sealed class UnavailableProgressStore : IClearingProgressStore
        {
            public ProgressLoadResult Load()
            {
                return new ProgressLoadResult(ClearingProgressData.Fresh, ProgressLoadKind.Unavailable, false);
            }
            public bool TrySave(ClearingProgressData expected, ClearingProgressData next) { return false; }
        }

        private static ClearingProgress CreateProgress()
        {
            // An empty platform path must never turn into a relative project save.
            if (!string.IsNullOrWhiteSpace(Application.persistentDataPath))
            {
                try
                {
                    return new ClearingProgress(new FileClearingProgressStore(
                        Path.Combine(Application.persistentDataPath, "ClearingProgress")));
                }
                catch (ArgumentException) { }
                catch (IOException) { }
                catch (NotSupportedException) { }
                catch (System.Security.SecurityException) { }
            }
            return new ClearingProgress(new UnavailableProgressStore());
        }

        private void InitializeProgress(ClearingProgress value)
        {
            progress = value;
            // Player.Awake already cloned the asset before this runtime's Start.
            baseMaxHealth = player.Stats.MaxHealth;
            baseMaxMana = player.Stats.MaxMana;
            progressNotice = LoadNotice();
        }

        private string LoadNotice()
        {
            if (progress == null || !progress.CanWrite) return "Save unavailable; existing files protected.";
            return progress.LoadKind == ProgressLoadKind.Recovered ? "Recovered earlier progress; latest transaction may be lost." : "";
        }

        private Vector2 PlayerPoint { get { return playerBody.position + ClearingVisuals.PlayerFootOffset; } }
        private bool NearBeacon { get { return Vector2.Distance(PlayerPoint, ClearingVisuals.BeaconPosition) <= .95f; } }

        private void Update()
        {
            if (run == null) return;
            SynchronizeDeath();
            if (Input.GetKeyDown(KeyCode.M)) sound.ToggleMute();
            // Snapshot terminal admission so R cannot also attack/interact this frame.
            if (run.IsDead || run.IsComplete)
            {
                if (run.IsComplete && !paused)
                {
                    if (Input.GetKeyDown(KeyCode.E)) BankCompletion();
                    if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) BuyUpgrade(ClearingUpgrade.Vitality);
                    else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) BuyUpgrade(ClearingUpgrade.Focus);
                }
                if (Input.GetKeyDown(KeyCode.R)) ResetRun();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
                if (!paused)
                {
                    DiscoverSupplies();
                    if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space)) StartAttack(PlayerAttackKind.Light);
                    if (Input.GetKeyDown(KeyCode.K)) StartAttack(PlayerAttackKind.Burst);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        // Objective interaction keeps priority over optional supplies.
                        if (NearBeacon)
                        {
                            if (run.TryCompleteObjective())
                            {
                                StopActors();
                                if (BankCompletion() != ProgressActionResult.Saved) sound.Reward();
                                Feedback("The beacon is restored. The clearing is safe.", 5f);
                            }
                            else Feedback("Defeat all three sentinels to break the seal.", 2f);
                        }
                        else
                        {
                            int nearby = NearbySupply();
                            if (nearby >= 0) TryUseSupply(nearby);
                            else Feedback(run.GateUnlocked ? "Move closer to the north beacon." :
                                "Defeat all three sentinels to break the seal.", 2f);
                        }
                    }
                }
            }
            RefreshViews();
            // Resource strings and layout rebuilds do not need to run every rendered frame.
            if (Time.unscaledTime >= nextHudAt)
            {
                nextHudAt = Time.unscaledTime + .05f;
                hud.Refresh(player.Stats, run, paused, sound.Muted, NearBeacon,
                    Time.unscaledTime < feedbackUntil ? feedback : "", progress, rewardId, progressNotice, SupplyHint());
            }
        }

        private static Vector2 SupplyPoint(int index)
        {
            return new Vector2(ClearingSupplies.PositionX(index), ClearingSupplies.PositionY(index));
        }

        private bool SuppliesPlayable
        {
            get
            {
                return run != null && !paused && !run.IsDead && !run.IsComplete && player != null &&
                    player.Stats != null && player.Stats.Health > 0f &&
                    !float.IsNaN(player.Stats.Health) && !float.IsInfinity(player.Stats.Health);
            }
        }

        private void DiscoverSupplies()
        {
            if (!SuppliesPlayable) return;
            Vector2 feet = PlayerPoint;
            for (int index = 0; index < ClearingSupplies.Count; index++)
            {
                if (supplies.IsDiscovered(index)) continue;
                Vector2 point = SupplyPoint(index);
                float distance = Vector2.Distance(feet, point);
                if (distance <= ClearingSupplies.DiscoveryRadius &&
                    supplies.TryDiscover(index, distance, ClearLine(feet, point), true))
                    Feedback(index == 0 ? "Herbs found. Approach and press E when you need HP." :
                        "Rune found. Approach and press E when you need MP.", 3f);
            }
        }

        private int NearbySupply()
        {
            if (!SuppliesPlayable) return -1;
            Vector2 feet = PlayerPoint;
            int nearest = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < ClearingSupplies.Count; index++)
            {
                if (!supplies.IsDiscovered(index)) continue;
                Vector2 point = SupplyPoint(index);
                float distance = Vector2.Distance(feet, point);
                if (distance <= ClearingSupplies.UseRadius && distance < nearestDistance &&
                    ClearLine(feet, point))
                {
                    nearest = index;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private void TryUseSupply(int index)
        {
            if (!SuppliesPlayable || index < 0 || index >= ClearingSupplies.Count) return;
            Vector2 point = SupplyPoint(index);
            PlayerStats stats = player.Stats;
            float gain;
            SupplyClaimResult result = supplies.TryClaim(index, Vector2.Distance(PlayerPoint, point),
                ClearLine(PlayerPoint, point), true, stats.Health, stats.MaxHealth, stats.Mana, stats.MaxMana, out gain);
            if (result == SupplyClaimResult.Restored)
            {
                // The same actor-owned snapshot is used by damage, mana costs and HUD.
                // Admission reports the clamped, representable gain; apply it once.
                if (index == 0) stats.Health = Mathf.Min(stats.MaxHealth, stats.Health + gain);
                else stats.Mana = Mathf.Min(stats.MaxMana, stats.Mana + gain);
                if (supplyVisuals != null) supplyVisuals.ShowClaim(index, run.Time);
                sound.Reward();
                string amount = gain < .01f ? "<0.01" :
                    gain.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                supplyNotices[index] = (index == 0 ? "Herbs: +" : "Rune: +") + amount + (index == 0 ? " HP." : " MP.");
                Feedback(supplyNotices[index], 3f);
            }
            else if (result == SupplyClaimResult.Full)
                Feedback(index == 0 ? "HP full; herbs saved for later." : "MP full; rune saved for later.", 2f);
            else if (result == SupplyClaimResult.Spent)
                Feedback(index == 0 ? "Herbs already used this run." : "Rune already used this run.", 2f);
            else if (result == SupplyClaimResult.Invalid)
                Feedback("Supply unavailable with invalid resources; charge preserved.", 2f);
            else if (result == SupplyClaimResult.Blocked || result == SupplyClaimResult.OutOfRange)
                Feedback("Approach the supply with a clear path.", 2f);
            nextHudAt = 0f;
        }

        private string SupplyHint()
        {
            int index = NearbySupply();
            if (index < 0) return "";
            if (supplies.IsClaimed(index))
                return string.IsNullOrEmpty(supplyNotices[index]) ?
                    (index == 0 ? "Herbs used this run" : "Rune used this run") :
                    supplyNotices[index] + " Used this run.";
            PlayerStats stats = player.Stats;
            bool valid = stats.MaxHealth > 0f && stats.Health <= stats.MaxHealth &&
                stats.MaxMana >= 0f && stats.Mana >= 0f && stats.Mana <= stats.MaxMana &&
                !float.IsNaN(stats.MaxHealth) && !float.IsInfinity(stats.MaxHealth) &&
                !float.IsNaN(stats.Mana) && !float.IsInfinity(stats.Mana) &&
                !float.IsNaN(stats.MaxMana) && !float.IsInfinity(stats.MaxMana);
            if (!valid) return "Supply unavailable; charge preserved";
            if (index == 0 && stats.Health >= stats.MaxHealth) return "HP full - herbs saved for later";
            if (index == 1 && stats.Mana >= stats.MaxMana) return "MP full - rune saved for later";
            return index == 0 ? "E - herbs: restore up to 4 HP" : "E - rune: restore up to 6 MP";
        }

        private ProgressActionResult BankCompletion()
        {
            if (paused || run.IsDead || !run.IsComplete) return ProgressActionResult.RunIncomplete;
            ProgressActionResult result = progress == null ? ProgressActionResult.Unavailable : progress.BankCompletion(run, rewardId);
            if (result == ProgressActionResult.Saved)
            {
                progressNotice = "30 coins banked. Upgrades apply next run.";
                sound.Reward();
            }
            else if (result != ProgressActionResult.AlreadyBanked)
                progressNotice = result == ProgressActionResult.CapacityReached ?
                    "Progress limit reached; reward unbanked. R loses these run coins." :
                    result == ProgressActionResult.Unavailable ?
                    "Save unavailable; existing files protected. Reward unbanked." :
                    "Reward not saved; unbanked. E - retry; R loses these run coins.";
            nextHudAt = 0f;
            return result;
        }

        private void BuyUpgrade(ClearingUpgrade upgrade)
        {
            if (paused || run.IsDead || !run.IsComplete) return;
            if (progress == null || !progress.IsCompletionBanked(rewardId))
            {
                progressNotice = progress == null || !progress.CanWrite ?
                    "Save unavailable; existing files protected. Reward unbanked." :
                    progress.Data.ClearedRuns == ClearingProgressData.MaxClearedRuns ?
                    "Progress limit reached; reward unbanked. R loses these run coins." :
                    "Reward unbanked. Bank this run first. E - retry saving.";
                nextHudAt = 0f;
                return;
            }
            ProgressActionResult result = progress.Buy(upgrade);
            progressNotice = result == ProgressActionResult.Saved ? "Upgrade saved. Bonus applies next run." :
                result == ProgressActionResult.InsufficientCoins ? "Not enough banked coins. Complete another run." :
                result == ProgressActionResult.MaxRank ? "This upgrade is already at maximum rank." :
                "Upgrade not saved; coins unchanged. Press the same key to retry.";
            if (result == ProgressActionResult.Saved) sound.Reward();
            nextHudAt = 0f;
        }

        private void StartAttack(PlayerAttackKind kind)
        {
            if (player.Stats.Health <= 0f || float.IsNaN(player.Stats.Health) || float.IsInfinity(player.Stats.Health)) return;
            float availableMana = player.Stats.Mana;
            if (!run.CanPlayerAttack(kind, availableMana))
            {
                if (kind == PlayerAttackKind.Burst && availableMana < ClearingRun.BurstManaCost)
                    Feedback("Not enough mana. Light attacks cost no mana; mana regenerates.", 2f);
                return;
            }
            // Admission and spend are synchronous and share the checked pre-spend balance.
            if (kind == PlayerAttackKind.Burst && !mana.TryUseMana(ClearingRun.BurstManaCost)) return;
            if (!run.BeginPlayerAttack(kind, availableMana, out strikeId)) return;
            strikeKind = kind;
            strikeDirection = movement.FacingDirection;
            if (strikeDirection.sqrMagnitude < .01f) strikeDirection = Vector2.down;
            strikeDirection.Normalize();
            sound.Attack(kind == PlayerAttackKind.Burst);
        }

        private void ConfigureSentinelRole(EnemyView enemy, int index)
        {
            enemy.Kind = SentinelTactics.RoleForIndex(index);
        }

        private void PreparePresentation(EnemyView enemy, int index)
        {
            // All feedback renderers are allocated once. The existing four-border
            // warning uses the role-sized fixed contact outline throughout an attack.
            enemy.WarningFill = visuals.Block("Telegraph charge", enemy.Warning, Vector2.zero,
                Vector2.one, ClearingPalette.WithAlpha(ClearingPalette.Amber, .15f), -3);
            enemy.WarningCrossA = visuals.Block("Active cross A", enemy.Warning, Vector2.zero,
                new Vector2(1.3f, 2f / 30f), ClearingPalette.Cream, -1);
            enemy.WarningCrossB = visuals.Block("Active cross B", enemy.Warning, Vector2.zero,
                new Vector2(1.3f, 2f / 30f), ClearingPalette.Cream, -1);
            enemy.WarningCrossA.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            enemy.WarningCrossB.transform.localRotation = Quaternion.Euler(0f, 0f, 135f);
            enemy.WarningCrossA.gameObject.SetActive(false);
            enemy.WarningCrossB.gameObject.SetActive(false);

            GameObject impact = new GameObject("Reusable sentinel impact " + (index + 1));
            impact.transform.SetParent(transform, false);
            enemy.Impact = impact.transform;
            enemy.ImpactSprites = new SpriteRenderer[3];
            enemy.ImpactSprites[0] = visuals.Block("Impact center", enemy.Impact, Vector2.zero,
                new Vector2(4f / 30f, 4f / 30f), ClearingPalette.Cream, 30003, true);
            enemy.ImpactSprites[1] = visuals.Block("Impact horizontal", enemy.Impact, Vector2.zero,
                new Vector2(20f / 30f, 2f / 30f), ClearingPalette.Cream, 30002, true);
            enemy.ImpactSprites[2] = visuals.Block("Impact vertical", enemy.Impact, Vector2.zero,
                new Vector2(2f / 30f, 20f / 30f), ClearingPalette.Cream, 30002, true);
            impact.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (run == null) return;
            SynchronizeDeath();
            if (paused || run.IsDead || run.IsComplete) return;
            run.Advance(Time.fixedDeltaTime);
            DiscoverSupplies();
            player.Stats.Mana = Mathf.Min(player.Stats.MaxMana,
                player.Stats.Mana + ClearingRun.ManaRegenerationRate * Time.fixedDeltaTime);
            bool contactedSentinel = false;
            bool defeatedSentinel = false;
            // Resolve this player's admitted action against every target before
            // enemy contacts can end the run. A lethal enemy's array index must
            // not decide how many targets the same burst reaches on this tick.
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                SentinelState state = run.GetSentinel(i);
                if (state.IsDead) continue;
                float healthBefore = state.Health;
                Vector2 difference = enemy.Body.position - PlayerPoint;
                if (run.PlayerAttackActive && InStrike(difference) && ClearLine(PlayerPoint, enemy.Body.position) && run.TryHitSentinel(strikeId, i))
                {
                    contactedSentinel = true;
                    defeatedSentinel |= state.IsDead;
                    enemy.HurtUntil = (float)run.Time + .12f;
                    ShowImpact(enemy, state.IsDead, strikeKind == PlayerAttackKind.Burst);
                    if (damageNumbers != null) damageNumbers.Show(i, healthBefore - state.Health,
                        enemy.Body.position + new Vector2(0f, 1.7f),
                        strikeKind == PlayerAttackKind.Burst ? ClearingPalette.CyanBright : ClearingPalette.Cream, run.Time);
                    if (state.IsDead)
                    {
                        enemy.Footprint = null;
                        enemy.Body.velocity = Vector2.zero;
                        enemy.Warning.gameObject.SetActive(false);
                        enemy.Object.SetActive(false);
                        Feedback("Sentinel defeated. " + run.DefeatedCount + "/3 seals broken. +" +
                            ClearingRun.CoinsPerSentinel + " coins.", 2f);
                        continue;
                    }
                }
            }
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                SentinelState state = run.GetSentinel(i);
                if (state.IsDead) continue;
                Vector2 target = PlayerPoint;
                Vector2 origin = enemy.Body.position;
                Vector2 toPlayer = target - origin;
                bool clear = ClearLine(origin, target);
                SentinelDecision decision = SentinelDecision.Hold;
                if (state.AttackPhase == SentinelAttackPhase.Ready)
                {
                    enemy.Footprint = null;
                    bool canRetreat = toPlayer.sqrMagnitude > .0001f;
                    if (enemy.Kind == SentinelAttackKind.Sigil && toPlayer.magnitude < 2.2f && canRetreat)
                        canRetreat = ClearLine(origin, origin - toPlayer.normalized * .55f);
                    decision = SentinelTactics.ChooseAction(enemy.Kind, toPlayer.magnitude, clear, canRetreat);
                    if (decision == SentinelDecision.Attack)
                    {
                        SentinelFootprint footprint = SentinelTactics.CreateFootprint(enemy.Kind,
                            origin.x, origin.y, target.x, target.y);
                        if (footprint != null && run.BeginSentinelAttack(i, enemy.Kind))
                        {
                            enemy.Footprint = footprint;
                            enemy.AttackCenter = new Vector2(footprint.CenterX, footprint.CenterY);
                        }
                    }
                }
                // Never slide through telegraph/active/recovery or after admission.
                if (state.AttackPhase == SentinelAttackPhase.Ready &&
                    (decision == SentinelDecision.Approach || decision == SentinelDecision.Retreat))
                {
                    Vector2 direction = decision == SentinelDecision.Retreat ? -toPlayer.normalized : toPlayer.normalized;
                    enemy.Body.MovePosition(origin + direction * (1.1f * Time.fixedDeltaTime));
                }
                else enemy.Body.velocity = Vector2.zero;
                // Warning and contact use the same immutable snapshot, including
                // the original LOS origin for a remotely placed sigil.
                SentinelFootprint snapshot = enemy.Footprint;
                Vector2 contact = target - enemy.AttackCenter;
                bool inside = snapshot != null ? snapshot.Contains(target.x, target.y) :
                    Mathf.Abs(contact.x) <= .95f && Mathf.Abs(contact.y) <= .95f;
                Vector2 lineOrigin = snapshot != null ? new Vector2(snapshot.OriginX, snapshot.OriginY) : enemy.AttackCenter;
                if (state.AttackPhase == SentinelAttackPhase.Active && inside &&
                    ClearLine(lineOrigin, target) && run.TryResolveSentinelHit(i))
                {
                    float healthBefore = player.Stats.Health;
                    health.TakeDamage(ClearingRun.SentinelDamage);
                    if (damageNumbers != null) damageNumbers.Show(ClearingRun.SentinelCount,
                        healthBefore - player.Stats.Health, PlayerPoint + new Vector2(0f, 2.35f), ClearingPalette.Danger, run.Time);
                    sound.Hurt();
                    if (player.Stats.Health <= 0f)
                    {
                        run.NotifyPlayerDeath();
                        StopActors();
                        break;
                    }
                }
            }
            // Several targets in one burst share a cue. A later kill can upgrade an
            // earlier contact cue once; repeated contacts cannot create audio spam.
            if (contactedSentinel && !run.IsDead)
            {
                if (defeatedSentinel && lastKillAudioActionId != strikeId)
                {
                    sound.Hit(true, strikeKind == PlayerAttackKind.Burst);
                    lastKillAudioActionId = strikeId;
                    lastHitAudioActionId = strikeId;
                }
                else if (lastHitAudioActionId != strikeId)
                {
                    sound.Hit(false, strikeKind == PlayerAttackKind.Burst);
                    lastHitAudioActionId = strikeId;
                }
            }
            if (run.GateUnlocked && !gateWasOpened)
            {
                gateWasOpened = true;
                visuals.Gate.SetActive(false);
                sound.Reward();
                Feedback("The seal is broken. Go north and press E at the beacon.", 7f);
            }
        }

        private bool InStrike(Vector2 difference)
        {
            float forward = Vector2.Dot(difference, strikeDirection);
            float side = Mathf.Abs(difference.x * strikeDirection.y - difference.y * strikeDirection.x);
            return strikeKind == PlayerAttackKind.Light ? forward >= .15f && forward <= 1.4f && side <= .55f :
                forward >= .1f && forward <= 2.9f && side <= 1f;
        }

        private static bool ClearLine(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, 1 << 0).collider == null;
        }

        private void RefreshViews()
        {
            if (damageNumbers != null) damageNumbers.Refresh(run.Time);
            if (supplyVisuals != null) supplyVisuals.Refresh(supplies, run.Time);
            bool active = run.PlayerAttackActive;
            strike.gameObject.SetActive(active);
            if (active)
            {
                float center = strikeKind == PlayerAttackKind.Light ? .775f : 1.5f;
                Vector2 location = PlayerPoint + strikeDirection * center;
                strike.transform.position = new Vector3(location.x, location.y, 0f);
                strike.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(strikeDirection.y, strikeDirection.x) * Mathf.Rad2Deg);
                strike.transform.localScale = strikeKind == PlayerAttackKind.Light ? new Vector3(1.25f, 1.1f, 1) : new Vector3(2.8f, 2f, 1);
                strike.color = ClearingPalette.WithAlpha(strikeKind == PlayerAttackKind.Light ?
                    ClearingPalette.AmberBright : ClearingPalette.CyanBright, .35f);
            }
            playerRenderer.color = run.PlayerInvulnerabilityRemaining > 0f ? new Color(1f, .67f, .67f, .65f) : Color.white;
            playerRenderer.sortingOrder = ActorDepth(PlayerPoint.y) + 8;
            // The beacon raster already contains the palette; avoid multiplying its
            // cyan/cream pixels by another colored tint when the objective unlocks.
            visuals.Beacon.color = run.GateUnlocked ? Color.white : new Color(.5f, .58f, .54f);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                SentinelState state = run.GetSentinel(i);
                int depth = ActorDepth(enemy.Body.position.y);
                for (int j = 0; j < enemy.Renderers.Length; j++)
                    if (enemy.ActorLayers[j]) enemy.Renderers[j].sortingOrder = depth + enemy.LocalOrders[j];
                RefreshWarning(enemy, state);
                RefreshImpact(enemy);
                enemy.Eye.color = run.Time < enemy.HurtUntil ? ClearingPalette.Cream :
                    state.AttackPhase == SentinelAttackPhase.Active ? ClearingPalette.Cream :
                    state.AttackPhase == SentinelAttackPhase.Telegraph ? ClearingPalette.Danger :
                    enemy.Kind == SentinelAttackKind.Sigil ? ClearingPalette.CyanBright : ClearingPalette.AmberBright;
                enemy.Health.color = run.Time < enemy.HurtUntil ? ClearingPalette.Cream : ClearingPalette.Amber;
                float ratio = state.Health / ClearingRun.SentinelMaxHealth;
                enemy.Health.transform.localScale = new Vector3(.72f * ratio, .067f, 1f);
                enemy.Health.transform.localPosition = new Vector3((ratio - 1f) * .36f, 1.5f, 0f);
            }
        }

        private void RefreshWarning(EnemyView enemy, SentinelState state)
        {
            bool active = state.AttackPhase == SentinelAttackPhase.Active;
            bool warning = state.AttackPhase == SentinelAttackPhase.Telegraph || active;
            enemy.Warning.gameObject.SetActive(warning && !state.IsDead && !run.IsDead && !run.IsComplete);
            if (!warning) return;
            SentinelFootprint snapshot = enemy.Footprint;
            enemy.Warning.position = snapshot != null ? new Vector2(snapshot.CenterX, snapshot.CenterY) : enemy.AttackCenter;
            enemy.Warning.rotation = Quaternion.Euler(0f, 0f, snapshot != null ? snapshot.AngleDegrees : 0f);
            enemy.Warning.localScale = Vector3.one;
            // Only the inner charge grows. The outer damage boundary never moves,
            // shrinks or expands; an active X adds a non-color timing cue.
            float progress = active ? 1f : Mathf.Clamp01(state.PhaseProgress);
            float width = snapshot != null ? snapshot.Width : 1.9f;
            float height = snapshot != null ? snapshot.Height : 1.9f;
            enemy.WarningFill.transform.localScale = new Vector3(width * progress, height * progress, 1f);
            float crossLength = enemy.Kind == SentinelAttackKind.Sweep ? 1.3f : .68f * Mathf.Min(width, height);
            enemy.WarningCrossA.transform.localScale = new Vector3(crossLength, 2f / 30f, 1f);
            enemy.WarningCrossB.transform.localScale = new Vector3(crossLength, 2f / 30f, 1f);
            enemy.WarningFill.color = ClearingPalette.WithAlpha(active ? ClearingPalette.Danger : ClearingPalette.Amber,
                active ? .32f : .08f + .14f * progress);
            enemy.WarningCrossA.gameObject.SetActive(active);
            enemy.WarningCrossB.gameObject.SetActive(active);
        }

        private void ShowImpact(EnemyView enemy, bool isKill, bool isBurst)
        {
            enemy.ImpactStartedAt = run.Time;
            enemy.ImpactUntil = run.Time + (isKill ? .3f : .12f);
            enemy.ImpactWasKill = isKill;
            enemy.ImpactColor = isKill ? ClearingPalette.AmberBright :
                isBurst ? ClearingPalette.CyanBright : ClearingPalette.Cream;
            Vector2 point = enemy.Body.position + new Vector2(0f, .7f);
            // This root is independent of the defeated actor so the confirmation
            // survives its collider/body being immediately removed from play.
            enemy.Impact.position = new Vector3(point.x, point.y, 0f);
            enemy.Impact.localRotation = Quaternion.Euler(0f, 0f, isKill || isBurst ? 45f : 0f);
            enemy.Impact.gameObject.SetActive(true);
            RefreshImpact(enemy);
        }

        private void RefreshImpact(EnemyView enemy)
        {
            bool visible = run.Time < enemy.ImpactUntil && !run.IsDead && !run.IsComplete;
            enemy.Impact.gameObject.SetActive(visible);
            if (!visible) return;
            float progress = (float)((run.Time - enemy.ImpactStartedAt) / (enemy.ImpactUntil - enemy.ImpactStartedAt));
            float scale = enemy.ImpactWasKill ? 1f + .6f * progress : .75f + .25f * progress;
            enemy.Impact.localScale = new Vector3(scale, scale, 1f);
            Color color = ClearingPalette.WithAlpha(enemy.ImpactColor, 1f - progress);
            foreach (SpriteRenderer renderer in enemy.ImpactSprites) renderer.color = color;
        }

        private static int ActorDepth(float footY)
        {
            // One depth slot per existing art pixel; 16 slots keep all child offsets (1..7) together.
            return Mathf.RoundToInt(-footY * 30f) * 16;
        }

        private void LateUpdate()
        {
            if (run == null) return;
            // Fit the centered hero's full height above its feet at the north boundary.
            sceneCamera.orthographicSize = Mathf.Max(6f, 7.75f / Mathf.Max(sceneCamera.aspect, .1f));
            sceneCamera.transform.position = new Vector3(0f, .8f, -10f);
        }

        private void Feedback(string text, float duration)
        {
            feedback = text;
            feedbackUntil = Time.unscaledTime + duration;
            nextHudAt = 0f;
        }

        private void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (paused)
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }
            else Time.timeScale = timeScaleBeforePause;
            movement.SetControlEnabled(!paused && !run.IsDead && !run.IsComplete);
            nextHudAt = 0f;
        }

        private void SynchronizeDeath()
        {
            if (run.IsDead || run.IsComplete) return;
            float currentHealth = player.Stats.Health;
            if (currentHealth > 0f && !float.IsNaN(currentHealth) && !float.IsInfinity(currentHealth)) return;
            run.NotifyPlayerDeath();
            if (paused) SetPaused(false);
            StopActors();
            nextHudAt = 0f;
        }

        private void StopActors()
        {
            if (damageNumbers != null) damageNumbers.Clear();
            if (supplyVisuals != null) supplyVisuals.Clear();
            movement.SetControlEnabled(false);
            playerBody.velocity = Vector2.zero;
            if (strike != null) strike.gameObject.SetActive(false);
            foreach (EnemyView enemy in enemies)
            {
                if (enemy == null) continue;
                enemy.Body.velocity = Vector2.zero;
                enemy.Footprint = null;
                if (enemy.Warning != null) enemy.Warning.gameObject.SetActive(false);
                if (enemy.Impact != null) enemy.Impact.gameObject.SetActive(false);
            }
        }

        private void ResetRun()
        {
            if (damageNumbers != null) damageNumbers.Clear();
            if (supplyVisuals != null) supplyVisuals.Clear();
            supplies.Reset();
            for (int index = 0; index < supplyNotices.Length; index++) supplyNotices[index] = "";
            bool lostReward = run.IsComplete && progress != null && !progress.IsCompletionBanked(rewardId);
            if (paused) SetPaused(false);
            run.ResetRun();
            rewardId = Guid.NewGuid().ToString("N");
            if (progress != null)
            {
                // Recompute from authoring bases, never from previously boosted maxima.
                player.Stats.MaxHealth = baseMaxHealth > 0f && !float.IsInfinity(baseMaxHealth) ? baseMaxHealth + progress.BonusHealth : baseMaxHealth;
                player.Stats.MaxMana = baseMaxMana > 0f && !float.IsInfinity(baseMaxMana) ? baseMaxMana + progress.BonusMana : baseMaxMana;
                progressNotice = LoadNotice();
            }
            player.ResetForNewRun();
            movement.ResetMovement();
            playerBody.position = ClearingVisuals.PlayerSpawn - ClearingVisuals.PlayerFootOffset;
            playerBody.velocity = Vector2.zero;
            movement.enabled = true;
            movement.SetControlEnabled(true);
            playerRenderer.color = Color.white;
            gateWasOpened = false;
            lastHitAudioActionId = 0;
            lastKillAudioActionId = 0;
            sound.ResetFeedback();
            visuals.Gate.SetActive(true);
            strike.gameObject.SetActive(false);
            strikeId = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                enemy.Object.SetActive(true);
                enemy.Body.position = enemySpawns[i];
                enemy.Body.velocity = Vector2.zero;
                enemy.HurtUntil = 0f;
                enemy.Footprint = null;
                enemy.ImpactStartedAt = 0d;
                enemy.ImpactUntil = 0d;
                enemy.ImpactWasKill = false;
                enemy.Impact.gameObject.SetActive(false);
                enemy.WarningCrossA.gameObject.SetActive(false);
                enemy.WarningCrossB.gameObject.SetActive(false);
                enemy.Warning.gameObject.SetActive(false);
            }
            Feedback(lostReward ? "Previous run coins were not saved. Defeat the three sentinels." :
                "Defeat the sentinels. Sidestep spear lanes; leave rune marks.", 7f);
        }

        private void OnDisable()
        {
            if (damageNumbers != null) damageNumbers.Clear();
            if (supplyVisuals != null) supplyVisuals.Clear();
            if (sound != null) sound.ResetFeedback();
            // A paused scene must never leave the next loaded scene frozen.
            if (paused)
            {
                Time.timeScale = timeScaleBeforePause;
                paused = false;
            }
            if (movement != null && run != null)
            {
                // Hiding objects alone lets RefreshViews/FixedUpdate resurrect
                // their saved contacts and pulses after this component returns.
                run.CancelTransientActions();
                strikeId = 0;
                foreach (EnemyView enemy in enemies)
                {
                    if (enemy == null) continue;
                    enemy.HurtUntil = 0f;
                    enemy.ImpactStartedAt = 0d;
                    enemy.ImpactUntil = 0d;
                    enemy.ImpactWasKill = false;
                }
                StopActors();
                movement.SetControlEnabled(!run.IsDead && !run.IsComplete);
            }
        }

        private void OnDestroy()
        {
            if (damageNumbers != null) damageNumbers.Dispose();
            if (supplyVisuals != null) supplyVisuals.Dispose();
            if (visuals != null) visuals.Dispose();
            if (sound != null) sound.Dispose();
        }
    }
}
