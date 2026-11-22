using Godot;

namespace Veilbound.UI;

/// <summary>
/// Game Over death screen.
/// Summarizes the player's run statistics and allows immediate replay.
/// </summary>
public partial class GameOverScreen : Control
{
    private Label _floorStatLabel;
    private Label _roomsStatLabel;
    private Label _enemiesStatLabel;
    private Label _itemsStatLabel;

    private Button _restartButton;
    private Button _menuButton;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _floorStatLabel = GetNode<Label>("%FloorStatLabel");
        _roomsStatLabel = GetNode<Label>("%RoomsStatLabel");
        _enemiesStatLabel = GetNode<Label>("%EnemiesStatLabel");
        _itemsStatLabel = GetNode<Label>("%ItemsStatLabel");

        _restartButton = GetNode<Button>("%RestartButton");
        _menuButton = GetNode<Button>("%MenuButton");

        _restartButton.Pressed += OnRestartPressed;
        _menuButton.Pressed += OnMenuPressed;

        StyleModal();
        Visible = false;
    }

    private void StyleModal()
    {
        var panel = GetNodeOrNull<PanelContainer>("CenterContainer/PanelContainer");
        if (panel != null)
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.08f, 0.06f, 0.12f, 0.96f),
                BorderColor = new Color(0.70f, 0.18f, 0.28f, 0.95f),
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            panel.AddThemeStyleboxOverride("panel", style);
        }

        StyleButton(_restartButton, new Color(0.65f, 0.15f, 0.25f, 0.9f), new Color(0.90f, 0.25f, 0.35f, 1.0f));
        StyleButton(_menuButton, new Color(0.18f, 0.15f, 0.25f, 0.9f), new Color(0.35f, 0.30f, 0.45f, 1.0f));
    }

    private static void StyleButton(Button btn, Color normalBg, Color hoverBg)
    {
        if (btn == null) return;

        var normalStyle = new StyleBoxFlat
        {
            BgColor = normalBg,
            BorderColor = new Color(0.85f, 0.70f, 0.25f, 0.8f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = hoverBg,
            BorderColor = new Color(1.0f, 0.88f, 0.40f, 1.0f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };

        btn.AddThemeStyleboxOverride("normal", normalStyle);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", normalStyle);
        btn.AddThemeColorOverride("font_color", new Color(1.0f, 0.95f, 0.85f));
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
    }

    public void ShowDeathScreen(int floor, int rooms, int enemiesKilled, int itemsCollected)
    {
        Visible = true;

        if (_floorStatLabel != null) _floorStatLabel.Text = $"🏛️  Depth: Floor {floor} (The Crypt)";
        if (_roomsStatLabel != null) _roomsStatLabel.Text = $"🗝️  Chambers Purged: {rooms}";
        if (_enemiesStatLabel != null) _enemiesStatLabel.Text = $"⚔️  Foes Vanquished: {enemiesKilled}";
        if (_itemsStatLabel != null) _itemsStatLabel.Text = $"🔮  Occult Relics Harvested: {itemsCollected}";

        _restartButton.GrabFocus();

        // Dramatic red vignette fade-in
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.6);
    }

    private void OnRestartPressed()
    {
        Visible = false;
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnMenuPressed()
    {
        Visible = false;
        Systems.GameManager.Instance?.ReturnToMenu();
    }
}
