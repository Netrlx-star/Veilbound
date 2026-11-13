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

        var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        if (player != null)
        {
            player.Stats.EnemiesKilled++;
        }

        // Drop system: Hearts, Coins, Bombs, Keys
        SpawnDrops(player);

        // Simple death animation: shrink and fade
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.3);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void SpawnDrops(Player.PlayerController player)
    {
        var targetParent = GetParent() ?? (Node)GetTree().CurrentScene;
        float roll = GD.Randf();

        // 1. Heart drop: 30% base (45% if hurt)
        float heartChance = (player != null && player.Stats.CurrentHealth < player.Stats.MaxHealth) ? 0.45f : 0.30f;
        if (roll < heartChance)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Heart, 0, 1);
            return;
        }

        // 2. Coin drop: 25% chance
        if (roll < heartChance + 0.25f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Coin, 0, 1);
            return;
        }

        // 3. Bomb drop: 8% chance (refills player's bombs!)
        if (roll < heartChance + 0.25f + 0.08f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Bomb, 0, 1);
            return;
        }

        // 4. Key drop: 8% chance (allows unlocking shops & chests!)
        if (roll < heartChance + 0.25f + 0.08f + 0.08f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Key, 0, 1);
        }
    }
}
