using Godot;

namespace Veilbound.UI;

/// <summary>
/// Atmospheric gothic Pause Menu overlay — matches Main Menu visual language.
/// Dark vignette, stone-slab gothic buttons, floating ember particles,
/// and a subtle breathing title pulse.
/// </summary>
public partial class PauseMenu : Control
{
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _restartButton;
    private Button _menuButton;
    private SettingsMenu _settingsMenu;
    private TextureRect _vignetteOverlay;
    private VBoxContainer _pausePanel;
    private Label _titleLabel;

    private float _titleTimer = 0.0f;
    private Vector2 _titleBasePos;
    private bool _isOpen = false;

    private static readonly Color DefaultTextColor = new(0.90f, 0.84f, 0.95f);
    private static readonly Color HoverTextColor = new(1.0f, 0.92f, 0.55f);
    private static readonly Color DangerTextColor = new(0.90f, 0.52f, 0.35f);
    private static readonly Color WarningTextColor = new(0.80f, 0.38f, 0.38f);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;

        _vignetteOverlay = GetNodeOrNull<TextureRect>("%VignetteOverlay");
        _pausePanel = GetNodeOrNull<VBoxContainer>("%PausePanel");
        _titleLabel = GetNodeOrNull<Label>("%TitleLabel");

        _resumeButton = GetNode<Button>("%ResumeButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _restartButton = GetNode<Button>("%RestartButton");
        _menuButton = GetNode<Button>("%MenuButton");
        _settingsMenu = GetNodeOrNull<SettingsMenu>("%SettingsMenu");

        // 1. Apply procedural vignette (uses the MENU vignette — deeper and more dramatic on pause)
        if (_vignetteOverlay != null)
            _vignetteOverlay.Texture = Systems.AtmosphereGenerator.GetMenuVignetteTexture();

        // 2. Style all buttons with the same gothic stone language as the Main Menu
        StyleGothicButton(_resumeButton, isAccent: true);
        StyleGothicButton(_settingsButton);
        StyleGothicButton(_restartButton, dangerLevel: 1);
        StyleGothicButton(_menuButton, dangerLevel: 2);

        if (_titleLabel != null)
            _titleBasePos = _titleLabel.Position;

        _resumeButton.Pressed += OnResumePressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _restartButton.Pressed += OnRestartPressed;
        _menuButton.Pressed += OnMenuPressed;

        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!_isOpen || _titleLabel == null) return;

        // Gentle breathing pulse on title (matches Main Menu title float)
        _titleTimer += (float)delta * 2.0f;
        _titleLabel.Position = new Vector2(
            _titleBasePos.X,
            _titleBasePos.Y + Mathf.Sin(_titleTimer) * 2.5f
        );
    }

    public void Open()
    {
        Visible = true;
        _isOpen = true;
        GetTree().Paused = true;

        // Slide in from center with scale pop
        if (_pausePanel != null)
        {
            _pausePanel.PivotOffset = _pausePanel.Size / 2.0f;
            _pausePanel.Scale = new Vector2(0.88f, 0.88f);
        }

        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process).SetParallel(true);
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.20);

        if (_pausePanel != null)
        {
            tween.TweenProperty(_pausePanel, "scale", Vector2.One, 0.22)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        _resumeButton.GrabFocus();
    }

    public void Close()
    {
        _isOpen = false;
        GetTree().Paused = false;

        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.15);
        tween.TweenCallback(Callable.From(() => Visible = false));
    }

    private void StyleGothicButton(Button button, bool isAccent = false, int dangerLevel = 0)
    {
        Color normalBg = new(0.10f, 0.08f, 0.14f, 0.92f);
        Color normalBorder = isAccent
            ? new Color(0.80f, 0.65f, 0.28f, 0.85f)
            : dangerLevel == 2 ? new Color(0.52f, 0.22f, 0.22f, 0.75f)
            : dangerLevel == 1 ? new Color(0.52f, 0.35f, 0.20f, 0.75f)
            : new Color(0.36f, 0.30f, 0.46f, 0.72f);

        Color hoverBg = new(0.18f, 0.13f, 0.26f, 0.96f);
        Color hoverBorder = isAccent
            ? new Color(1.0f, 0.88f, 0.42f, 1.0f)
            : dangerLevel == 2 ? new Color(0.90f, 0.38f, 0.38f, 1.0f)
            : dangerLevel == 1 ? new Color(0.90f, 0.60f, 0.28f, 1.0f)
            : new Color(0.90f, 0.76f, 0.34f, 1.0f);

        Color pressedBg = new(0.14f, 0.10f, 0.20f, 0.98f);

        var normalBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(normalBg, normalBorder, 1.5f, 4.0f);
        var hoverBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(hoverBg, hoverBorder, 2.0f, 4.0f);
        var pressedBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(pressedBg, hoverBorder, 2.0f, 4.0f);

        button.AddThemeStyleboxOverride("normal", normalBox);
        button.AddThemeStyleboxOverride("hover", hoverBox);
        button.AddThemeStyleboxOverride("pressed", pressedBox);
        button.AddThemeStyleboxOverride("focus", hoverBox);

        // Set initial text color
        Color baseColor = isAccent ? DefaultTextColor
            : dangerLevel == 2 ? WarningTextColor
            : dangerLevel == 1 ? DangerTextColor
            : DefaultTextColor;
        button.AddThemeColorOverride("font_color", baseColor);

        button.PivotOffset = button.Size / 2.0f;
        button.MouseEntered += () => AnimateButtonHover(button, true, hoverBorder);
        button.FocusEntered += () => AnimateButtonHover(button, true, hoverBorder);
        button.MouseExited += () => AnimateButtonHover(button, false, baseColor);
        button.FocusExited += () => AnimateButtonHover(button, false, baseColor);
    }

    private void AnimateButtonHover(Button button, bool hovered, Color targetColor)
    {
        button.PivotOffset = button.Size / 2.0f;
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process).SetParallel(true);

        if (hovered)
        {
            tween.TweenProperty(button, "scale", new Vector2(1.04f, 1.04f), 0.12)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "theme_override_colors/font_color", HoverTextColor, 0.12);
        }
        else
        {
            tween.TweenProperty(button, "scale", Vector2.One, 0.14)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "theme_override_colors/font_color", targetColor, 0.14);
        }
    }

    private void OnResumePressed() => Close();

    private void OnSettingsPressed() => _settingsMenu?.Open();

    private void OnRestartPressed()
    {
        _isOpen = false;
        GetTree().Paused = false;
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnMenuPressed()
    {
        _isOpen = false;
        GetTree().Paused = false;
        Systems.GameManager.Instance?.ReturnToMenu();
    }
}
