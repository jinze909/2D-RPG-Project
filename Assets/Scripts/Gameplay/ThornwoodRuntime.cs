using System;
using UnityEngine;

namespace Rpg.Gameplay
{
    /// <summary>
    /// The clearing's unlocked expedition. It reuses the same actor-owned resources,
    /// input, attack rules and transaction store without resetting the completed clearing.
    /// World objects, contact tokens and HUD belong to this expedition alone.
    /// </summary>
    internal sealed class ThornwoodRuntime : IDisposable
    {
        private readonly Player player;
        private readonly Camera sceneCamera;
        private readonly ClearingAudio sound;
        private readonly ClearingProgress progress;
        private readonly float baseMaxHealth;
        private readonly float baseMaxMana;
        private readonly PlayerHealth health;
        private readonly PlayerMana mana;
        private readonly PlayerMovement movement;
        private readonly Rigidbody2D playerBody;
        private readonly SpriteRenderer playerRenderer;
        private readonly GameObject uiRoot;
        private readonly ThornwoodVisuals visuals;
        private readonly ClearingHud hud;
        private readonly ClearingDamageNumbers damageNumbers;
        private readonly SpriteRenderer strike;
        private readonly ThornwoodRun run = new ThornwoodRun();
        private string rewardId;
        private bool rewardBanked;
        private bool initialized;
        private bool disposed;
        private float timeScaleBeforePause;
        private Vector3 previousCameraPosition;
        private float previousCameraSize;
        private Vector2 lastSafePoint;
        private long strikeId;
        private PlayerAttackKind strikeKind;
        private Vector2 strikeDirection;
        private long lastHitAudioActionId;
        private long lastKillAudioActionId;
        private string feedback = "";
        private double feedbackUntil;
        private string progressNotice = "";
        private float nextHudAt;

        internal bool Active { get { return !disposed && run.Entered; } }
        internal bool HasUnbankedReward { get { return initialized && run.Combat.IsComplete && !rewardBanked; } }

        internal ThornwoodRuntime(Transform parent, Player player, Camera camera,
            ClearingVisuals sharedArt, ClearingAudio sound, ClearingProgress progress,
            float baseMaxHealth, float baseMaxMana)
        {
            this.player = player;
            sceneCamera = camera;
            this.sound = sound;
            this.progress = progress;
            this.baseMaxHealth = baseMaxHealth;
            this.baseMaxMana = baseMaxMana;
            health = player.GetComponent<PlayerHealth>();
            mana = player.GetComponent<PlayerMana>();
            movement = player.GetComponent<PlayerMovement>();
            playerBody = player.GetComponent<Rigidbody2D>();
            playerRenderer = player.GetComponent<SpriteRenderer>();
            visuals = new ThornwoodVisuals(parent, sharedArt);
            uiRoot = new GameObject("Thornwood expedition UI");
            uiRoot.transform.SetParent(parent, false);
            hud = new ClearingHud(uiRoot.transform);
            damageNumbers = new ClearingDamageNumbers(visuals.Root.transform);
            strike = sharedArt.Block("Thornwood directional player strike", visuals.Root.transform,
                Vector2.zero, Vector2.one, ClearingPalette.WithAlpha(ClearingPalette.AmberBright, .35f), 30000, true);
            strike.gameObject.SetActive(false);
            visuals.SetActive(false);
            uiRoot.SetActive(false);
        }

        // ClearingRuntime owns real unlocking and banking admission before calling.
        internal bool Enter()
        {
            if (disposed || Active || !Alive || Time.timeScale <= 0f) return false;
            if (!initialized) ResetAttempt();
            if (!run.TryEnter(true, true, Alive, false)) return false;
            previousCameraPosition = sceneCamera.transform.position;
            previousCameraSize = sceneCamera.orthographicSize;
            visuals.SetActive(true);
            uiRoot.SetActive(true);
            Teleport(ThornwoodLayout.Entry);
            movement.enabled = true;
            movement.SetControlEnabled(!run.Combat.IsDead);
            playerRenderer.color = Color.white;
            Feedback(run.Combat.IsComplete ? "Expedition complete. Return south to the clearing." :
                "Collect three thorn seeds and defeat three Briar Stalkers. Open the north cache; return south at any time.", 7d);
            RefreshViews();
            RefreshHud();
            return true;
        }

        internal void Update()
        {
            if (!Active) return;
            SynchronizeDeath();
            // Resume controls only when this active owner is executing again.
            // Suspend must not leave movement running without managed contacts.
            movement.SetControlEnabled(!run.Paused && !run.Combat.IsDead);
            if (Input.GetKeyDown(KeyCode.M)) sound.ToggleMute();
            if (Input.GetKeyDown(KeyCode.Escape) && !run.Combat.IsDead) SetPaused(!run.Paused);
            if (run.Combat.IsDead || run.Combat.IsComplete)
            {
                if (!run.Paused && Input.GetKeyDown(KeyCode.R))
                {
                    bool lost = run.Combat.IsComplete && !rewardBanked;
                    ResetAttempt();
                    run.TryEnter(true, true, Alive, false);
                    Teleport(ThornwoodLayout.Entry);
                    movement.SetControlEnabled(true);
                    Feedback(lost ? "Previous cache reward was not saved. Expedition restarted." :
                        "Expedition restarted. Collect three seeds and defeat three stalkers.", 6d);
                }
                else if (!run.Paused && Input.GetKeyDown(KeyCode.E)) Interact();
            }
            else if (!run.Paused)
            {
                if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space)) StartAttack(PlayerAttackKind.Light);
                if (Input.GetKeyDown(KeyCode.K)) StartAttack(PlayerAttackKind.Burst);
                if (Input.GetKeyDown(KeyCode.E)) Interact();
            }
            if (!Active) return;
            RefreshViews();
            if (Time.unscaledTime >= nextHudAt) RefreshHud();
        }

        private bool Alive
        {
            get { return player != null && player.Stats != null && player.Stats.Health > 0f && Finite(player.Stats.Health); }
        }

        private Vector2 PlayerPoint { get { return playerBody.position + ClearingVisuals.PlayerFootOffset; } }
        private static Vector2 Point(ThornwoodPoint point) { return new Vector2(point.X, point.Y); }

        private void Teleport(ThornwoodPoint point)
        {
            movement.ResetMovement();
            lastSafePoint = Point(point);
            playerBody.position = lastSafePoint - ClearingVisuals.PlayerFootOffset;
            playerBody.velocity = Vector2.zero;
        }

        private static bool ClearLine(Vector2 from, Vector2 to)
        {
            return ThornwoodLayout.HasLineOfSight(from.x, from.y, to.x, to.y) &&
                Physics2D.Linecast(from, to, 1 << 0).collider == null;
        }

        private bool Near(ThornwoodPoint point)
        {
            Vector2 location = Point(point);
            return Vector2.Distance(PlayerPoint, location) <= .75f && ClearLine(PlayerPoint, location);
        }

        private void Interact()
        {
            if (!Active || run.Paused || run.Combat.IsDead) return;
            // The south route never depends on kills, seeds or a working save store.
            if (Near(ThornwoodLayout.Entry)) { Leave(); return; }
            if (run.Combat.IsComplete)
            {
                BankCompletion();
                return;
            }
            for (int i = 0; i < ThornwoodRun.SeedCount; i++)
            {
                ThornwoodPoint point = ThornwoodLayout.SeedPosition(i);
                if (!Near(point)) continue;
                if (run.TryCollectSeed(i, PlayerPoint.x, PlayerPoint.y, ClearLine(PlayerPoint, Point(point))))
                {
                    visuals.SetSeedCollected(i);
                    sound.Reward();
                    Feedback("Thorn seed collected: " + run.SeedsCollected + "/3. Return south whenever you need to leave.", 4d);
                }
                else Feedback("This seed has already been collected this expedition.", 2d);
                return;
            }
            if (Near(ThornwoodLayout.Cache))
            {
                if (!run.TryComplete(PlayerPoint.x, PlayerPoint.y, ClearLine(PlayerPoint, Point(ThornwoodLayout.Cache))))
                {
                    Feedback("Cache sealed: collect all three seeds and defeat all three stalkers.", 4d);
                    return;
                }
                ClearContacts();
                visuals.SetCacheOpened(true);
                movement.SetControlEnabled(true);
                BankCompletion();
                Feedback("Thornwood restored. Cache earns 30 coins; return south to the clearing.", 6d);
                return;
            }
            Feedback(run.CacheReady ? "All objectives complete. Open the north cache with E, then return south." :
                "Explore the forest: three thorn seeds and three Briar Stalkers. South exit is always open.", 4d);
        }

        private ProgressActionResult BankCompletion()
        {
            if (!Active || run.Paused || run.Combat.IsDead || !run.Combat.IsComplete) return ProgressActionResult.RunIncomplete;
            // v1 records only the last reward identity. Preserve this accepted
            // transaction independently while visiting another completed activity.
            if (rewardBanked) return ProgressActionResult.AlreadyBanked;
            ProgressActionResult result = progress == null ? ProgressActionResult.Unavailable : progress.BankCompletion(run.Combat, rewardId);
            if (result == ProgressActionResult.Saved || result == ProgressActionResult.AlreadyBanked)
            {
                rewardBanked = true;
                progressNotice = "30 forest coins banked. Existing upgrades apply to your next new attempt.";
                if (result == ProgressActionResult.Saved) sound.Reward();
            }
            else progressNotice = result == ProgressActionResult.CapacityReached ?
                "Progress limit reached. Forest reward unbanked; R loses it." :
                "Forest reward NOT SAVED. E retries saving; leaving preserves this attempt. R loses unbanked coins.";
            nextHudAt = 0f;
            return result;
        }

        private void StartAttack(PlayerAttackKind kind)
        {
            if (!run.IsPlayable || !Alive) return;
            float availableMana = player.Stats.Mana;
            if (!run.Combat.CanPlayerAttack(kind, availableMana))
            {
                if (kind == PlayerAttackKind.Burst && availableMana < ClearingRun.BurstManaCost)
                    Feedback("Not enough MP. J is free; MP regenerates while exploring.", 2d);
                return;
            }
            if (kind == PlayerAttackKind.Burst && !mana.TryUseMana(ClearingRun.BurstManaCost)) return;
            if (!run.TryBeginPlayerAttack(kind, availableMana, out strikeId)) return;
            strikeKind = kind;
            strikeDirection = movement.FacingDirection;
            if (strikeDirection.sqrMagnitude < .01f) strikeDirection = Vector2.down;
            strikeDirection.Normalize();
            sound.Attack(kind == PlayerAttackKind.Burst);
        }

        internal void FixedUpdate()
        {
            if (!Active) return;
            SynchronizeDeath();
            if (run.Paused || run.Combat.IsDead) return;
            ProtectBounds();
            if (run.Combat.IsComplete) return;
            run.Advance(Time.fixedDeltaTime);
            player.Stats.Mana = Mathf.Min(player.Stats.MaxMana,
                player.Stats.Mana + ClearingRun.ManaRegenerationRate * Time.fixedDeltaTime);
            bool contacted = false, killed = false;
            // Apply every admitted player contact before any hostile retaliation.
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
            {
                SentinelState state = run.Combat.GetSentinel(i);
                if (state.IsDead) continue;
                ThornwoodEnemyView enemy = visuals.Enemies[i];
                Vector2 point = enemy.Body.position;
                float before = state.Health;
                if (!run.Combat.PlayerAttackActive || !InStrike(point - PlayerPoint) || !ClearLine(PlayerPoint, point) ||
                    !run.TryHitStalker(strikeId, i)) continue;
                contacted = true;
                killed |= state.IsDead;
                visuals.ShowImpact(i, state.IsDead, strikeKind == PlayerAttackKind.Burst, run.Combat.Time);
                damageNumbers.Show(i, before - state.Health, point + new Vector2(0f, 1.45f),
                    strikeKind == PlayerAttackKind.Burst ? ClearingPalette.CyanBright : ClearingPalette.Cream, run.Combat.Time);
                if (state.IsDead) enemy.Body.velocity = Vector2.zero;
                if (state.IsDead) Feedback("Briar Stalker defeated: " + run.Combat.DefeatedCount + "/3. Collect seeds for the north cache.", 4d);
            }
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
            {
                SentinelState state = run.Combat.GetSentinel(i);
                if (state.IsDead) continue;
                ThornwoodEnemyView enemy = visuals.Enemies[i];
                Vector2 origin = enemy.Body.position, target = PlayerPoint, difference = target - origin;
                bool clear = ClearLine(origin, target);
                SentinelDecision decision = run.ChooseStalkerAction(i, difference.magnitude, clear);
                if (state.AttackPhase == SentinelAttackPhase.Ready && decision == SentinelDecision.Attack)
                    run.TryBeginStalkerAttack(i, origin.x, origin.y, target.x, target.y, clear);
                if (state.AttackPhase == SentinelAttackPhase.Ready && decision == SentinelDecision.Approach)
                {
                    Vector2 next = origin + difference.normalized * (ThornwoodRun.StalkerSpeed * Time.fixedDeltaTime);
                    if (ThornwoodLayout.CanTravel(origin.x, origin.y, next.x, next.y, .24f) && ClearLine(origin, next))
                        enemy.Body.MovePosition(next);
                    else enemy.Body.velocity = Vector2.zero;
                }
                else enemy.Body.velocity = Vector2.zero;
                SentinelFootprint footprint = run.GetStalkerFootprint(i);
                if (state.AttackPhase != SentinelAttackPhase.Active || footprint == null) continue;
                bool contactClear = ClearLine(new Vector2(footprint.OriginX, footprint.OriginY), target);
                if (!run.TryResolveStalkerHit(i, target.x, target.y, contactClear)) continue;
                float before = player.Stats.Health;
                health.TakeDamage(ClearingRun.SentinelDamage);
                damageNumbers.Show(ClearingRun.SentinelCount, before - player.Stats.Health,
                    target + new Vector2(0f, 2.35f), ClearingPalette.Danger, run.Combat.Time);
                sound.Hurt();
                if (!Alive)
                {
                    run.NotifyPlayerDeath();
                    StopActors();
                    break;
                }
            }
            if (contacted && !run.Combat.IsDead)
            {
                if (killed && lastKillAudioActionId != strikeId)
                {
                    sound.Hit(true, strikeKind == PlayerAttackKind.Burst);
                    lastKillAudioActionId = lastHitAudioActionId = strikeId;
                }
                else if (lastHitAudioActionId != strikeId)
                {
                    sound.Hit(false, strikeKind == PlayerAttackKind.Burst);
                    lastHitAudioActionId = strikeId;
                }
            }
        }

        private void ProtectBounds()
        {
            Vector2 feet = PlayerPoint;
            // Native collisions are supplemented by the same tested layout used
            // for boundaries, so an out-of-bounds body cannot bypass a wall.
            if (ThornwoodLayout.CanTravel(lastSafePoint.x, lastSafePoint.y, feet.x, feet.y,
                ThornwoodLayout.FootRadius)) lastSafePoint = feet;
            else
            {
                playerBody.position = lastSafePoint - ClearingVisuals.PlayerFootOffset;
                playerBody.velocity = Vector2.zero;
            }
        }

        private bool InStrike(Vector2 difference)
        {
            float forward = Vector2.Dot(difference, strikeDirection);
            float side = Mathf.Abs(difference.x * strikeDirection.y - difference.y * strikeDirection.x);
            return strikeKind == PlayerAttackKind.Light ? forward >= .15f && forward <= 1.4f && side <= .55f :
                forward >= .1f && forward <= 2.9f && side <= 1f;
        }

        private void RefreshViews()
        {
            damageNumbers.Refresh(run.Combat.Time);
            visuals.Refresh(run.Combat.Time);
            for (int i = 0; i < ClearingRun.SentinelCount; i++)
            {
                ThornwoodEnemyView enemy = visuals.Enemies[i];
                Vector2 facing = PlayerPoint - enemy.Body.position;
                visuals.RefreshEnemy(i, run.Combat.GetSentinel(i), facing, run.Combat.Time, run.GetStalkerFootprint(i));
            }
            bool active = run.IsPlayable && run.Combat.PlayerAttackActive;
            strike.gameObject.SetActive(active);
            if (active)
            {
                float center = strikeKind == PlayerAttackKind.Light ? .775f : 1.5f;
                Vector2 location = PlayerPoint + strikeDirection * center;
                strike.transform.position = new Vector3(location.x, location.y, 0f);
                strike.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(strikeDirection.y, strikeDirection.x) * Mathf.Rad2Deg);
                strike.transform.localScale = strikeKind == PlayerAttackKind.Light ? new Vector3(1.25f, 1.1f, 1f) : new Vector3(2.8f, 2f, 1f);
                strike.color = ClearingPalette.WithAlpha(strikeKind == PlayerAttackKind.Light ?
                    ClearingPalette.AmberBright : ClearingPalette.CyanBright, .35f);
            }
            playerRenderer.color = run.Combat.PlayerInvulnerabilityRemaining > 0f ? new Color(1f, .67f, .67f, .65f) : Color.white;
            playerRenderer.sortingOrder = Mathf.RoundToInt(-PlayerPoint.y * 30f) * 16 + 8;
        }

        private void RefreshHud()
        {
            nextHudAt = Time.unscaledTime + .05f;
            hud.RefreshThornwood(player.Stats, run, run.Paused, sound.Muted,
                run.Combat.Time < feedbackUntil ? feedback : "", progress, rewardId,
                progressNotice, InteractionHint(), rewardBanked);
        }

        private string InteractionHint()
        {
            if (run.Paused) return "";
            if (run.Combat.IsDead) return "R - retry the expedition";
            if (Near(ThornwoodLayout.Entry)) return "E - return to clearing (expedition retained)";
            if (run.Combat.IsComplete) return rewardBanked ? "South exit: E - return to clearing" : "E - retry saving 30 forest coins; south exit remains open";
            for (int i = 0; i < ThornwoodRun.SeedCount; i++)
                if (Near(ThornwoodLayout.SeedPosition(i))) return run.IsSeedCollected(i) ?
                    "Seed collected this expedition (" + run.SeedsCollected + "/3)" : "E - collect thorn seed";
            if (Near(ThornwoodLayout.Cache)) return run.CacheReady ? "E - open cache and save 30 coins" : "Cache requires three seeds and three defeated stalkers";
            return "";
        }

        internal void LateUpdate()
        {
            if (!Active) return;
            sceneCamera.orthographicSize = Mathf.Max(6.7f, 8.6f / Mathf.Max(sceneCamera.aspect, .1f));
            sceneCamera.transform.position = new Vector3(ThornwoodLayout.CenterX, .8f, -10f);
        }

        private void Feedback(string text, double duration)
        {
            feedback = text;
            feedbackUntil = run.Combat.Time + duration;
            nextHudAt = 0f;
        }

        private void SetPaused(bool value)
        {
            if (run.Paused == value) return;
            if (value) timeScaleBeforePause = Time.timeScale;
            run.SetPaused(value);
            Time.timeScale = value ? 0f : timeScaleBeforePause;
            movement.SetControlEnabled(!value && !run.Combat.IsDead);
            if (value)
            {
                playerBody.velocity = Vector2.zero;
                foreach (ThornwoodEnemyView enemy in visuals.Enemies) enemy.Body.velocity = Vector2.zero;
            }
            nextHudAt = 0f;
        }

        private void SynchronizeDeath()
        {
            if (run.Combat.IsDead || run.Combat.IsComplete || Alive) return;
            run.NotifyPlayerDeath();
            if (run.Paused) SetPaused(false);
            StopActors();
            nextHudAt = 0f;
        }

        private void ClearContacts()
        {
            run.Suspend();
            strikeId = 0;
            strike.gameObject.SetActive(false);
            damageNumbers.Clear();
            visuals.Clear();
            playerBody.velocity = Vector2.zero;
            foreach (ThornwoodEnemyView enemy in visuals.Enemies) enemy.Body.velocity = Vector2.zero;
            sound.ResetFeedback();
        }

        private void StopActors()
        {
            ClearContacts();
            movement.SetControlEnabled(false);
        }

        private void ResetAttempt()
        {
            if (run.Paused) SetPaused(false);
            ClearContacts();
            run.Reset();
            visuals.Reset();
            rewardId = Guid.NewGuid().ToString("N");
            rewardBanked = false;
            lastHitAudioActionId = lastKillAudioActionId = 0;
            progressNotice = progress == null || !progress.CanWrite ? "Saving unavailable; existing files protected." : "";
            player.Stats.MaxHealth = baseMaxHealth > 0f && Finite(baseMaxHealth) && progress != null ? baseMaxHealth + progress.BonusHealth : baseMaxHealth;
            player.Stats.MaxMana = baseMaxMana > 0f && Finite(baseMaxMana) && progress != null ? baseMaxMana + progress.BonusMana : baseMaxMana;
            player.ResetForNewRun();
            playerRenderer.color = Color.white;
            initialized = true;
            feedback = "";
            feedbackUntil = 0d;
            nextHudAt = 0f;
        }

        private void Leave()
        {
            if (run.Paused) SetPaused(false);
            ClearContacts();
            if (!run.TryLeave()) return;
            visuals.SetActive(false);
            uiRoot.SetActive(false);
            Teleport(ThornwoodLayout.ReturnToClearing);
            movement.SetControlEnabled(false);
            playerRenderer.color = Color.white;
            sceneCamera.transform.position = previousCameraPosition;
            sceneCamera.orthographicSize = previousCameraSize;
        }

        // Component disable keeps the expedition and its accepted reward, but
        // cancels unsafe transient contacts without refunding costs/cooldowns.
        internal void Suspend()
        {
            if (disposed) return;
            if (run.Paused) SetPaused(false);
            run.Suspend();
            ClearContacts();
            if (Active) movement.SetControlEnabled(false);
        }

        internal void ResetExpedition()
        {
            if (disposed) return;
            if (run.Paused) SetPaused(false);
            ClearContacts();
            run.Reset();
            run.TryLeave();
            initialized = false;
            rewardId = "";
            rewardBanked = false;
            visuals.Reset();
            visuals.SetActive(false);
            uiRoot.SetActive(false);
        }

        public void Dispose()
        {
            if (disposed) return;
            if (run.Paused) SetPaused(false);
            ClearContacts();
            disposed = true;
            damageNumbers.Dispose();
            visuals.Dispose();
            UnityEngine.Object.Destroy(uiRoot);
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
