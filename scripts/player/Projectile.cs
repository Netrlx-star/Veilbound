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
    public bool IsPiercing { get; set; } = false;
    public int BouncesRemaining { get; set; } = 0;
    public Enemies.StatusEffectType InflictedStatus { get; set; } = Enemies.StatusEffectType.None;

    // ── Internal ──
    private Vector2 _startPosition;
    private float _distanceTraveled = 0.0f;
    private readonly System.Collections.Generic.HashSet<Node2D> _piercedEnemies = new();

    public override void _Ready()
    {
        _startPosition = GlobalPosition;

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null && sprite.Texture == null)
        {
            Color tearColor = InflictedStatus switch
            {
                Enemies.StatusEffectType.Poison => new Color(0.35f, 0.95f, 0.35f),
                Enemies.StatusEffectType.Burn => new Color(1.0f, 0.55f, 0.15f),
                Enemies.StatusEffectType.Slow => new Color(0.40f, 0.85f, 1.0f),
                _ => new Color(0.35f, 0.75f, 1.0f)
            };
            sprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(6, tearColor);
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
            if (BouncesRemaining > 0)
            {
                BouncesRemaining--;
                // Invert direction with slight reflection angle
                Direction = -Direction + new Vector2((float)GD.RandRange(-0.2, 0.2), (float)GD.RandRange(-0.2, 0.2)).Normalized();
                return;
            }

            DestroyProjectile();
            return;
        }

        // Hit an enemy
        if (body.IsInGroup("enemies"))
        {
            if (_piercedEnemies.Contains(body)) return;
            _piercedEnemies.Add(body);

            if (body is Enemies.BaseEnemy enemy)
            {
                enemy.TakeDamage(Damage);
                enemy.ApplyKnockback(Direction, 200.0f);

                // Inflict status effect if present
                if (InflictedStatus != Enemies.StatusEffectType.None)
                {
                    enemy.StatusManager?.ApplyEffect(InflictedStatus, 3.5f, Damage * 0.4f);
                }

                // Bloodbound rally recovery
                var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
                player?.OnSuccessfulAttackHit();
            }
            else if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }

            if (!IsPiercing)
            {
                DestroyProjectile();
            }
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
