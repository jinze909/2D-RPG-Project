using UnityEngine;

[RequireComponent(typeof(Player), typeof(PlayerAnimations), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    private bool controlsEnabled = true;

    public void SetControlEnabled(bool value)
    {
        controlsEnabled = value;
        if (!value) StopMovement();
    }

    public void ResetMovement()
    {
        StopMovement();
    }
    [Header("Config")]
    [SerializeField] private float speed;

    private PlayerAnimations playerAnimations;
    private PlayerActions actions;
    private Player player;
    private Rigidbody2D rb2D;
    private Vector2 moveDirection;

    private void Awake()
    {
        player = GetComponent<Player>();
        actions = new PlayerActions();
        rb2D = GetComponent<Rigidbody2D>();
        playerAnimations = GetComponent<PlayerAnimations>();
    }

    private void Update()
    {
        ReadMovement();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void ReadMovement()
    {
        if (!CanMove())
        {
            StopMovement();
            return;
        }

        // Preserve action magnitude while limiting diagonal keyboard input.
        moveDirection = Vector2.ClampMagnitude(actions.Movement.Move.ReadValue<Vector2>(), 1f);
        if (moveDirection == Vector2.zero)
        {
            playerAnimations.SetMoveBoolTransition(false);
            return;
        }

        playerAnimations.SetMoveBoolTransition(true);
        FacingDirection = moveDirection.normalized;
        playerAnimations.SetMoveAnimation(moveDirection.normalized);
    }

    private void Move()
    {
        // Health can change after Update; never apply a cached move after death.
        if (!CanMove())
        {
            StopMovement();
            return;
        }

        if (moveDirection == Vector2.zero)
        {
            rb2D.velocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = rb2D.position + moveDirection * (speed * Time.fixedDeltaTime);
        rb2D.MovePosition(targetPosition);
    }

    private bool CanMove()
    {
        return controlsEnabled && player.Stats != null && player.Stats.Health > 0f
            && !float.IsNaN(player.Stats.Health) && !float.IsInfinity(player.Stats.Health);
    }

    private void StopMovement()
    {
        moveDirection = Vector2.zero;
        rb2D.velocity = Vector2.zero;
        // Keep MoveX/MoveY so stopping never changes the last facing direction.
        playerAnimations.SetMoveBoolTransition(false);
    }

    private void OnEnable()
    {
        actions.Enable();
    }

    private void OnDisable()
    {
        actions.Disable();
        StopMovement();
    }

    private void OnDestroy()
    {
        actions.Dispose();
    }
}
