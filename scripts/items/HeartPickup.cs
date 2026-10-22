using Godot;

namespace Veilbound.Items;

/// <summary>
/// A consumable heart pickup that heals the player upon contact.
/// </summary>
public partial class HeartPickup : Area2D
{
    [Export] public int HealAmount { get; set; } = 2; // 2 half-hearts = 1 full red heart

    private float _bobTimer = 0.0f;
    private Vector2 _basePosition;

    public override void _Ready()
    {
        _basePosition = Position;

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null && sprite.Texture == null)
        {
            sprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(8, new Color(0.95f, 0.15f, 0.2f));
        }

        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        // Gentle bobbing animation
        _bobTimer += (float)delta * 4.0f;
        Position = new Vector2(_basePosition.X, _basePosition.Y + Mathf.Sin(_bobTimer) * 3.0f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player.PlayerController player)
        {
            // Only pick up if player is missing health
            if (player.Stats.CurrentHealth < player.Stats.MaxHealth)
            {
                player.Heal(HealAmount);

                // Quick pop scale animation before removal
                var tween = CreateTween();
                tween.TweenProperty(this, "scale", Vector2.One * 1.4f, 0.1);
                tween.TweenProperty(this, "modulate:a", 0.0f, 0.1);
                tween.TweenCallback(Callable.From(QueueFree));
            }
        }
    }
}
