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
        private ClearingVisuals visuals;
        private ClearingHud hud;
        private ClearingAudio sound;
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
        private string feedback = "Defeat the three sentinels. Step out of orange warnings.";
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
            internal float HurtUntil;
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
                new Color(1f, .86f, .52f, .5f), 30000, true);
            strike.gameObject.SetActive(false);
            hud = new ClearingHud(transform);
            sound = new ClearingAudio(gameObject);
            ResetRun();
        }

        private Vector2 PlayerPoint { get { return playerBody.position + ClearingVisuals.PlayerFootOffset; } }
        private bool NearBeacon { get { return Vector2.Distance(PlayerPoint, ClearingVisuals.BeaconPosition) <= .95f; } }

        private void Update()
        {
            if (run == null) return;
            SynchronizeDeath();
            if (Input.GetKeyDown(KeyCode.M)) sound.ToggleMute();
            if ((run.IsDead || run.IsComplete) && Input.GetKeyDown(KeyCode.R)) ResetRun();
            if (!run.IsDead && !run.IsComplete && Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
            if (!paused && !run.IsDead && !run.IsComplete)
            {
                if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space)) StartAttack(PlayerAttackKind.Light);
                if (Input.GetKeyDown(KeyCode.K)) StartAttack(PlayerAttackKind.Burst);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (NearBeacon && run.TryCompleteObjective())
                    {
                        StopActors();
                        sound.Reward();
                        Feedback("The beacon is restored. The clearing is safe.", 5f);
                    }
                    else Feedback(run.GateUnlocked ? "Move closer to the north beacon." : "Defeat all three sentinels to break the seal.", 2f);
                }
            }
            RefreshViews();
            // Resource strings and layout rebuilds do not need to run every rendered frame.
            if (Time.unscaledTime >= nextHudAt)
            {
                nextHudAt = Time.unscaledTime + .05f;
                hud.Refresh(player.Stats, run, paused, sound.Muted, NearBeacon,
                    Time.unscaledTime < feedbackUntil ? feedback : "");
            }
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

        private void FixedUpdate()
        {
            if (run == null) return;
            SynchronizeDeath();
            if (paused || run.IsDead || run.IsComplete) return;
            run.Advance(Time.fixedDeltaTime);
            player.Stats.Mana = Mathf.Min(player.Stats.MaxMana,
                player.Stats.Mana + ClearingRun.ManaRegenerationRate * Time.fixedDeltaTime);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                SentinelState state = run.GetSentinel(i);
                if (state.IsDead) continue;
                Vector2 difference = enemy.Body.position - PlayerPoint;
                if (run.PlayerAttackActive && InStrike(difference) && ClearLine(PlayerPoint, enemy.Body.position) && run.TryHitSentinel(strikeId, i))
                {
                    enemy.HurtUntil = (float)run.Time + .12f;
                    if (state.IsDead)
                    {
                        enemy.Object.SetActive(false);
                        Feedback("Sentinel defeated. " + run.DefeatedCount + "/3 seals broken.", 2f);
                        continue;
                    }
                }
                Vector2 toPlayer = PlayerPoint - enemy.Body.position;
                bool clear = ClearLine(enemy.Body.position, PlayerPoint);
                if (state.AttackPhase == SentinelAttackPhase.Ready && clear && toPlayer.magnitude <= .85f)
                {
                    if (run.BeginSentinelAttack(i)) enemy.AttackCenter = enemy.Body.position;
                }
                if (state.AttackPhase == SentinelAttackPhase.Ready && clear && toPlayer.magnitude < 3.7f)
                    enemy.Body.MovePosition(enemy.Body.position + toPlayer.normalized * (1.1f * Time.fixedDeltaTime));
                else enemy.Body.velocity = Vector2.zero;
                // The warning is a stationary square; collision uses that same saved footprint.
                Vector2 contact = PlayerPoint - enemy.AttackCenter;
                if (state.AttackPhase == SentinelAttackPhase.Active && Mathf.Abs(contact.x) <= .95f && Mathf.Abs(contact.y) <= .95f &&
                    ClearLine(enemy.AttackCenter, PlayerPoint) && run.TryResolveSentinelHit(i))
                {
                    health.TakeDamage(ClearingRun.SentinelDamage);
                    sound.Hurt();
                    if (player.Stats.Health <= 0f)
                    {
                        run.NotifyPlayerDeath();
                        StopActors();
                        break;
                    }
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
            bool active = run.PlayerAttackActive;
            strike.gameObject.SetActive(active);
            if (active)
            {
                float center = strikeKind == PlayerAttackKind.Light ? .775f : 1.5f;
                Vector2 location = PlayerPoint + strikeDirection * center;
                strike.transform.position = new Vector3(location.x, location.y, 0f);
                strike.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(strikeDirection.y, strikeDirection.x) * Mathf.Rad2Deg);
                strike.transform.localScale = strikeKind == PlayerAttackKind.Light ? new Vector3(1.25f, 1.1f, 1) : new Vector3(2.8f, 2f, 1);
                strike.color = strikeKind == PlayerAttackKind.Light ? new Color(1f, .86f, .52f, .35f) : new Color(.47f, .87f, 1f, .4f);
            }
            playerRenderer.color = run.PlayerInvulnerabilityRemaining > 0f ? new Color(1f, .67f, .67f, .65f) : Color.white;
            playerRenderer.sortingOrder = ActorDepth(PlayerPoint.y) + 8;
            visuals.Beacon.color = run.GateUnlocked ? new Color(.56f, .95f, .9f) : new Color(.45f, .47f, .4f);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyView enemy = enemies[i];
                SentinelState state = run.GetSentinel(i);
                int depth = ActorDepth(enemy.Body.position.y);
                for (int j = 0; j < enemy.Renderers.Length; j++)
                    if (enemy.ActorLayers[j]) enemy.Renderers[j].sortingOrder = depth + enemy.LocalOrders[j];
                bool warning = state.AttackPhase == SentinelAttackPhase.Telegraph || state.AttackPhase == SentinelAttackPhase.Active;
                enemy.Warning.gameObject.SetActive(warning && !state.IsDead && !run.IsDead && !run.IsComplete);
                if (warning) enemy.Warning.position = enemy.AttackCenter;
                enemy.Eye.color = run.Time < enemy.HurtUntil ? Color.white : warning ? new Color(1f, .36f, .15f) : new Color(.98f, .79f, .39f);
                float ratio = state.Health / ClearingRun.SentinelMaxHealth;
                enemy.Health.transform.localScale = new Vector3(.72f * ratio, .067f, 1f);
                enemy.Health.transform.localPosition = new Vector3((ratio - 1f) * .36f, 1.5f, 0f);
            }
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
            movement.SetControlEnabled(false);
            playerBody.velocity = Vector2.zero;
            foreach (EnemyView enemy in enemies)
                if (enemy != null) enemy.Body.velocity = Vector2.zero;
        }

        private void ResetRun()
        {
            if (paused) SetPaused(false);
            run.ResetRun();
            player.ResetForNewRun();
            movement.ResetMovement();
            playerBody.position = ClearingVisuals.PlayerSpawn - ClearingVisuals.PlayerFootOffset;
            playerBody.velocity = Vector2.zero;
            movement.enabled = true;
            movement.SetControlEnabled(true);
            playerRenderer.color = Color.white;
            gateWasOpened = false;
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
                enemy.Warning.gameObject.SetActive(false);
            }
            Feedback("Defeat the three sentinels. Step out of orange warnings.", 7f);
        }

        private void OnDisable()
        {
            // A paused scene must never leave the next loaded scene frozen.
            if (paused)
            {
                Time.timeScale = timeScaleBeforePause;
                paused = false;
            }
            if (movement != null && run != null)
            {
                StopActors();
                movement.SetControlEnabled(!run.IsDead && !run.IsComplete);
            }
        }

        private void OnDestroy()
        {
            if (visuals != null) visuals.Dispose();
            if (sound != null) sound.Dispose();
        }
    }
}
