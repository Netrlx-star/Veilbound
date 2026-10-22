using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// Advanced in-game HUD inspired by The Binding of Isaac.
/// Displays modular pixel heart containers, low-health heartbeat pulsation,
/// consumables (coins, bombs, keys), stat tracker (Found HUD), minimap,
/// boss health bar, and pause/game-over overlays.
/// </summary>
public partial class HUD : CanvasLayer
{
    // ── Health ──
    private HBoxContainer _heartsContainer;
    private Tween _heartbeatTween;

    // ── Consumables ──
    private Label _coinLabel;
    private Label _bombLabel;
    private Label _keyLabel;
    private TextureRect _coinIcon;
    private TextureRect _bombIcon;
    private TextureRect _keyIcon;

    // ── Stat Tracker (Found HUD) ──
    private Label _damageLabel;
    private Label _tearsLabel;
    private Label _speedLabel;
    private Label _rangeLabel;

    // ── Floor & Rooms ──
    private Label _floorLabel;
    private Label _roomsLabel;

    // ── Minimap ──
    private Minimap _minimap;

    // ── Boss Bar ──
    private Control _bossBarContainer;
    private Label _bossNameLabel;
    private ProgressBar _bossProgressBar;

    // ── Overlays ──
    private PauseMenu _pauseMenu;
    private GameOverScreen _gameOverScreen;

    public override void _Ready()
    {
        _heartsContainer = GetNode<HBoxContainer>("%HeartsContainer");

        // Consumables
        _coinLabel = GetNodeOrNull<Label>("%CoinLabel");
        _bombLabel = GetNodeOrNull<Label>("%BombLabel");
        _keyLabel = GetNodeOrNull<Label>("%KeyLabel");
        _coinIcon = GetNodeOrNull<TextureRect>("%CoinIcon");
        _bombIcon = GetNodeOrNull<TextureRect>("%BombIcon");
        _keyIcon = GetNodeOrNull<TextureRect>("%KeyIcon");

        // Initialize icons with procedural retro textures
        if (_coinIcon != null) _coinIcon.Texture = Systems.PlaceholderSprite.CreateCoinTexture();
        if (_bombIcon != null) _bombIcon.Texture = Systems.PlaceholderSprite.CreateBombTexture();
        if (_keyIcon != null) _keyIcon.Texture = Systems.PlaceholderSprite.CreateKeyTexture();

        // Stat tracker
        _damageLabel = GetNodeOrNull<Label>("%DamageLabel");
        _tearsLabel = GetNodeOrNull<Label>("%TearsLabel");
        _speedLabel = GetNodeOrNull<Label>("%SpeedLabel");
        _rangeLabel = GetNodeOrNull<Label>("%RangeLabel");

        // Floor info
        _floorLabel = GetNode<Label>("%FloorLabel");
        _roomsLabel = GetNode<Label>("%RoomsLabel");

        // Minimap & Boss
        _minimap = GetNodeOrNull<Minimap>("%Minimap");
        _bossBarContainer = GetNodeOrNull<Control>("%BossBarContainer");
        _bossNameLabel = GetNodeOrNull<Label>("%BossNameLabel");
        _bossProgressBar = GetNodeOrNull<ProgressBar>("%BossProgressBar");
        if (_bossBarContainer != null) _bossBarContainer.Visible = false;

        // Overlays
        _pauseMenu = GetNodeOrNull<PauseMenu>("%PauseMenu");
        _gameOverScreen = GetNodeOrNull<GameOverScreen>("%GameOverScreen");

        UpdateFloorDisplay(1);
        UpdateRoomsDisplay(0);
        UpdateConsumables(0, 1, 1);
    }

    // ══════════════════════════════════════════════
    //  HEALTH SYSTEM
    // ══════════════════════════════════════════════

    /// <summary>
    /// Renders modular heart containers using authentic pixel textures.
    /// Triggers low-health pulsation when at or below 1 full heart.
    /// </summary>
    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Clear old hearts
        foreach (var child in _heartsContainer.GetChildren())
        {
            child.QueueFree();
        }

        int totalContainers = Mathf.CeilToInt(maxHealth / 2.0f);

        for (int i = 0; i < totalContainers; i++)
        {
            int heartValue = currentHealth - (i * 2);
            int state = 0; // 0 = empty, 1 = half, 2 = full

            if (heartValue >= 2) state = 2;
            else if (heartValue == 1) state = 1;
            else state = 0;

            var heartRect = new TextureRect
            {
                Texture = Systems.PlaceholderSprite.CreateHeartTexture(state),
                CustomMinimumSize = new Vector2(28, 24),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
            };

            _heartsContainer.AddChild(heartRect);
        }

        // Low health heartbeat effect
        HandleLowHealthPulsation(currentHealth);
    }

    private void HandleLowHealthPulsation(int currentHealth)
    {
        _heartbeatTween?.Kill();

        if (currentHealth > 0 && currentHealth <= 2)
        {
            _heartbeatTween = CreateTween().SetLoops();
            _heartbeatTween.TweenProperty(_heartsContainer, "scale", new Vector2(1.15f, 1.15f), 0.1)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            _heartbeatTween.TweenProperty(_heartsContainer, "scale", Vector2.One, 0.1)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            _heartbeatTween.TweenProperty(_heartsContainer, "scale", new Vector2(1.08f, 1.08f), 0.08)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            _heartbeatTween.TweenProperty(_heartsContainer, "scale", Vector2.One, 0.12)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            _heartbeatTween.TweenInterval(0.6);
        }
        else
        {
            _heartsContainer.Scale = Vector2.One;
        }
    }

    // ══════════════════════════════════════════════
    //  CONSUMABLES & STATS
    // ══════════════════════════════════════════════

    public void UpdateConsumables(int coins, int bombs, int keys)
    {
        if (_coinLabel != null) _coinLabel.Text = coins.ToString("D2");
        if (_bombLabel != null) _bombLabel.Text = bombs.ToString("D2");
        if (_keyLabel != null) _keyLabel.Text = keys.ToString("D2");
    }

    public void UpdateStats(float damage, float fireRate, float speed, float range)
    {
        if (_damageLabel != null) _damageLabel.Text = $"DMG: {damage:F1}";
        if (_tearsLabel != null) _tearsLabel.Text = $"RATE: {(1.0f / fireRate):F1}/s";
        if (_speedLabel != null) _speedLabel.Text = $"SPD: {(speed / 200.0f):F2}";
        if (_rangeLabel != null) _rangeLabel.Text = $"RNG: {range:F0}";
    }

    public void UpdateFloorDisplay(int floor)
    {
        if (_floorLabel != null) _floorLabel.Text = $"Floor {floor}";
    }

    public void UpdateRoomsDisplay(int rooms)
    {
        if (_roomsLabel != null) _roomsLabel.Text = $"Rooms: {rooms}";
    }

    public void UpdateMinimap(int[,] grid, HashSet<Vector2I> visited, Vector2I currentPos)
    {
        _minimap?.Refresh(grid, visited, currentPos);
    }

    // ══════════════════════════════════════════════
    //  BOSS BAR & OVERLAYS
    // ══════════════════════════════════════════════

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
        if (_bossBarContainer != null) _bossBarContainer.Visible = false;
    }

    public void TogglePauseMenu()
    {
        if (_pauseMenu == null) return;
        if (_pauseMenu.Visible) _pauseMenu.Close();
        else _pauseMenu.Open();
    }

    public void ShowGameOver(int floor, int rooms, int enemiesKilled, int itemsCollected)
    {
        _gameOverScreen?.ShowDeathScreen(floor, rooms, enemiesKilled, itemsCollected);
    }
}
