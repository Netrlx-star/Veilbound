using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// Projectile fired by enemies towards the player.
/// Deals damage to the player and gets destroyed upon colliding with walls or the player.
/// </summary>
public partial class EnemyProjectile : Area2D
{
    [Export] public float Speed { get; set; } = 220.0f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float MaxLifetime { get; set; } = 4.0f;
    public bool IsDeflected { get; set; } = false;

    public Vector2 Direction { get; set; } = Vector2.Zero;

    private float _lifetime = 0.0f;

    public override void _Ready()
    {
        AddToGroup("enemy_projectiles");

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null && sprite.Texture == null)
        {
            // Enemy tears are crimson/blood red
            sprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(7, new Color(0.9f, 0.2f, 0.2f));
        }

        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += Direction * Speed * (float)delta;
        _lifetime += (float)delta;

        if (_lifetime >= MaxLifetime)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        // Hit wall
        if (body is StaticBody2D)
        {
            QueueFree();
            return;
        }

        // If deflected, damages enemies instead of player
        if (IsDeflected)
        {
            if (body is BaseEnemy enemy)
            {
                enemy.TakeDamage(Damage * 4.0f); // Heavy reflected damage
                enemy.ApplyKnockback(Direction, 250.0f);
                QueueFree();
            }
            return;
        }

        // Hit player
        if (body is Player.PlayerController player)
        {
            player.TakeDamage(Damage);
            QueueFree();
        }
    }
}
