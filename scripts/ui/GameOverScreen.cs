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

        Visible = false;
    }

    public void ShowDeathScreen(int floor, int rooms, int enemiesKilled, int itemsCollected)
    {
        Visible = true;

        if (_floorStatLabel != null) _floorStatLabel.Text = $"Floor: {floor}";
        if (_roomsStatLabel != null) _roomsStatLabel.Text = $"Rooms Cleared: {rooms}";
        if (_enemiesStatLabel != null) _enemiesStatLabel.Text = $"Enemies Vanquished: {enemiesKilled}";
        if (_itemsStatLabel != null) _itemsStatLabel.Text = $"Relics Collected: {itemsCollected}";

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
