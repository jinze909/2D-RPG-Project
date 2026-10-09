using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    // Retain the original prototype's P key; actual gameplay scenes opt out.
    public bool DebugDamageEnabled { get; set; } = true;
    [Header("Config")]
    [SerializeField] private PlayerStats stats;

    private PlayerAnimations playerAnimations;
    private Player owner;
    private PlayerStats CurrentStats => owner != null ? owner.Stats : stats;

    private void Awake()
    {
        playerAnimations = GetComponent<PlayerAnimations>();
        owner = GetComponent<Player>();
    }

    private void Update()
    {
        if (DebugDamageEnabled && Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(1f);
        }
    }
    public void TakeDamage(float amount)
    {
        var current = CurrentStats;
        if (current == null || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)
            || current.Health <= 0f || float.IsNaN(current.Health) || float.IsInfinity(current.Health))
        {
            return;
        }

        current.Health = Mathf.Max(current.Health - amount, 0f);
        if (current.Health == 0f)
        {
            PlayerDead();
        }
    }
    private void PlayerDead()
    {
        if (playerAnimations != null) playerAnimations.SetDeadAnimation();
    }
}
