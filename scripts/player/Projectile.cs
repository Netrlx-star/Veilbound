using Godot;

namespace Veilbound.Player;

/// <summary>
/// Player projectile (tear). Travels in a direction until it hits something
/// or exceeds its range, then disappears.
/// </summary>
public partial class Projectile : Area2D
{
    // ── Set by the player when spawning ──
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Speed { get; set; } = 350.0f;
    public float Damage { get; set; } = 3.5f;
    public float MaxRange { get; set; } = 300.0f;

    // ── Internal ──
    private Vector2 _startPosition;
    private float _distanceTraveled = 0.0f;

    public override void _Ready()
    {
        _startPosition = GlobalPosition;

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null && sprite.Texture == null)
        {
            sprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(6, new Color(0.35f, 0.75f, 1.0f));
        }

        // Connect to body_entered for hitting enemies/walls
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        var movement = Direction * Speed * (float)delta;
        GlobalPosition += movement;
        _distanceTraveled += movement.Length();

        // Destroy when range exceeded
        if (_distanceTraveled >= MaxRange)
        {
            DestroyProjectile();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        // Hit a wall (StaticBody2D on walls layer)
        if (body is StaticBody2D)
        {
            DestroyProjectile();
            return;
        }

        // Hit an enemy
        if (body.IsInGroup("enemies"))
        {
            if (body is Enemies.BaseEnemy enemy)
            {
                enemy.TakeDamage(Damage);
                enemy.ApplyKnockback(Direction, 200.0f);
            }
            else if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }
            DestroyProjectile();
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Could be used for hitting enemy hurtboxes
        if (area.IsInGroup("enemy_hurtbox"))
        {
            var enemy = area.GetParent();
            if (enemy != null && enemy.HasMethod("TakeDamage"))
            {
                enemy.Call("TakeDamage", Damage);
            }
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        // TODO: Spawn hit particle effect here
        QueueFree();
    }
}
