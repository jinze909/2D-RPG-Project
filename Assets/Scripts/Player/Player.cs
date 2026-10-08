using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStats stats;

    private PlayerStats runtimeStats;

    // stats remains the authoring template. Current resources belong to one
    // actor/session and must never be written back into that shared asset.
    public PlayerStats Stats => runtimeStats != null ? runtimeStats : stats;

    private void Awake()
    {
        if (stats == null) return;
        runtimeStats = Instantiate(stats);
        runtimeStats.ResetPlayer();
    }

    public void ResetForNewRun()
    {
        if (Stats == null) return;
        Stats.ResetPlayer();
        var animations = GetComponent<PlayerAnimations>();
        if (animations != null) animations.SetReviveAnimation();
    }

    private void OnDestroy()
    {
        if (runtimeStats != null) Destroy(runtimeStats);
    }
}
