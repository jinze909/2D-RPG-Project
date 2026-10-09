using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Player Stats")]

public class PlayerStats : ScriptableObject
{
    [Header("Config")]
    public int level;
    [Header("Health")]
    public float Health;
    public float MaxHealth;
    [Header("Mana")]
    public float Mana;
    public float MaxMana;

    public void ResetPlayer()
    {
        Health = ValidMaximum(MaxHealth);
        Mana = ValidMaximum(MaxMana);
    }

    private static float ValidMaximum(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;
    }
}
