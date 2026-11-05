using Godot;

namespace Veilbound.Items;

/// <summary>
/// A consumable heart pickup that heals the player upon contact.
/// Features authentic pixel heart styling and floating bounce animation.
/// </summary>
public partial class HeartPickup : Area2D
{
    [Export] public int HealAmount { get; set; } = 2; // 2 half-hearts = 1 full red heart

    private float _bobTimer = 0.0f;
    private Vector2 _basePosition;
    private bool _initialized = false;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            _sprite.Texture = Systems.PlaceholderSprite.CreateHeartTexture(2);
            _sprite.Scale = new Vector2(1.6f, 1.6f);
        }

        BodyEntered += OnBodyEntered;

        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _initialized = true;
        }
    }

    public void Initialize(Vector2 worldPos)
    {
        GlobalPosition = worldPos;
        _basePosition = worldPos;
        _initialized = true;

        // Bouncy spawn animation (pop up and settle)
        Scale = Vector2.Zero;
        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.One, 0.28)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        Vector2 bounceTarget = worldPos + new Vector2((float)GD.RandRange(-16, 16), (float)GD.RandRange(-12, 12));
        _basePosition = bounceTarget;
        tween.TweenProperty(this, "global_position", bounceTarget, 0.28)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    public override void _Process(double delta)
    {
        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _initialized = true;
        }

        // Gentle floating bob
        _bobTimer += (float)delta * 4.5f;
        GlobalPosition = new Vector2(_basePosition.X, _basePosition.Y + Mathf.Sin(_bobTimer) * 3.5f);
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
                var tween = CreateTween().SetParallel(true);
                tween.TweenProperty(this, "scale", Vector2.One * 1.6f, 0.12);
                tween.TweenProperty(this, "modulate:a", 0.0f, 0.12);
                tween.Chain().TweenCallback(Callable.From(QueueFree));
            }
        }
    }
}
