using Godot;

namespace Veilbound.Items;

public enum ConsumableType
{
    Heart,
    Coin,
    Bomb,
    Key
}

/// <summary>
/// Universal pickup entity for dropped consumables (Heart, Coin, Bomb, Key).
/// Features bouncy spawn animation, floating idle bob, and collision collection.
/// Can be configured as a shop item with a coin price tag.
/// </summary>
public partial class ConsumablePickup : Area2D
{
    [Export] public ConsumableType Type { get; set; } = ConsumableType.Coin;
    [Export] public int Value { get; set; } = 1;
    [Export] public int Price { get; set; } = 0; // 0 = free pickup, > 0 = shop purchase

    private Sprite2D _sprite;
    private Label _priceLabel;
    private float _bobTimer = 0.0f;
    private Vector2 _basePosition;
    private bool _initialized = false;
    private bool _collected = false;

    public override void _Ready()
    {
        CollisionLayer = 32; // items / pickups layer (bit 6)
        CollisionMask = 2;   // player layer (bit 2)

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        var colShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (colShape == null)
        {
            colShape = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new CircleShape2D { Radius = 12.0f }
            };
            AddChild(colShape);
        }

        AssignTexture();

        // Price label for shop items
        if (Price > 0)
        {
            _priceLabel = new Label
            {
                Name = "PriceLabel",
                Text = $"{Price}¢",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(-25, 14),
                Size = new Vector2(50, 20)
            };
            _priceLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.2f));
            _priceLabel.AddThemeFontSizeOverride("font_size", 12);
            AddChild(_priceLabel);
        }

        BodyEntered += OnBodyEntered;

        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _initialized = true;
        }
    }

    private void AssignTexture()
    {
        Systems.AssetGenerator.InitializeAssets();

        switch (Type)
        {
            case ConsumableType.Heart:
                _sprite.Texture = Systems.PlaceholderSprite.CreateHeartTexture(2);
                _sprite.Scale = new Vector2(1.5f, 1.5f);
                break;
            case ConsumableType.Coin:
                _sprite.Texture = Systems.PlaceholderSprite.CreateCoinTexture();
                _sprite.Scale = new Vector2(1.6f, 1.6f);
                break;
            case ConsumableType.Bomb:
                _sprite.Texture = Systems.PlaceholderSprite.CreateBombTexture();
                _sprite.Scale = new Vector2(1.6f, 1.6f);
                break;
            case ConsumableType.Key:
                _sprite.Texture = Systems.PlaceholderSprite.CreateKeyTexture();
                _sprite.Scale = new Vector2(1.5f, 1.5f);
                break;
        }
    }

    public void Initialize(Vector2 worldPos, ConsumableType type, int price = 0, int value = 1)
    {
        Type = type;
        Price = price;
        Value = value;
        GlobalPosition = worldPos;
        _basePosition = worldPos;
        _initialized = true;

        AssignTexture();

        // Bouncy spawn pop animation
        Scale = Vector2.Zero;
        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.One, 0.28)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        if (price == 0)
        {
            Vector2 bounceOffset = new Vector2((float)GD.RandRange(-16, 16), (float)GD.RandRange(-12, 12));
            Vector2 bounceTarget = worldPos + bounceOffset;
            _basePosition = bounceTarget;
            tween.TweenProperty(this, "global_position", bounceTarget, 0.28)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
    }

    public override void _Process(double delta)
    {
        if (_collected) return;

        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _initialized = true;
        }

        _bobTimer += (float)delta * 4.5f;
        // Subtle floating bob
        GlobalPosition = new Vector2(_basePosition.X, _basePosition.Y + Mathf.Sin(_bobTimer) * 3.0f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected) return;

        if (body is Player.PlayerController player)
        {
            // If it's a shop item, verify player can afford it
            if (Price > 0)
            {
                if (player.Stats.Coins < Price)
                {
                    // Flash price red to indicate lack of funds
                    if (_priceLabel != null)
                    {
                        var flashTween = CreateTween();
                        flashTween.TweenProperty(_priceLabel, "modulate", new Color(1, 0.2f, 0.2f), 0.1);
                        flashTween.TweenProperty(_priceLabel, "modulate", Colors.White, 0.15);
                    }
                    return;
                }

                // Deduct coins
                player.Stats.Coins -= Price;
                player.NotifyStatsChanged();
            }

            // Verify if pickup can be collected
            bool collectedSuccessfully = false;

            switch (Type)
            {
                case ConsumableType.Heart:
                    if (player.Stats.CurrentHealth < player.Stats.MaxHealth || Price > 0)
                    {
                        player.Heal(Value * 2); // 1 Heart value = 2 half-hearts
                        collectedSuccessfully = true;
                    }
                    break;

                case ConsumableType.Coin:
                    player.AddCoins(Value);
                    collectedSuccessfully = true;
                    break;

                case ConsumableType.Bomb:
                    player.AddBombs(Value);
                    collectedSuccessfully = true;
                    break;

                case ConsumableType.Key:
                    player.AddKeys(Value);
                    collectedSuccessfully = true;
                    break;
            }

            if (collectedSuccessfully)
            {
                _collected = true;

                // Pop and fade collect animation
                var tween = CreateTween().SetParallel(true);
                tween.TweenProperty(this, "scale", Vector2.One * 1.5f, 0.12);
                tween.TweenProperty(this, "modulate:a", 0.0f, 0.12);
                tween.Chain().TweenCallback(Callable.From(QueueFree));
            }
        }
    }
}
