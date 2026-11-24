using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// Tactical 3-Column Gothic Codex & Pause Menu overlay.
/// Left Pane: Real-time Hero Telemetry & Attributes breakdown.
/// Center Pane: Active Rosary Relic Grid & interactive Inspection Card.
/// Right Pane: Descent Record summary & Sanctuary navigation buttons.
/// </summary>
public partial class PauseMenu : Control
{
    private PanelContainer _leftPanel;
    private PanelContainer _centerPanel;
    private PanelContainer _rightPanel;
    private PanelContainer _inspectionCard;

    private VBoxContainer _telemetryBox;
    private Label _rosaryCountLabel;
    private HFlowContainer _rosaryGrid;
    private Label _rosaryEmptyLabel;

    private Label _inspectionTitle;
    private Label _inspectionType;
    private Label _inspectionDesc;
    private Label _inspectionStats;

    private Label _summaryFloorLabel;
    private Label _summaryRoomsLabel;
    private Label _summaryKillsLabel;
    private Label _summaryTreasuryLabel;

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
    private static readonly Color GoldAccentColor = new(0.95f, 0.85f, 0.40f);
    private static readonly Color CyanAccentColor = new(0.35f, 0.85f, 0.95f);
    private static readonly Color DimTextColor = new(0.68f, 0.62f, 0.78f);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;

        _vignetteOverlay = GetNodeOrNull<TextureRect>("%VignetteOverlay");
        _pausePanel = GetNodeOrNull<VBoxContainer>("%PausePanel");
        _titleLabel = GetNodeOrNull<Label>("%TitleLabel");

        _leftPanel = GetNodeOrNull<PanelContainer>("%LeftTelemetryPanel");
        _centerPanel = GetNodeOrNull<PanelContainer>("%CenterRosaryPanel");
        _rightPanel = GetNodeOrNull<PanelContainer>("%RightActionsPanel");
        _inspectionCard = GetNodeOrNull<PanelContainer>("%InspectionCard");

        _telemetryBox = GetNodeOrNull<VBoxContainer>("%TelemetryBox");
        _rosaryCountLabel = GetNodeOrNull<Label>("%RosaryCountLabel");
        _rosaryGrid = GetNodeOrNull<HFlowContainer>("%RosaryGrid");
        _rosaryEmptyLabel = GetNodeOrNull<Label>("%RosaryEmptyLabel");

        _inspectionTitle = GetNodeOrNull<Label>("%InspectionTitle");
        _inspectionType = GetNodeOrNull<Label>("%InspectionType");
        _inspectionDesc = GetNodeOrNull<Label>("%InspectionDesc");
        _inspectionStats = GetNodeOrNull<Label>("%InspectionStats");

        _summaryFloorLabel = GetNodeOrNull<Label>("%SummaryFloorLabel");
        _summaryRoomsLabel = GetNodeOrNull<Label>("%SummaryRoomsLabel");
        _summaryKillsLabel = GetNodeOrNull<Label>("%SummaryKillsLabel");
        _summaryTreasuryLabel = GetNodeOrNull<Label>("%SummaryTreasuryLabel");

        _resumeButton = GetNode<Button>("%ResumeButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _restartButton = GetNode<Button>("%RestartButton");
        _menuButton = GetNode<Button>("%MenuButton");
        _settingsMenu = GetNodeOrNull<SettingsMenu>("%SettingsMenu");

        // 1. Procedural menu vignette
        if (_vignetteOverlay != null)
            _vignetteOverlay.Texture = Systems.AtmosphereGenerator.GetMenuVignetteTexture();

        // 2. Style the 3 column tome panels with gothic beveled borders
        Color panelBg = new(0.08f, 0.06f, 0.12f, 0.94f);
        Color panelBorder = new(0.36f, 0.28f, 0.48f, 0.80f);
        var panelBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(panelBg, panelBorder, 2.0f, 6.0f);

        _leftPanel?.AddThemeStyleboxOverride("panel", panelBox);
        _centerPanel?.AddThemeStyleboxOverride("panel", panelBox);
        _rightPanel?.AddThemeStyleboxOverride("panel", panelBox);

        // Style the inspection card with an occult amber tint
        Color inspectBg = new(0.12f, 0.09f, 0.17f, 0.96f);
        Color inspectBorder = new(0.65f, 0.52f, 0.28f, 0.85f);
        var inspectBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(inspectBg, inspectBorder, 1.5f, 4.0f);
        _inspectionCard?.AddThemeStyleboxOverride("panel", inspectBox);

        // 3. Style all buttons with gothic stone language
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

        // Query player stats and descent state
        var player = Player.PlayerController.Instance 
            ?? GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        var stats = player?.Stats;

        PopulateHeroTelemetry(stats);
        PopulateRosaryGrid(stats);
        PopulateDescentRecord(stats);

        // Slide in from center with scale pop
        if (_pausePanel != null)
        {
            _pausePanel.PivotOffset = _pausePanel.Size / 2.0f;
            _pausePanel.Scale = new Vector2(0.92f, 0.92f);
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

    private void PopulateHeroTelemetry(Player.PlayerStats stats)
    {
        if (_telemetryBox == null) return;

        foreach (Node child in _telemetryBox.GetChildren())
            child.QueueFree();

        var header = new Label
        {
            Text = "✦ HERO TELEMETRY ✦",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeColorOverride("font_color", GoldAccentColor);
        header.AddThemeFontSizeOverride("font_size", 15);
        _telemetryBox.AddChild(header);

        var selectedClass = Systems.GameManager.Instance?.SelectedClass ?? Systems.PlayerClass.Wanderer;
        string className = selectedClass == Systems.PlayerClass.Bladesmith 
            ? "THE BLADESMITH" 
            : "THE WANDERER";
        string classSubtitle = selectedClass == Systems.PlayerClass.Bladesmith
            ? "Vanguard of the Sanguine Forge"
            : "Seeker of the Astral Shards";

        var classLabel = new Label
        {
            Text = $"[ {className} ]",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        classLabel.AddThemeColorOverride("font_color", CyanAccentColor);
        classLabel.AddThemeFontSizeOverride("font_size", 14);
        _telemetryBox.AddChild(classLabel);

        var classSubLabel = new Label
        {
            Text = classSubtitle,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        classSubLabel.AddThemeColorOverride("font_color", DimTextColor);
        classSubLabel.AddThemeFontSizeOverride("font_size", 11);
        _telemetryBox.AddChild(classSubLabel);

        _telemetryBox.AddChild(new HSeparator());

        if (stats == null)
        {
            var noData = new Label { Text = "No Telemetry Signal..." };
            noData.AddThemeColorOverride("font_color", DimTextColor);
            _telemetryBox.AddChild(noData);
            return;
        }

        // Vitals
        int fullHearts = stats.CurrentHealth / 2;
        int halfHearts = stats.CurrentHealth % 2;
        int emptyHearts = (stats.MaxHealth - stats.CurrentHealth) / 2;
        string heartGlyphs = new string('♥', fullHearts) + (halfHearts > 0 ? "½" : "") + new string('♡', emptyHearts);

        AddTelemetryRow("Soul Vessels", $"{stats.CurrentHealth}/{stats.MaxHealth} HP  {heartGlyphs}", new Color(1.0f, 0.35f, 0.45f));
        AddTelemetryRow("Blood Rally", $"{stats.RallyHealth / 2.0f:F1} HP Window (2.5s)", new Color(1.0f, 0.75f, 0.20f));

        _telemetryBox.AddChild(new HSeparator());

        // Combat Attributes
        AddTelemetryRow("Damage", $"{stats.Damage:F1} ⚔", GoldAccentColor);
        AddTelemetryRow("Attack Rate", $"{1.0f / stats.FireRate:F1}/s 🏹", CyanAccentColor);
        AddTelemetryRow("Move Speed", $"{stats.MoveSpeed:F0} 👟", DefaultTextColor);
        AddTelemetryRow("Shot Range", $"{stats.ProjectileRange:F0} 🎯", DefaultTextColor);
        AddTelemetryRow("Fortune", $"{(stats.Luck >= 0 ? "+" : "")}{stats.Luck:F1} 🎲", GoldAccentColor);
        AddTelemetryRow("Multishot", $"{stats.Multishot}x Spread 💥", stats.Multishot > 1 ? CyanAccentColor : DefaultTextColor);

        string modText = stats.HasPiercing ? "Spectral (Piercing)"
            : stats.HasBouncing ? "Rubber (Bouncing)"
            : stats.HasBladeDeflection ? "Blade Deflect"
            : "Standard Tear";
        AddTelemetryRow("Weapon Mod", modText, stats.HasPiercing || stats.HasBouncing ? CyanAccentColor : DimTextColor);

        string statusText = stats.InflictedStatus != Enemies.StatusEffectType.None
            ? stats.InflictedStatus.ToString()
            : "None";
        Color statusColor = stats.InflictedStatus switch
        {
            Enemies.StatusEffectType.Poison => new Color(0.4f, 0.95f, 0.4f),
            Enemies.StatusEffectType.Burn => new Color(1.0f, 0.55f, 0.2f),
            Enemies.StatusEffectType.Bleed => new Color(0.95f, 0.2f, 0.3f),
            _ => DimTextColor
        };
        AddTelemetryRow("Curse Afflicted", statusText, statusColor);

        _telemetryBox.AddChild(new HSeparator());

        // Active Relic Slot
        string activeName = stats.HeldActiveItem != null ? stats.HeldActiveItem.Name : "None";
        string activeStatus = stats.HeldActiveItem != null
            ? $"{stats.ActiveItemCharges}/{stats.HeldActiveItem.MaxCharges} Charges {(stats.ActiveItemCharges >= stats.HeldActiveItem.MaxCharges ? "(READY!)" : "")}"
            : "No Relic Attuned";
        AddTelemetryRow("Active [SPACE]", $"{activeName}", stats.HeldActiveItem != null ? GoldAccentColor : DimTextColor);
        AddTelemetryRow("Charges", activeStatus, stats.HeldActiveItem != null && stats.ActiveItemCharges >= stats.HeldActiveItem.MaxCharges ? CyanAccentColor : DimTextColor);
    }

    private void AddTelemetryRow(string labelText, string valText, Color valColor)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var lbl = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        lbl.AddThemeColorOverride("font_color", DimTextColor);
        lbl.AddThemeFontSizeOverride("font_size", 12);

        var val = new Label
        {
            Text = valText,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        val.AddThemeColorOverride("font_color", valColor);
        val.AddThemeFontSizeOverride("font_size", 12);

        row.AddChild(lbl);
        row.AddChild(val);
        _telemetryBox.AddChild(row);
    }

    private void PopulateRosaryGrid(Player.PlayerStats stats)
    {
        if (_rosaryGrid == null) return;

        foreach (Node child in _rosaryGrid.GetChildren())
            child.QueueFree();

        var items = stats?.CollectedItems ?? new List<Items.ItemData>();
        if (_rosaryCountLabel != null)
            _rosaryCountLabel.Text = $"Claimed Relics & Arcana ({items.Count})";

        var scroll = _rosaryGrid.GetParent<ScrollContainer>();

        if (items.Count == 0)
        {
            if (_rosaryEmptyLabel != null) _rosaryEmptyLabel.Visible = true;
            if (scroll != null) scroll.Visible = false;

            if (_inspectionTitle != null) _inspectionTitle.Text = "No Relics Attuned";
            if (_inspectionType != null) _inspectionType.Text = "✦ SEEK THE DARK VAULTS ✦";
            if (_inspectionDesc != null) _inspectionDesc.Text = "Hover or select any relic in the rosary grid once discovered to inspect its lore and dark boons.";
            if (_inspectionStats != null) _inspectionStats.Text = "";
            return;
        }

        if (_rosaryEmptyLabel != null) _rosaryEmptyLabel.Visible = false;
        if (scroll != null) scroll.Visible = true;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(44, 44),
                TooltipText = item.Name
            };

            Color itemColor = item.ItemColor;
            Color border = new(0.50f, 0.40f, 0.65f, 0.85f);
            var normalBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(new Color(itemColor.R * 0.35f, itemColor.G * 0.35f, itemColor.B * 0.35f, 0.95f), border, 1.5f, 4.0f);
            var hoverBox = Systems.AtmosphereGenerator.CreateGothicStyleBox(new Color(itemColor.R * 0.6f, itemColor.G * 0.6f, itemColor.B * 0.6f, 1.0f), GoldAccentColor, 2.0f, 4.0f);

            btn.AddThemeStyleboxOverride("normal", normalBox);
            btn.AddThemeStyleboxOverride("hover", hoverBox);
            btn.AddThemeStyleboxOverride("pressed", hoverBox);
            btn.AddThemeStyleboxOverride("focus", hoverBox);

            // Icon representation
            string initial = item.Name.Length > 0 ? item.Name.Substring(0, 1) : "✦";
            btn.Text = initial;
            btn.AddThemeColorOverride("font_color", GoldAccentColor);
            btn.AddThemeFontSizeOverride("font_size", 16);

            btn.MouseEntered += () => DisplayRelicInspection(item);
            btn.Pressed += () => DisplayRelicInspection(item);

            _rosaryGrid.AddChild(btn);
        }

        // Default display the first relic
        DisplayRelicInspection(items[0]);
    }

    private void DisplayRelicInspection(Items.ItemData item)
    {
        if (item == null) return;

        if (_inspectionTitle != null)
        {
            _inspectionTitle.Text = item.Name;
            _inspectionTitle.AddThemeColorOverride("font_color", item.ItemColor);
        }

        if (_inspectionType != null)
            _inspectionType.Text = "✦ PASSIVE RELIC ✦";

        if (_inspectionDesc != null)
            _inspectionDesc.Text = item.Description;

        if (_inspectionStats != null)
        {
            string summary = item.GetStatSummary();
            _inspectionStats.Text = !string.IsNullOrEmpty(summary) ? $"Stat Boons: {summary}" : "";
        }
    }

    private void PopulateDescentRecord(Player.PlayerStats stats)
    {
        int floor = Systems.GameManager.Instance?.CurrentFloor ?? 1;
        int rooms = Systems.GameManager.Instance?.RoomsCleared ?? 0;
        int kills = stats?.EnemiesKilled ?? 0;

        if (_summaryFloorLabel != null)
            _summaryFloorLabel.Text = $"Floor {floor} — The Crypt";

        if (_summaryRoomsLabel != null)
            _summaryRoomsLabel.Text = $"Chambers Purged: {rooms}";

        if (_summaryKillsLabel != null)
            _summaryKillsLabel.Text = $"Foes Banished: {kills}";

        if (_summaryTreasuryLabel != null)
            _summaryTreasuryLabel.Text = $"🪙 {stats?.Coins ?? 0}   💣 {stats?.Bombs ?? 0}   🗝 {stats?.Keys ?? 0}";
    }

    private void StyleGothicButton(Button button, bool isAccent = false, int dangerLevel = 0)
    {
        if (button == null) return;

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
