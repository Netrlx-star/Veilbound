using Godot;

namespace Veilbound.Items;

/// <summary>
/// An item pedestal typically placed in Treasure Rooms or Boss Rooms.
/// Displays an item floating above it that the player can pick up.
/// </summary>
public partial class ItemPedestal : Node2D
{
    public ItemData Item { get; set; }

    private Area2D _pickupArea;
    private Sprite2D _itemSprite;
    private Label _nameLabel;
    private float _floatTimer = 0.0f;
    private bool _collected = false;

    public override void _Ready()
    {
        Item ??= ItemData.GetRandomItem();

        _pickupArea = GetNode<Area2D>("PickupArea");
        _itemSprite = GetNode<Sprite2D>("ItemSprite");
        _nameLabel = GetNode<Label>("NameLabel");

        _itemSprite.Texture = Systems.PlaceholderSprite.CreateCircleTexture(12, Item.ItemColor);
        _nameLabel.Text = $"{Item.Name}\n{Item.Description}";
        _nameLabel.Visible = false;

        _pickupArea.BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (_collected) return;

        // Floating animation for the item
        _floatTimer += (float)delta * 3.0f;
        _itemSprite.Position = new Vector2(0, -28.0f + Mathf.Sin(_floatTimer) * 4.0f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected) return;

        if (body is Player.PlayerController player)
        {
            _collected = true;

            // Apply item bonuses to player
            player.Stats.Damage += Item.DamageBonus;
            player.Stats.FireRate = Mathf.Max(0.12f, player.Stats.FireRate - Item.FireRateReduction);
            player.Stats.MoveSpeed += Item.SpeedBonus;
            player.Stats.ProjectileRange += Item.RangeBonus;

            if (Item.MaxHealthBonus > 0)
            {
                player.Stats.MaxHealth += Item.MaxHealthBonus;
                player.Heal(Item.MaxHealthBonus);
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
        }
    }
}
