using Godot;

namespace Veilbound.Items;

/// <summary>
/// An item pedestal typically placed in Treasure Rooms or Boss Rooms.
/// Displays an item floating above it that the player can pick up.
/// </summary>
public partial class ItemPedestal : Node2D
{
    public ItemData Item { get; set; }
    public ActiveItemData ActiveItem { get; set; }
    [Export] public int Price { get; set; } = 0; // 0 = free, > 0 = shop item

    private Area2D _pickupArea;
    private Sprite2D _itemSprite;
    private Label _nameLabel;
    private Label _priceLabel;
    private float _floatTimer = 0.0f;
    private float _flashCooldown = 0.0f;
    private bool _collected = false;

    public override void _Ready()
    {
        // 25% chance to roll an Active Spacebar Item instead of a passive item
        if (Item == null && ActiveItem == null)
        {
            if (GD.Randf() < 0.25f)
            {
                ActiveItem = ActiveItemData.GetRandomActiveItem();
            }
            else
            {
                Item = ItemData.GetRandomItem();
            }
        }

        _pickupArea = GetNode<Area2D>("PickupArea");
        _itemSprite = GetNode<Sprite2D>("ItemSprite");
        _nameLabel = GetNode<Label>("NameLabel");

        Color itemColor = ActiveItem != null ? ActiveItem.ItemColor : (Item != null ? Item.ItemColor : Colors.White);
        string itemName = ActiveItem != null ? ActiveItem.Name : (Item != null ? Item.Name : "Item");
        string itemDesc = ActiveItem != null ? ActiveItem.Description : (Item != null ? Item.Description : "");

        _itemSprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(12, itemColor);
        _nameLabel.Text = $"{itemName}\n{itemDesc}";
        _nameLabel.Visible = false;

        if (Price > 0)
        {
            _priceLabel = new Label
            {
                Name = "PriceLabel",
                Text = $"{Price}¢",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(-30, 16),
                Size = new Vector2(60, 20)
            };
            _priceLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.2f));
            _priceLabel.AddThemeFontSizeOverride("font_size", 13);
            AddChild(_priceLabel);
        }

        _pickupArea.BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (_collected) return;

        // Floating animation for the item
        _floatTimer += (float)delta * 3.0f;
        _itemSprite.Position = new Vector2(0, -28.0f + Mathf.Sin(_floatTimer) * 4.0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_flashCooldown > 0.0f)
        {
            _flashCooldown -= (float)delta;
        }

        if (_collected || _pickupArea == null) return;

        var bodies = _pickupArea.GetOverlappingBodies();
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

        if (Price > 0)
        {
            if (player.Stats.Coins < Price)
            {
                if (_priceLabel != null && _flashCooldown <= 0.0f)
                {
                    _flashCooldown = 0.35f;
                    var flashTween = CreateTween();
                    flashTween.TweenProperty(_priceLabel, "modulate", new Color(1, 0.2f, 0.2f), 0.1);
                    flashTween.TweenProperty(_priceLabel, "modulate", Colors.White, 0.15);
                }
                return false;
            }

            player.Stats.Coins -= Price;
            if (_priceLabel != null)
            {
                _priceLabel.Visible = false;
            }
        }

        _collected = true;

        if (ActiveItem != null)
        {
            // Equip active item
            player.SetActiveItem(ActiveItem);
            string activeSubtitle = $"{ActiveItem.Description}\n[SPACEBAR to Activate · {ActiveItem.MaxCharges} Charges]";
            UI.HUD.Instance?.ShowItemBanner(ActiveItem.Name, activeSubtitle, ActiveItem.ItemColor);
        }
        else if (Item != null)
        {
            // Apply passive item bonuses to player
            player.Stats.Damage += Item.DamageBonus;
            player.Stats.FireRate = Mathf.Max(0.12f, player.Stats.FireRate - Item.FireRateReduction);
            player.Stats.MoveSpeed += Item.SpeedBonus;
            player.Stats.ProjectileRange += Item.RangeBonus;
            player.Stats.Multishot += Item.MultishotBonus;

            if (Item.GrantsPiercing) player.Stats.HasPiercing = true;
            if (Item.GrantsBouncing) player.Stats.HasBouncing = true;
            if (Item.InflictsStatus != Enemies.StatusEffectType.None) player.Stats.InflictedStatus = Item.InflictsStatus;

            if (Item.MaxHealthBonus > 0)
            {
                player.Stats.MaxHealth += Item.MaxHealthBonus;
                player.Heal(Item.MaxHealthBonus);
            }

            string statSummary = Item.GetStatSummary();
            string bannerDesc = !string.IsNullOrEmpty(statSummary) ? $"{Item.Description}\n{statSummary}" : Item.Description;
            UI.HUD.Instance?.ShowItemBanner(Item.Name, bannerDesc, Item.ItemColor);
            UI.HUD.Instance?.AddPassiveRelic(Item);
        }

            player.Stats.ItemsCollected++;
            player.NotifyStatsChanged();

        // Show floating label
        _nameLabel.Visible = true;
        _nameLabel.Position = new Vector2(-75, -70);

        // Item pickup animation (float up and fade out)
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_itemSprite, "position:y", _itemSprite.Position.Y - 30.0f, 0.8);
        tween.TweenProperty(_itemSprite, "modulate:a", 0.0f, 0.8);
        tween.TweenProperty(_nameLabel, "position:y", _nameLabel.Position.Y - 20.0f, 1.2);
        tween.TweenProperty(_nameLabel, "modulate:a", 0.0f, 1.2);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            _itemSprite.Visible = false;
            _nameLabel.Visible = false;
        }));

        return true;
    }
}
