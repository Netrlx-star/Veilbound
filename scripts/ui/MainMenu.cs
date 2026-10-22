using Godot;

namespace Veilbound.UI;

/// <summary>
/// Main Menu screen. Displays the game title and navigation buttons.
/// </summary>
public partial class MainMenu : Control
{
    private Button _playButton;
    private Button _settingsButton;
    private Button _exitButton;

    public override void _Ready()
    {
        _playButton = GetNode<Button>("%PlayButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _exitButton = GetNode<Button>("%ExitButton");

        _playButton.Pressed += OnPlayPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _exitButton.Pressed += OnExitPressed;

        // Fade in effect
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.5f);
    }

    private void OnPlayPressed()
    {
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnSettingsPressed()
    {
        // TODO: Open settings menu
        GD.Print("Settings not yet implemented");
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
