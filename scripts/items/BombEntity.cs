using Godot;

namespace Veilbound.Items;

/// <summary>
/// A placed iron bomb that ticks down and detonates with a damaging shockwave.
/// Destroys obstacles (rocks and pots) and deals high area-of-effect damage.
/// </summary>
public partial class BombEntity : Node2D
{
    [Export] public float FuseTime { get; set; } = 1.3f;
    [Export] public float BlastRadius { get; set; } = 110.0f;
    [Export] public float Damage { get; set; } = 35.0f;

    private Sprite2D _sprite;
    private double _timer = 0.0;
    private bool _exploded = false;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            Systems.AssetGenerator.InitializeAssets();
            _sprite.Texture = Systems.AssetGenerator.BombEntityTexture;
        }

        // Ticking pulse animation
        var tween = CreateTween().SetLoops();
        tween.TweenProperty(this, "scale", new Vector2(1.15f, 1.15f), 0.2);
        tween.TweenProperty(this, "scale", Vector2.One, 0.2);
    }

    public override void _Process(double delta)
    {
        if (_exploded) return;

        _timer += delta;

        // Flash red faster as fuse expires
        if (_timer > FuseTime * 0.5f && _sprite != null)
        {
            float flash = Mathf.Sin((float)_timer * 18.0f) * 0.5f + 0.5f;
            _sprite.Modulate = new Color(1.0f, 1.0f - flash * 0.7f, 1.0f - flash * 0.7f);
        }

        if (_timer >= FuseTime)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _exploded = true;

        // Visual blast circle shockwave
        var shockwave = new ColorRect
        {
            Color = new Color(1.0f, 0.6f, 0.2f, 0.85f),
            Position = new Vector2(-BlastRadius, -BlastRadius),
            Size = new Vector2(BlastRadius * 2, BlastRadius * 2),
            ZIndex = 5
        };
        GetTree().CurrentScene.AddChild(shockwave);

        var blastTween = shockwave.CreateTween();
        blastTween.SetParallel(true);
        blastTween.TweenProperty(shockwave, "scale", new Vector2(1.3f, 1.3f), 0.25);
        blastTween.TweenProperty(shockwave, "modulate:a", 0.0f, 0.25);
        blastTween.Chain().TweenCallback(Callable.From(shockwave.QueueFree));

        // Damage detection using spatial query
        var spaceState = GetWorld2D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters2D();
        var circle = new CircleShape2D { Radius = BlastRadius };
        query.Shape = circle;
        query.Transform = Transform2D.Identity.Translated(GlobalPosition);
        query.CollisionMask = 1 | 2 | 4; // Walls/obstacles (1), Player (2), Enemies (4)

        var results = spaceState.IntersectShape(query);
        foreach (var hit in results)
        {
            var collider = hit["collider"].As<Node2D>();
            if (collider == null) continue;

            // Damage Enemies
            if (collider is Enemies.BaseEnemy enemy)
            {
                var dir = (enemy.GlobalPosition - GlobalPosition).Normalized();
                enemy.TakeDamage(Damage);
                enemy.ApplyKnockback(dir, 350.0f);
            }
            // Destroy Obstacles (Rocks / Pots)
            else if (collider is Environments.RoomObstacle obstacle)
            {
                obstacle.Explode();
            }
            // Damage Player if caught in blast
            else if (collider is Player.PlayerController player)
            {
                var dist = GlobalPosition.DistanceTo(player.GlobalPosition);
                if (dist < BlastRadius * 0.75f)
                {
                    player.TakeDamage(1);
                }
            }
        }

        QueueFree();
    }
}
