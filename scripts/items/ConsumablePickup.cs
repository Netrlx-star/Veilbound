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
    private CollisionShape2D _colShape;
    private Label _priceLabel;
    private float _bobTimer = 0.0f;
    private float _flashCooldown = 0.0f;
    private Vector2 _basePosition;
    private bool _initialized = false;
    private bool _collected = false;

    public override void _Ready()
    {
        CollisionLayer = 32; // items / pickups layer (bit 6)
        CollisionMask = 2;   // player layer (bit 2)
        Monitoring = true;
        Monitorable = true;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
        {
            _sprite = new Sprite2D { Name = "Sprite2D" };
            AddChild(_sprite);
        }

        _colShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_colShape == null)
        {
            _colShape = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new CircleShape2D { Radius = 18.0f }
            };
            AddChild(_colShape);
        }

        AssignTexture();
        UpdatePriceLabel();

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

        if (_sprite == null) return;

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

    private void UpdatePriceLabel()
    {
        if (Price > 0)
        {
            if (_priceLabel == null)
            {
                _priceLabel = new Label
                {
                    Name = "PriceLabel",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Position = new Vector2(-30, 16),
                    Size = new Vector2(60, 20)
                };
                _priceLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.2f));
                _priceLabel.AddThemeFontSizeOverride("font_size", 13);
                AddChild(_priceLabel);
            }
            _priceLabel.Text = $"{Price}¢";
            _priceLabel.Visible = true;
        }
        else if (_priceLabel != null)
        {
            _priceLabel.Visible = false;
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
        UpdatePriceLabel();

        // Never collapse Area2D physics scale to zero - keep Area2D scale at 1.0 always
        Scale = Vector2.One;

        if (price > 0)
        {
            // Shop item: clean stationary placement with gentle fade-in
            Modulate = new Color(1, 1, 1, 0);
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 1.0f, 0.22);
        }
        else
        {
            // Free drop: visual pop on sprite + slight position scatter
            if (_sprite != null)
            {
                Vector2 targetScale = _sprite.Scale;
                _sprite.Scale = Vector2.Zero;
                var tween = CreateTween().SetParallel(true);
                tween.TweenProperty(_sprite, "scale", targetScale, 0.28)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

                Vector2 bounceOffset = new Vector2((float)GD.RandRange(-16, 16), (float)GD.RandRange(-12, 12));
                Vector2 bounceTarget = worldPos + bounceOffset;
                _basePosition = bounceTarget;
                tween.TweenProperty(this, "global_position", bounceTarget, 0.28)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            }
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
        // Float only the visual sprite, keeping Area2D collision position rock steady
        if (_sprite != null)
        {
            _sprite.Position = new Vector2(0, Mathf.Sin(_bobTimer) * 3.0f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_flashCooldown > 0.0f)
        {
            _flashCooldown -= (float)delta;
        }

        if (_collected) return;

        // Continuous overlap check ensures instant collection upon contact
        var bodies = GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is Player.PlayerController player)
            {
                if (TryCollect(player))
                {
                    break;
                }
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player.PlayerController player)
        {
            TryCollect(player);
        }
    }

    public bool TryCollect(Player.PlayerController player)
    {
        if (_collected) return false;

        // If it's a shop item, verify player can afford it
        if (Price > 0)
        {
            if (player.Stats.Coins < Price)
            {
                if (_priceLabel != null && _flashCooldown <= 0.0f)
                {
                    _flashCooldown = 0.35f;
                    var flashTween = CreateTween();
                    flashTween.TweenProperty(_priceLabel, "modulate", new Color(1.0f, 0.2f, 0.2f), 0.1);
                    flashTween.TweenProperty(_priceLabel, "modulate", Colors.White, 0.15);
                }
                return false;
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

            if (_priceLabel != null)
            {
                _priceLabel.Visible = false;
            }

            // Pop and fade collect animation
            var tween = CreateTween().SetParallel(true);
            if (_sprite != null)
            {
                tween.TweenProperty(_sprite, "scale", _sprite.Scale * 1.5f, 0.12);
            }
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.12);
            tween.Chain().TweenCallback(Callable.From(QueueFree));
            return true;
        }

        return false;
    }
}
