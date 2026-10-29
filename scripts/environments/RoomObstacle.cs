using Godot;

namespace Veilbound.Environments;

public enum ObstacleType { Rock, Pot }

/// <summary>
/// Solid dungeon obstacle (Rock or Ceramic Pot).
/// Blocks player and projectile paths.
/// Pots can be destroyed by projectiles or bombs; Rocks are destroyed by bombs.
/// </summary>
public partial class RoomObstacle : StaticBody2D
{
    [Export] public ObstacleType Type { get; set; } = ObstacleType.Rock;

    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = 1; // Wall layer
        CollisionMask = 0;
        AddToGroup("obstacles");

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            Systems.AssetGenerator.InitializeAssets();
            _sprite.Texture = (Type == ObstacleType.Rock)
                ? Systems.AssetGenerator.RockObstacleTexture
                : Systems.AssetGenerator.PotObstacleTexture;
        }
    }

    /// <summary>
    /// Hit by player tear or attack.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (Type == ObstacleType.Pot)
        {
            DestroyObstacle();
        }
    }

    /// <summary>
    /// Destroyed by bomb explosion or heavy impact.
    /// </summary>
    public void Explode()
    {
        DestroyObstacle();
    }

    private void DestroyObstacle()
    {
        // 20% chance to drop coin or heart
        float dropRoll = GD.Randf();
        if (dropRoll < 0.15f)
        {
            var heartScene = GD.Load<PackedScene>("res://scenes/items/HeartPickup.tscn");
            if (heartScene != null)
            {
                var heart = heartScene.Instantiate<Node2D>();
                heart.GlobalPosition = GlobalPosition;
                GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, heart);
            }
        }
        else if (dropRoll < 0.30f)
        {
            var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
            player?.AddCoins(1);
        }

        // Debris crumbling tween
        var tween = CreateTween();
        tween.Parallel().TweenProperty(this, "scale", Vector2.Zero, 0.2);
        tween.Parallel().TweenProperty(this, "modulate:a", 0.0f, 0.2);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
