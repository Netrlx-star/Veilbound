using Godot;

namespace Veilbound.UI;

/// <summary>
/// Atmospheric gothic Main Menu screen.
/// Features a dedicated Heroic Altar preview with animated character sprite,
/// detailed class stats breakdown, drifting ember particles,
/// gothic styled buttons, subtle mouse parallax, and diploma project watermark.
/// Inspired by Hollow Knight, Darkest Dungeon, and Blasphemous.
/// </summary>
public partial class MainMenu : Control
{
    private Button _continueButton;
    private Button _playButton;
    private Button _characterSelectBtn;
    private Button _settingsButton;
    private Button _exitButton;

    private Label _titleLabel;
    private SettingsMenu _settingsMenu;

    private Control _parallaxRoot;
    private TextureRect _vignetteOverlay;
    private TextureRect _pedestalSprite;
    private TextureRect _heroSprite;
    private Label _heroTitleLabel;
    private Label _heroSubtitleLabel;
    private PanelContainer _heroStatsPanel;
    private Label _statHpLabel;
    private Label _statDmgLabel;
    private Label _statSpeedLabel;
    private Label _statWeaponLabel;
    private Label _statSpecialLabel;
    private Control _heroStage;

    private float _titleTimer = 0.0f;
    private float _heroFloatTimer = 0.0f;
    private Vector2 _titleBasePos;
    private Vector2 _heroBasePos;
    private Vector2 _targetParallaxOffset = Vector2.Zero;
    private Vector2 _currentParallaxOffset = Vector2.Zero;

    private static readonly Color DefaultButtonTextColor = new(0.90f, 0.85f, 0.95f);
    private static readonly Color HoverButtonTextColor = new(1.0f, 0.92f, 0.55f);

    public override void _Ready()
    {
        Systems.AssetGenerator.InitializeAssets();

        _parallaxRoot = GetNodeOrNull<Control>("%ParallaxRoot");
        _vignetteOverlay = GetNodeOrNull<TextureRect>("%VignetteOverlay");
        _pedestalSprite = GetNodeOrNull<TextureRect>("%PedestalSprite");
        _heroSprite = GetNodeOrNull<TextureRect>("%HeroSprite");
        _heroTitleLabel = GetNodeOrNull<Label>("%HeroTitleLabel");
        _heroSubtitleLabel = GetNodeOrNull<Label>("%HeroSubtitleLabel");
        _heroStatsPanel = GetNodeOrNull<PanelContainer>("%HeroStatsPanel");
        _statHpLabel = GetNodeOrNull<Label>("%StatHpLabel");
        _statDmgLabel = GetNodeOrNull<Label>("%StatDmgLabel");
        _statSpeedLabel = GetNodeOrNull<Label>("%StatSpeedLabel");
        _statWeaponLabel = GetNodeOrNull<Label>("%StatWeaponLabel");
        _statSpecialLabel = GetNodeOrNull<Label>("%StatSpecialLabel");
        _heroStage = GetNodeOrNull<Control>("%HeroStage");

        _continueButton = GetNodeOrNull<Button>("%ContinueButton");
        _playButton = GetNode<Button>("%PlayButton");
        _characterSelectBtn = GetNodeOrNull<Button>("%CharacterSelectBtn");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _exitButton = GetNode<Button>("%ExitButton");
        _titleLabel = GetNodeOrNull<Label>("%TitleLabel");
        _settingsMenu = GetNodeOrNull<SettingsMenu>("%SettingsMenu");

        // 1. Setup Procedural Atmospheric Visuals
        if (_vignetteOverlay != null)
        {
            _vignetteOverlay.Texture = Systems.AtmosphereGenerator.GetRadialVignetteTexture(0.82f);
        }

        if (_pedestalSprite != null)
        {
            _pedestalSprite.Texture = Systems.AtmosphereGenerator.GetAltarPedestalTexture();
        }

        if (_heroStatsPanel != null)
        {
            var panelBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(
                new Color(0.08f, 0.07f, 0.12f, 0.92f),
                new Color(0.35f, 0.30f, 0.48f, 0.85f),
                1.5f,
                6.0f
            );
            _heroStatsPanel.AddThemeStyleboxOverride("panel", panelBox);
        }

        // Add Altar embers
        if (_heroStage != null)
        {
            var altarEmbers = Systems.AtmosphereGenerator.CreateEmberParticles(
                new Vector2(220, 175),
                new Color(0.95f, 0.70f, 0.25f, 0.55f),
                16
            );
            _heroStage.AddChild(altarEmbers);
            _heroStage.MoveChild(altarEmbers, 1);
        }

        // Add Menu background floating dust
        if (_parallaxRoot != null)
        {
            var menuDust = Systems.AtmosphereGenerator.CreateAmbientDustParticles(
                new Vector2(1280, 720),
                new Color(0.65f, 0.70f, 0.90f, 0.18f),
                24
            );
            _parallaxRoot.AddChild(menuDust);
            _parallaxRoot.MoveChild(menuDust, 2);
        }

        if (_titleLabel != null)
        {
            _titleBasePos = _titleLabel.Position;
        }

        if (_heroSprite != null)
        {
            _heroBasePos = _heroSprite.Position;
        }

        // 2. Style Gothic Buttons
        StyleGothicButton(_playButton);
        StyleGothicButton(_settingsButton);
        StyleGothicButton(_exitButton);
        if (_characterSelectBtn != null) StyleGothicButton(_characterSelectBtn);
        if (_continueButton != null) StyleGothicButton(_continueButton, isGoldAccent: true);

        // 3. Connect signals
        _playButton.Pressed += OnPlayPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _exitButton.Pressed += OnExitPressed;

        if (_characterSelectBtn != null)
        {
            _characterSelectBtn.Pressed += OnCharacterSelectPressed;
        }

        CheckSavedRunStatus();
        UpdateCharacterDisplay(animate: false);

        // 4. Smooth Fade-in
        Modulate = new Color(1, 1, 1, 0);
        var fadeIn = CreateTween();
        fadeIn.TweenProperty(this, "modulate:a", 1.0f, 0.55f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Gentle floating animation for Title
        if (_titleLabel != null)
        {
            _titleTimer += dt * 2.2f;
            _titleLabel.Position = new Vector2(_titleBasePos.X, _titleBasePos.Y + Mathf.Sin(_titleTimer) * 3.5f);
        }

        // Idle breathing float for Altar Hero
        if (_heroSprite != null)
        {
            _heroFloatTimer += dt * 2.8f;
            _heroSprite.Position = new Vector2(_heroBasePos.X, _heroBasePos.Y + Mathf.Sin(_heroFloatTimer) * 3.0f);
        }

        // Smooth Mouse Parallax
        if (_parallaxRoot != null)
        {
            _currentParallaxOffset = _currentParallaxOffset.Lerp(_targetParallaxOffset, dt * 5.0f);
            _parallaxRoot.Position = _currentParallaxOffset;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            // Subtle 2.5D optical parallax offset (+/- 7px)
            Vector2 viewportCenter = GetViewportRect().Size / 2.0f;
            Vector2 mouseDiff = (motion.Position - viewportCenter) / viewportCenter;
            _targetParallaxOffset = -mouseDiff * 7.0f;
        }
    }

    private void StyleGothicButton(Button button, bool isGoldAccent = false)
    {
        Color normalBg = new(0.11f, 0.09f, 0.15f, 0.92f);
        Color normalBorder = isGoldAccent ? new Color(0.85f, 0.70f, 0.30f, 0.85f) : new Color(0.38f, 0.33f, 0.48f, 0.75f);
        Color hoverBg = new(0.20f, 0.15f, 0.28f, 0.96f);
        Color hoverBorder = isGoldAccent ? new Color(1.0f, 0.88f, 0.40f, 1.0f) : new Color(0.92f, 0.78f, 0.35f, 1.0f);
        Color pressedBg = new(0.16f, 0.11f, 0.22f, 0.98f);

        var normalBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(normalBg, normalBorder, 1.5f, 4.0f);
        var hoverBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(hoverBg, hoverBorder, 2.0f, 4.0f);
        var pressedBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(pressedBg, hoverBorder, 2.0f, 4.0f);

        button.AddThemeStyleboxOverride("normal", normalBox);
        button.AddThemeStyleboxOverride("hover", hoverBox);
        button.AddThemeStyleboxOverride("pressed", pressedBox);
        button.AddThemeStyleboxOverride("focus", hoverBox);

        button.PivotOffset = button.Size / 2.0f;
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
            tween.TweenProperty(button, "scale", new Vector2(1.04f, 1.04f), 0.14)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "theme_override_colors/font_color", HoverButtonTextColor, 0.14);
        }
        else
        {
            tween.TweenProperty(button, "scale", Vector2.One, 0.14)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(button, "theme_override_colors/font_color", DefaultButtonTextColor, 0.14);
        }
    }

    private void OnCharacterSelectPressed()
    {
        var gm = Systems.GameManager.Instance;
        if (gm == null) return;

        gm.SelectedClass = (gm.SelectedClass == Systems.PlayerClass.Wanderer)
            ? Systems.PlayerClass.Bladesmith
            : Systems.PlayerClass.Wanderer;

        UpdateCharacterDisplay(animate: true);
    }

    private void UpdateCharacterDisplay(bool animate)
    {
        var gm = Systems.GameManager.Instance;
        bool isBladesmith = gm?.SelectedClass == Systems.PlayerClass.Bladesmith;

        if (_characterSelectBtn != null)
        {
            _characterSelectBtn.Text = isBladesmith
                ? "⟲ SWITCH TO WANDERER"
                : "⟲ SWITCH TO BLADESMITH";
        }

        if (isBladesmith)
        {
            if (_heroSprite != null) _heroSprite.Texture = Systems.AssetGenerator.KnightTexture;
            if (_heroTitleLabel != null)
            {
                _heroTitleLabel.Text = "THE BLADESMITH";
                _heroTitleLabel.Modulate = new Color(1.0f, 0.42f, 0.42f);
            }
            if (_heroSubtitleLabel != null) _heroSubtitleLabel.Text = "Melee Vanguard · Heavy Greatblade & Deflection";
            if (_statHpLabel != null) _statHpLabel.Text = "❤️ Vitality:   4 Hearts (8 HP)";
            if (_statDmgLabel != null) _statDmgLabel.Text = "🗡️ Damage:     6.0 Base (Slash Arc)";
            if (_statSpeedLabel != null) _statSpeedLabel.Text = "⚡ Agility:    215 Movement Speed";
            if (_statWeaponLabel != null) _statWeaponLabel.Text = "⚔️ Weapon:     Greatblade (360° Slash Arc)";
            if (_statSpecialLabel != null) _statSpecialLabel.Text = "⭐ Special:    Parries & Deflects Projectiles";
        }
        else
        {
            if (_heroSprite != null) _heroSprite.Texture = Systems.AssetGenerator.PlayerTexture;
            if (_heroTitleLabel != null)
            {
                _heroTitleLabel.Text = "THE WANDERER";
                _heroTitleLabel.Modulate = new Color(1.0f, 0.88f, 0.45f);
            }
            if (_heroSubtitleLabel != null) _heroSubtitleLabel.Text = "Ranged Occult Mystic · Tear Sparks & Relics";
            if (_statHpLabel != null) _statHpLabel.Text = "❤️ Vitality:   3 Hearts (6 HP)";
            if (_statDmgLabel != null) _statDmgLabel.Text = "🗡️ Damage:     3.5 Base (Ranged Tears)";
            if (_statSpeedLabel != null) _statSpeedLabel.Text = "⚡ Agility:    200 Movement Speed";
            if (_statWeaponLabel != null) _statWeaponLabel.Text = "🔮 Weapon:     Occult Tears (Range: 420)";
            if (_statSpecialLabel != null) _statSpecialLabel.Text = "⭐ Special:    Piercing & Bouncing tears";
        }

        if (animate && _heroSprite != null)
        {
            _heroSprite.PivotOffset = _heroSprite.Size / 2.0f;
            var pop = CreateTween();
            pop.TweenProperty(_heroSprite, "scale", new Vector2(0.85f, 1.25f), 0.08);
            pop.TweenProperty(_heroSprite, "scale", new Vector2(1.18f, 0.90f), 0.10);
            pop.TweenProperty(_heroSprite, "scale", Vector2.One, 0.12);
        }
    }

    private void OnPlayPressed()
    {
        Systems.GameManager.Instance?.StartNewRun();
    }

    private void OnContinuePressed()
    {
        Systems.GameManager.Instance?.ContinueSavedRun();
    }

    private void CheckSavedRunStatus()
    {
        if (_continueButton == null) return;

        bool hasSave = Systems.SaveManager.HasSavedRun();
        _continueButton.Visible = hasSave;

        if (hasSave)
        {
            _continueButton.Pressed += OnContinuePressed;
            _continueButton.GrabFocus();
        }
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
