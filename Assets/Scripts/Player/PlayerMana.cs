using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStats stats;

    public void UseMana(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }

        if (stats.Mana >= amount)
        {
            stats.Mana = Mathf.Max(stats.Mana - amount, 0f);
        }
    }
}
