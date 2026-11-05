using Godot;

namespace Veilbound.UI;

/// <summary>
/// Atmospheric gothic Main Menu screen.
/// Features floating glowing title and interactive button hover tweens.
/// </summary>
public partial class MainMenu : Control
{
    private Button _playButton;
    private Button _characterSelectBtn;
    private Button _settingsButton;
    private Button _exitButton;
    private Label _titleLabel;
    private SettingsMenu _settingsMenu;

    private float _titleTimer = 0.0f;
    private Vector2 _titleBasePos;

    private static readonly Color DefaultButtonColor = new(0.85f, 0.8f, 0.9f);
    private static readonly Color HoverButtonColor = new(1.0f, 0.9f, 0.5f);

    public override void _Ready()
    {
        _playButton = GetNode<Button>("%PlayButton");
        _characterSelectBtn = GetNodeOrNull<Button>("%CharacterSelectBtn");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _exitButton = GetNode<Button>("%ExitButton");
        _titleLabel = GetNodeOrNull<Label>("%TitleLabel");
        _settingsMenu = GetNodeOrNull<SettingsMenu>("%SettingsMenu");

        if (_titleLabel != null)
        {
            _titleBasePos = _titleLabel.Position;
        }

        _playButton.Pressed += OnPlayPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _exitButton.Pressed += OnExitPressed;

        if (_characterSelectBtn != null)
        {
            UpdateCharacterSelectButtonText();
            _characterSelectBtn.Pressed += OnCharacterSelectPressed;
            SetupButtonHoverEffects(_characterSelectBtn);
        }

        SetupButtonHoverEffects(_playButton);
        SetupButtonHoverEffects(_settingsButton);
        SetupButtonHoverEffects(_exitButton);

        // Fade in effect
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.6f);
    }

    public override void _Process(double delta)
    {
        // Gentle floating animation for Title
        if (_titleLabel != null)
        {
            _titleTimer += (float)delta * 2.0f;
            _titleLabel.Position = new Vector2(_titleBasePos.X, _titleBasePos.Y + Mathf.Sin(_titleTimer) * 4.0f);
        }
    }

    private void SetupButtonHoverEffects(Button button)
    {
        button.PivotOffset = button.Size / 2.0f;
        button.Modulate = DefaultButtonColor;

        button.MouseEntered += () => AnimateButtonHover(button, true);
        button.FocusEntered += () => AnimateButtonHover(button, true);
        button.MouseExited += () => AnimateButtonHover(button, false);
        button.FocusExited += () => AnimateButtonHover(button, false);
    }

    private void AnimateButtonHover(Button button, bool hovered)
    {
        button.PivotOffset = button.Size / 2.0f;
        var tween = CreateTween().SetParallel(true);

        if (hovered)
        {
            tween.TweenProperty(button, "scale", new Vector2(1.06f, 1.06f), 0.12)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "modulate", HoverButtonColor, 0.12);
        }
        else
        {
            tween.TweenProperty(button, "scale", Vector2.One, 0.15)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "modulate", DefaultButtonColor, 0.15);
        }
    }

    private void OnCharacterSelectPressed()
    {
        var gm = Systems.GameManager.Instance;
        if (gm == null) return;

        gm.SelectedClass = (gm.SelectedClass == Systems.PlayerClass.Wanderer)
            ? Systems.PlayerClass.Bladesmith
            : Systems.PlayerClass.Wanderer;

        UpdateCharacterSelectButtonText();
    }

    private void UpdateCharacterSelectButtonText()
    {
        if (_characterSelectBtn == null) return;
        var gm = Systems.GameManager.Instance;
        bool isBladesmith = gm?.SelectedClass == Systems.PlayerClass.Bladesmith;

        _characterSelectBtn.Text = isBladesmith
            ? "Class: ⚔️ Bladesmith (Melee)"
            : "Class: 🏹 Wanderer (Ranged)";
    }

    private void OnPlayPressed()
    {
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnSettingsPressed()
    {
        _settingsMenu?.Open();
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
