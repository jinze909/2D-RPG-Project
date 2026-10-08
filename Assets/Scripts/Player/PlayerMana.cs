using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStats stats;
    private Player owner;
    private PlayerStats CurrentStats => owner != null ? owner.Stats : stats;

    private void Awake()
    {
        owner = GetComponent<Player>();
    }

    public void UseMana(float amount)
    {
        TryUseMana(amount);
    }

    public bool TryUseMana(float amount)
    {
        var current = CurrentStats;
        if (current == null || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)
            || float.IsNaN(current.Mana) || float.IsInfinity(current.Mana) || current.Mana < amount)
            return false;

        current.Mana = Mathf.Max(current.Mana - amount, 0f);
        return true;
    }
}
