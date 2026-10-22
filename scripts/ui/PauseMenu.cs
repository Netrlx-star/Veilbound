using Godot;

namespace Veilbound.UI;

/// <summary>
/// Dark fantasy pause menu overlay.
/// Stops gameplay process while active, allowing player to resume, restart, or quit.
/// </summary>
public partial class PauseMenu : Control
{
    private Button _resumeButton;
    private Button _restartButton;
    private Button _menuButton;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused; // Must process while tree is paused

        _resumeButton = GetNode<Button>("%ResumeButton");
        _restartButton = GetNode<Button>("%RestartButton");
        _menuButton = GetNode<Button>("%MenuButton");

        _resumeButton.Pressed += OnResumePressed;
        _restartButton.Pressed += OnRestartPressed;
        _menuButton.Pressed += OnMenuPressed;

        Visible = false;
    }

    public void Open()
    {
        Visible = true;
        GetTree().Paused = true;
        _resumeButton.GrabFocus();

        // Fade-in animation
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.2);
    }

    public void Close()
    {
        GetTree().Paused = false;
        Visible = false;
    }

    private void OnResumePressed()
    {
        Close();
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnMenuPressed()
    {
        GetTree().Paused = false;
        Systems.GameManager.Instance?.ReturnToMenu();
    }
}
