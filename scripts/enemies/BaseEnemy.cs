using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// Base class for all enemies. Provides health, damage, knockback,
/// and death handling. Specific enemy types inherit from this.
/// </summary>
public partial class BaseEnemy : CharacterBody2D
{
    // ── Stats ──
    [Export] public float MaxHealth { get; set; } = 10.0f;
    [Export] public float ContactDamage { get; set; } = 1.0f;
    [Export] public float MoveSpeed { get; set; } = 80.0f;

    protected float CurrentHealth;
    protected bool IsDead = false;

    // ── Knockback ──
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private const float KnockbackFriction = 600.0f;

    // ── References ──
    protected Sprite2D Sprite;
    protected CollisionShape2D CollisionShape;

    // ── Signals ──
    [Signal] public delegate void EnemyDiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("enemies");

        Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        CollisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        EnemyReady();
    }

    /// <summary>
    /// Override in subclasses for specific enemy initialization.
    /// </summary>
    protected virtual void EnemyReady() { }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead) return;

        // Apply knockback
        if (_knockbackVelocity.Length() > 10.0f)
        {
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * (float)delta);
        }
        else
        {
            _knockbackVelocity = Vector2.Zero;
        }

        EnemyPhysicsProcess(delta);

        // Combine movement velocity with knockback
        Velocity += _knockbackVelocity;
        MoveAndSlide();

        // Contact damage with player
        CheckPlayerContact();
    }

    private void CheckPlayerContact()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is Player.PlayerController player)
            {
                player.TakeDamage((int)ContactDamage);
            }
        }
    }

    /// <summary>
    /// Override in subclasses for specific enemy movement/behavior.
    /// Set Velocity in this method (before knockback is added).
    /// </summary>
    protected virtual void EnemyPhysicsProcess(double delta) { }

    // ══════════════════════════════════════════════
    //  DAMAGE
    // ══════════════════════════════════════════════

    /// <summary>
    /// Called when this enemy takes damage (from projectiles, etc.)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        CurrentHealth -= damage;

        // Flash red
        if (Sprite != null)
        {
            Sprite.Modulate = new Color(1, 0.3f, 0.3f);
            var tween = CreateTween();
            tween.TweenProperty(Sprite, "modulate", Colors.White, 0.15);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Apply knockback force to this enemy.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        _knockbackVelocity = direction.Normalized() * force;
    }

    protected virtual void Die()
    {
        IsDead = true;
        EmitSignal(SignalName.EnemyDied);

        // 25% chance to drop a healing heart upon death
        if (GD.Randf() < 0.25f)
        {
            var heartScene = GD.Load<PackedScene>("res://scenes/items/HeartPickup.tscn");
            if (heartScene != null)
            {
                var heart = heartScene.Instantiate<Node2D>();
                heart.GlobalPosition = GlobalPosition;
                GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, heart);
            }
        }

        // Simple death animation: shrink and fade
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.3);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
