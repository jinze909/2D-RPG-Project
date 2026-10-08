using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] private PlayerStats stats;

    private PlayerAnimations playerAnimations;

    private void Awake()
    {
        playerAnimations = GetComponent<PlayerAnimations>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(1f);
        }
    }
    public void TakeDamage(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || stats.Health <= 0f)
        {
            return;
        }

        stats.Health = Mathf.Max(stats.Health - amount, 0f);
        if (stats.Health == 0f)
        {
            PlayerDead();
        }
    }
    private void PlayerDead()
    {
        playerAnimations.SetDeadAnimation();
    }
}
