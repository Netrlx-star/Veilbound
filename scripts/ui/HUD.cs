using Godot;

namespace Veilbound.UI;

/// <summary>
/// In-game HUD. Displays player health (hearts), current floor,
/// and rooms cleared count.
/// </summary>
public partial class HUD : CanvasLayer
{
    private HBoxContainer _heartsContainer;
    private Label _floorLabel;
    private Label _roomsLabel;
    private Minimap _minimap;

    // Boss Bar
    private Control _bossBarContainer;
    private Label _bossNameLabel;
    private ProgressBar _bossProgressBar;

    // Heart colors
    private static readonly Color FullHeartColor = new(0.9f, 0.15f, 0.15f);
    private static readonly Color HalfHeartColor = new(0.9f, 0.15f, 0.15f, 0.5f);
    private static readonly Color EmptyHeartColor = new(0.3f, 0.3f, 0.3f, 0.5f);

    public override void _Ready()
    {
        _heartsContainer = GetNode<HBoxContainer>("%HeartsContainer");
        _floorLabel = GetNode<Label>("%FloorLabel");
        _roomsLabel = GetNode<Label>("%RoomsLabel");
        _minimap = GetNodeOrNull<Minimap>("%Minimap");

        _bossBarContainer = GetNodeOrNull<Control>("%BossBarContainer");
        _bossNameLabel = GetNodeOrNull<Label>("%BossNameLabel");
        _bossProgressBar = GetNodeOrNull<ProgressBar>("%BossProgressBar");

        if (_bossBarContainer != null) _bossBarContainer.Visible = false;

        UpdateFloorDisplay(1);
        UpdateRoomsDisplay(0);
    }

    public void ShowBossBar(string name, float current, float max)
    {
        if (_bossBarContainer == null) return;
        _bossBarContainer.Visible = true;
        if (_bossNameLabel != null) _bossNameLabel.Text = name;
        if (_bossProgressBar != null)
        {
            _bossProgressBar.MaxValue = max;
            _bossProgressBar.Value = current;
        }
    }

    public void HideBossBar()
    {
        if (_bossBarContainer != null)
        {
            _bossBarContainer.Visible = false;
        }
    }

    public void UpdateMinimap(int[,] grid, System.Collections.Generic.HashSet<Vector2I> visited, Vector2I currentPos)
    {
        _minimap?.Refresh(grid, visited, currentPos);
    }

    /// <summary>
    /// Update the hearts display based on current/max health.
    /// Health is in half-hearts (6 = 3 full hearts).
    /// </summary>
    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Clear existing hearts
        foreach (var child in _heartsContainer.GetChildren())
        {
            child.QueueFree();
        }

        int fullHearts = maxHealth / 2;

        for (int i = 0; i < fullHearts; i++)
        {
            var heartRect = new ColorRect
            {
                CustomMinimumSize = new Vector2(28, 28),
                Size = new Vector2(28, 28)
            };

            int healthForThisHeart = currentHealth - (i * 2);

            if (healthForThisHeart >= 2)
            {
                heartRect.Color = FullHeartColor;        // Full heart
            }
            else if (healthForThisHeart == 1)
            {
                heartRect.Color = HalfHeartColor;        // Half heart
            }
            else
            {
                heartRect.Color = EmptyHeartColor;       // Empty heart
            }

            _heartsContainer.AddChild(heartRect);
        }
    }

    public void UpdateFloorDisplay(int floor)
    {
        if (_floorLabel != null)
            _floorLabel.Text = $"Floor {floor}";
    }

    public void UpdateRoomsDisplay(int rooms)
    {
        if (_roomsLabel != null)
            _roomsLabel.Text = $"Rooms: {rooms}";
    }
}
