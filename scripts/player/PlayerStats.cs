using Godot;

namespace Veilbound.Player;

/// <summary>
/// Tracks all modifiable player stats for the current run.
/// Items and pickups modify these values.
/// </summary>
public class PlayerStats
{
    // ── Health ──
    public int MaxHealth { get; set; } = 6;       // In half-hearts (6 = 3 full hearts)
    public int CurrentHealth { get; set; } = 6;

    // ── Movement ──
    public float MoveSpeed { get; set; } = 200.0f;

    // ── Combat ──
    public float Damage { get; set; } = 3.5f;
    public float FireRate { get; set; } = 0.4f;     // Seconds between shots (lower = faster)
    public float ProjectileSpeed { get; set; } = 350.0f;
    public float ProjectileRange { get; set; } = 300.0f;
    public float Luck { get; set; } = 0.0f;

    // ── Knockback ──
    public float KnockbackForce { get; set; } = 150.0f;

    /// <summary>
    /// Reset stats to default for a new run.
    /// </summary>
    public void Reset()
    {
        MaxHealth = 6;
        CurrentHealth = 6;
        MoveSpeed = 200.0f;
        Damage = 3.5f;
        FireRate = 0.4f;
        ProjectileSpeed = 350.0f;
        ProjectileRange = 300.0f;
        Luck = 0.0f;
        KnockbackForce = 150.0f;
    }

    /// <summary>
    /// Take damage. Returns true if the player died.
    /// </summary>
    public bool TakeDamage(int amount = 1)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;
        return CurrentHealth <= 0;
    }

    /// <summary>
    /// Heal the player. Won't exceed max health.
    /// </summary>
    public void Heal(int amount = 1)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }
}
