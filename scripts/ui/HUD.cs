using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// Advanced in-game HUD inspired by The Binding of Isaac, Enter the Gungeon, and Cult of the Lamb.
/// Features an occult gothic status crest, compact horizontal currency matrix with punch-scale pop,
/// active item spacebar slot with primed gold pulse, celebratory item discovery banner,
/// passive relics tray (Extra HUD), and a gothic dual-layer "Ghost Bar" boss health meter.
/// </summary>
public partial class HUD : CanvasLayer
{
    public static HUD Instance { get; private set; }

    // ── Status Crest & Health ──
    private PanelContainer _topLeftPanel;
    private HBoxContainer _heartsContainer;
    private Tween _heartbeatTween;
    private int _lastCurrentHealth = 6;
    private int _lastMaxHealth = 6;
    private int _currentRallyHealth = 0;
    private float _currentRallyTimer = 0.0f;

    // ── Consumables ──
    private Label _coinLabel;
    private Label _bombLabel;
    private Label _keyLabel;
    private TextureRect _coinIcon;
    private TextureRect _bombIcon;
    private TextureRect _keyIcon;
    private int _prevCoins = -1;
    private int _prevBombs = -1;
    private int _prevKeys = -1;
    private int _displayCoins = 0;
    private Tween _coinTween;

    // ── Active Item Slot UI ──
    private PanelContainer _activeItemContainer;
    private TextureRect _activeItemIcon;
    private Label _activeItemNameLabel;
    private HBoxContainer _chargePipsContainer;
    private Tween _activeItemPulseTween;

    // ── Grand Item Discovery Splash Banner ──
    private PanelContainer _itemBanner;
    private TextureRect _bannerIcon;
    private Label _bannerTitle;
    private Label _bannerSubtitle;
    private Label _bannerDismissHint;
    private Button _bannerCloseBtn;
    private Button _bannerTransparentBtn;
    private Tween _bannerTween;

    // ── Boss Intro Splash & Chamber Purged Banners ──
    private PanelContainer _bossIntroModal;
    private Label _bossIntroTitle;
    private Label _bossIntroSubtitle;
    private Tween _bossIntroTween;

    private PanelContainer _roomClearedBanner;
    private Label _roomClearedLabel;
    private Tween _roomClearedTween;

    // ── Passive Relics Tray (Extra HUD) ──
    private HFlowContainer _relicsTray;
    private readonly HashSet<string> _collectedRelics = new();
    private PanelContainer _relicTooltipCard;
    private Label _relicTooltipTitle;
    private Label _relicTooltipDesc;
    private Label _relicTooltipStats;
    private string _currentActiveItemName = "None";
    private string _currentActiveItemDesc = "";

    // ── Retractable Stats & Guide Drawer ──
    private Control _statsDrawer;
    private Button _drawerTabButton;
    private bool _isDrawerOpen = false;
    private Tween _drawerTween;

    // ── Stat Tracker Labels ──
    private Label _damageLabel;
    private Label _tearsLabel;
    private Label _speedLabel;
    private Label _rangeLabel;

    // ── Key Guide Labels ──
    private Label _moveGuideLabel;
    private Label _shootGuideLabel;
    private Label _bombGuideLabel;
    private Button _openSettingsBtn;

    // ── Floor & Rooms ──
    private Label _floorLabel;
    private Label _roomsLabel;

    // ── Minimap ──
    private Minimap _minimap;

    // ── Boss "Ghost Bar" ──
    private Control _bossBarContainer;
    private Label _bossNameLabel;
    private Label _bossHPLabel;
    private ProgressBar _bossProgressBar;
    private ProgressBar _bossGhostBar;
    private Tween _ghostBarTween;
    private float _lastBossHealth = -1.0f;

    // ── Overlays ──
    private PauseMenu _pauseMenu;
    private GameOverScreen _gameOverScreen;
    private SettingsMenu _settingsMenu;
    private TacticalMapOverlay _tacticalMapOverlay;

    public override void _Ready()
    {
        Instance = this;

        // Instantiate Full-Screen Tactical Cartography Overlay ([TAB])
        _tacticalMapOverlay = new TacticalMapOverlay();
        AddChild(_tacticalMapOverlay);

        // 1. Style TopLeft Occult Crest
        _topLeftPanel = GetNodeOrNull<PanelContainer>("TopLeftPanel");
        if (_topLeftPanel != null)
        {
            var crestStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.06f, 0.05f, 0.09f, 0.88f),
                BorderColor = new Color(0.40f, 0.34f, 0.52f, 0.90f),
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            _topLeftPanel.AddThemeStyleboxOverride("panel", crestStyle);
        }

        _heartsContainer = GetNode<HBoxContainer>("%HeartsContainer");

        // 2. Consumables Ribbon
        _coinLabel = GetNodeOrNull<Label>("%CoinLabel");
        _bombLabel = GetNodeOrNull<Label>("%BombLabel");
        _keyLabel = GetNodeOrNull<Label>("%KeyLabel");
        _coinIcon = GetNodeOrNull<TextureRect>("%CoinIcon");
        _bombIcon = GetNodeOrNull<TextureRect>("%BombIcon");
        _keyIcon = GetNodeOrNull<TextureRect>("%KeyIcon");

        if (_coinIcon != null) _coinIcon.Texture = Systems.PlaceholderSprite.CreateCoinTexture();
        if (_bombIcon != null) _bombIcon.Texture = Systems.PlaceholderSprite.CreateBombTexture();
        if (_keyIcon != null) _keyIcon.Texture = Systems.PlaceholderSprite.CreateKeyTexture();

        // 3. Active Item Slot
        _activeItemContainer = GetNodeOrNull<PanelContainer>("%ActiveItemContainer");
        _activeItemIcon = GetNodeOrNull<TextureRect>("%ActiveItemIcon");
        _activeItemNameLabel = GetNodeOrNull<Label>("%ActiveItemNameLabel");
        _chargePipsContainer = GetNodeOrNull<HBoxContainer>("%ChargePipsContainer");
        StyleActiveItemSlot(false);

        // 4. Grand Item Splash Banner
        _itemBanner = GetNodeOrNull<PanelContainer>("%ItemBanner");
        _bannerIcon = GetNodeOrNull<TextureRect>("%BannerIcon");
        _bannerTitle = GetNodeOrNull<Label>("%BannerTitle");
        _bannerSubtitle = GetNodeOrNull<Label>("%BannerSubtitle");
        _bannerDismissHint = GetNodeOrNull<Label>("%BannerDismissHint");
        _bannerCloseBtn = GetNodeOrNull<Button>("%BannerCloseBtn");
        _bannerTransparentBtn = GetNodeOrNull<Button>("%BannerTransparentButton");

        if (_bannerCloseBtn != null)
            _bannerCloseBtn.Pressed += DismissItemBanner;

        if (_bannerTransparentBtn != null)
        {
            _bannerTransparentBtn.Pressed += DismissItemBanner;
            _bannerTransparentBtn.MouseEntered += OnBannerHovered;
            _bannerTransparentBtn.MouseExited += OnBannerUnhovered;
        }

        if (_itemBanner != null)
        {
            ApplyItemBannerStyle(false);
            _itemBanner.Visible = false;
        }

        // 5. Relics Tray (Extra HUD)
        _relicsTray = GetNodeOrNull<HFlowContainer>("%RelicsTray");
        SetupRelicTooltipCard();

        if (_activeItemContainer != null)
        {
            _activeItemContainer.MouseEntered += () =>
            {
                if (_currentActiveItemName != "None")
                {
                    ShowRelicTooltip(_currentActiveItemName, _currentActiveItemDesc, "[SPACEBAR] Active Item", new Color(0.35f, 0.85f, 1.0f), _activeItemContainer);
                }
            };
            _activeItemContainer.MouseExited += HideRelicTooltip;
        }

        // 6. Stats & Controls Drawer
        _statsDrawer = GetNodeOrNull<Control>("%StatsDrawer");
        _drawerTabButton = GetNodeOrNull<Button>("%DrawerTabButton");
        _damageLabel = GetNodeOrNull<Label>("%DamageLabel");
        _tearsLabel = GetNodeOrNull<Label>("%TearsLabel");
        _speedLabel = GetNodeOrNull<Label>("%SpeedLabel");
        _rangeLabel = GetNodeOrNull<Label>("%RangeLabel");
        _moveGuideLabel = GetNodeOrNull<Label>("%MoveGuideLabel");
        _shootGuideLabel = GetNodeOrNull<Label>("%ShootGuideLabel");
        _bombGuideLabel = GetNodeOrNull<Label>("%BombGuideLabel");
        _openSettingsBtn = GetNodeOrNull<Button>("%OpenSettingsBtn");

        if (_drawerTabButton != null) _drawerTabButton.Pressed += ToggleStatsDrawer;
        if (_openSettingsBtn != null) _openSettingsBtn.Pressed += () => _settingsMenu?.Open();

        // 7. Floor info
        _floorLabel = GetNode<Label>("%FloorLabel");
        _roomsLabel = GetNode<Label>("%RoomsLabel");

        // 8. Minimap & Boss Ghost Bar
        _minimap = GetNodeOrNull<Minimap>("%Minimap");
        _bossBarContainer = GetNodeOrNull<Control>("%BossBarContainer");
        _bossNameLabel = GetNodeOrNull<Label>("%BossNameLabel");
        _bossHPLabel = GetNodeOrNull<Label>("%BossHPLabel");
        _bossProgressBar = GetNodeOrNull<ProgressBar>("%BossProgressBar");
        _bossGhostBar = GetNodeOrNull<ProgressBar>("%BossGhostBar");

        SetupBossBars();

        // Overlays
        _pauseMenu = GetNodeOrNull<PauseMenu>("%PauseMenu");
        _gameOverScreen = GetNodeOrNull<GameOverScreen>("%GameOverScreen");
        _settingsMenu = GetNodeOrNull<SettingsMenu>("%SettingsMenu");

        UpdateFloorDisplay(1);
        UpdateRoomsDisplay(0);
        UpdateConsumables(0, 1, 1);
        UpdateControlsGuide();
        UpdateActiveItem("None", 0, 0);

        SetupBossIntroModal();
        SetupRoomClearedBanner();
    }

    private void StyleActiveItemSlot(bool isPrimed)
    {
        if (_activeItemContainer == null) return;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.09f, 0.16f, 0.88f),
            BorderColor = isPrimed ? new Color(1.0f, 0.85f, 0.2f, 0.95f) : new Color(0.35f, 0.30f, 0.45f, 0.80f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _activeItemContainer.AddThemeStyleboxOverride("panel", style);
    }

    private void SetupBossBars()
    {
        if (_bossBarContainer != null) _bossBarContainer.Visible = false;

        // Front bar: Deep crimson blood
        if (_bossProgressBar != null)
        {
            var frontFill = new StyleBoxFlat
            {
                BgColor = new Color(0.85f, 0.15f, 0.22f, 1.0f),
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3
            };
            var emptyBg = new StyleBoxFlat { BgColor = Colors.Transparent };
            _bossProgressBar.AddThemeStyleboxOverride("fill", frontFill);
            _bossProgressBar.AddThemeStyleboxOverride("background", emptyBg);
        }

        // Ghost bar: Trailing amber damage chip
        if (_bossGhostBar != null)
        {
            var ghostFill = new StyleBoxFlat
            {
                BgColor = new Color(0.95f, 0.75f, 0.35f, 0.95f),
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3
            };
            var ghostBg = new StyleBoxFlat
            {
                BgColor = new Color(0.08f, 0.06f, 0.10f, 0.90f),
                BorderColor = new Color(0.40f, 0.32f, 0.45f, 0.90f),
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4
            };
            _bossGhostBar.AddThemeStyleboxOverride("fill", ghostFill);
            _bossGhostBar.AddThemeStyleboxOverride("background", ghostBg);
        }
    }

    // ══════════════════════════════════════════════
    //  ACTIVE ITEM & PRIMED CHARGE PULSE
    // ══════════════════════════════════════════════

    public void UpdateActiveItem(string name, int currentCharges, int maxCharges)
    {
        if (_activeItemNameLabel == null || _chargePipsContainer == null) return;

        _currentActiveItemName = name;
        var activeData = Items.ActiveItemData.ActiveItemCatalog.Find(a => a.Name == name);
        _currentActiveItemDesc = activeData != null ? activeData.Description : "";

        if (_activeItemContainer != null)
        {
            _activeItemContainer.TooltipText = name != "None"
                ? $"{name}\n{_currentActiveItemDesc}\n[SPACEBAR] {currentCharges}/{maxCharges} Charges"
                : "No Active Item equipped";
        }

        bool isPrimed = maxCharges > 0 && currentCharges >= maxCharges;
        _activeItemNameLabel.Text = isPrimed ? $"[SPACE] {name} (PRIMED!)" : $"[SPACE] {name}";

        StyleActiveItemSlot(isPrimed);

        _activeItemPulseTween?.Kill();
        if (isPrimed)
        {
            // Radiant golden pulsation when spacebar is primed!
            _activeItemPulseTween = CreateTween().SetLoops();
            _activeItemPulseTween.TweenProperty(_activeItemContainer, "scale", new Vector2(1.05f, 1.05f), 0.35)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            _activeItemPulseTween.TweenProperty(_activeItemContainer, "scale", Vector2.One, 0.35)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
        else if (_activeItemContainer != null)
        {
            _activeItemContainer.Scale = Vector2.One;
        }

        foreach (Node child in _chargePipsContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (maxCharges <= 0) return;

        for (int i = 0; i < maxCharges; i++)
        {
            var pip = new ColorRect
            {
                CustomMinimumSize = new Vector2(16, 8),
                Color = i < currentCharges ? new Color(0.20f, 0.95f, 0.40f) : new Color(0.22f, 0.20f, 0.28f, 0.8f)
            };
            _chargePipsContainer.AddChild(pip);
        }
    }

    // ══════════════════════════════════════════════
    //  GRAND ITEM DISCOVERY BANNER (ISAAC/GUNGEON SPLASH)
    // ══════════════════════════════════════════════

    public void ShowItemBanner(string title, string subtitle, Color color, Texture2D icon = null)
    {
        if (_itemBanner == null) return;

        _bannerTitle.Text = title.ToUpper();
        _bannerSubtitle.Text = subtitle;

        if (_bannerIcon != null)
        {
            _bannerIcon.Texture = icon ?? Systems.PlaceholderSprite.CreateCircleTexture(16, color);
        }

        _bannerTween?.Kill();
        _itemBanner.Visible = true;
        _itemBanner.Modulate = new Color(1, 1, 1, 0);
        _itemBanner.Position = new Vector2(_itemBanner.Position.X, 35);

        _bannerTween = CreateTween();
        // 1. Slide down with smooth bounce
        _bannerTween.Parallel().TweenProperty(_itemBanner, "position:y", 85.0f, 0.28)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _bannerTween.Parallel().TweenProperty(_itemBanner, "modulate:a", 1.0f, 0.22);

        // Infinite stay: remains visible indefinitely until the player clicks anywhere on the banner to dismiss it!

        Systems.CameraShaker.Shake(0.12f);
    }

    public void DismissItemBanner()
    {
        if (_itemBanner == null || !_itemBanner.Visible) return;

        _bannerTween?.Kill();
        _bannerTween = CreateTween();
        _bannerTween.Parallel().TweenProperty(_itemBanner, "position:y", 40.0f, 0.22)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _bannerTween.Parallel().TweenProperty(_itemBanner, "modulate:a", 0.0f, 0.20);
        _bannerTween.Chain().TweenCallback(Callable.From(() => _itemBanner.Visible = false));
    }

    private void ApplyItemBannerStyle(bool isHovered)
    {
        if (_itemBanner == null) return;

        var bannerStyle = new StyleBoxFlat
        {
            BgColor = isHovered 
                ? new Color(0.12f, 0.08f, 0.20f, 0.98f) 
                : new Color(0.07f, 0.04f, 0.12f, 0.94f),
            BorderColor = isHovered
                ? new Color(1.0f, 0.92f, 0.45f, 1.0f)
                : new Color(0.95f, 0.78f, 0.25f, 0.95f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ShadowColor = new Color(0, 0, 0, 0.65f),
            ShadowSize = isHovered ? 6 : 4
        };
        _itemBanner.AddThemeStyleboxOverride("panel", bannerStyle);
    }

    private void OnBannerHovered()
    {
        ApplyItemBannerStyle(true);
        if (_bannerDismissHint != null)
            _bannerDismissHint.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.50f, 1.0f));
    }

    private void OnBannerUnhovered()
    {
        ApplyItemBannerStyle(false);
        if (_bannerDismissHint != null)
            _bannerDismissHint.AddThemeColorOverride("font_color", new Color(0.70f, 0.65f, 0.80f, 0.8f));
    }

    // ══════════════════════════════════════════════
    //  PASSIVE RELICS TRAY (EXTRA HUD) & HOVER TOOLTIPS
    // ══════════════════════════════════════════════

    private void SetupRelicTooltipCard()
    {
        _relicTooltipCard = new PanelContainer
        {
            Name = "RelicTooltipCard",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 50
        };

        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.12f, 0.96f),
            BorderColor = new Color(0.85f, 0.70f, 0.25f, 0.90f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.60f),
            ShadowSize = 6
        };
        _relicTooltipCard.AddThemeStyleboxOverride("panel", cardStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _relicTooltipCard.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        margin.AddChild(vbox);

        _relicTooltipTitle = new Label
        {
            Name = "Title",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _relicTooltipTitle.AddThemeFontSizeOverride("font_size", 13);
        _relicTooltipTitle.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.35f));
        vbox.AddChild(_relicTooltipTitle);

        _relicTooltipDesc = new Label
        {
            Name = "Desc",
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0)
        };
        _relicTooltipDesc.AddThemeFontSizeOverride("font_size", 11);
        _relicTooltipDesc.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.92f));
        vbox.AddChild(_relicTooltipDesc);

        _relicTooltipStats = new Label
        {
            Name = "Stats",
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0)
        };
        _relicTooltipStats.AddThemeFontSizeOverride("font_size", 10);
        _relicTooltipStats.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 0.55f));
        vbox.AddChild(_relicTooltipStats);

        AddChild(_relicTooltipCard);
    }

    public void ShowRelicTooltip(string title, string desc, string stats, Color accentColor, Control sourceNode)
    {
        if (_relicTooltipCard == null) return;

        _relicTooltipTitle.Text = title.ToUpper();
        _relicTooltipTitle.AddThemeColorOverride("font_color", accentColor);

        _relicTooltipDesc.Text = desc;
        _relicTooltipStats.Text = stats;
        _relicTooltipStats.Visible = !string.IsNullOrEmpty(stats);

        var style = _relicTooltipCard.GetThemeStylebox("panel") as StyleBoxFlat;
        if (style != null)
        {
            style.BorderColor = accentColor;
        }

        _relicTooltipCard.ResetSize();

        // Position card: right under the badge below the minimap
        Vector2 sourcePos = sourceNode.GlobalPosition;
        float cardWidth = 210.0f;
        float posX = sourcePos.X - cardWidth + sourceNode.Size.X;
        float posY = sourcePos.Y + sourceNode.Size.Y + 6.0f;

        // Clamp to screen bounds
        posX = Mathf.Clamp(posX, 10.0f, 1280.0f - cardWidth - 10.0f);
        posY = Mathf.Clamp(posY, 10.0f, 720.0f - 110.0f);

        _relicTooltipCard.GlobalPosition = new Vector2(posX, posY);
        _relicTooltipCard.Visible = true;
        _relicTooltipCard.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(_relicTooltipCard, "modulate:a", 1.0f, 0.10);
    }

    public void HideRelicTooltip()
    {
        if (_relicTooltipCard == null || !_relicTooltipCard.Visible) return;
        _relicTooltipCard.Visible = false;
    }

    public void AddPassiveRelic(Items.ItemData item)
    {
        if (item == null) return;
        AddPassiveRelic(item.Name, item.ItemColor, item.Description, item.GetStatSummary());
    }

    public void AddPassiveRelic(string name, Color color, string description = "", string statsText = "")
    {
        if (_relicsTray == null || _collectedRelics.Contains(name)) return;
        _collectedRelics.Add(name);

        var badge = new PanelContainer
        {
            CustomMinimumSize = new Vector2(26, 26),
            PivotOffset = new Vector2(13, 13)
        };

        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.09f, 0.16f, 0.92f),
            BorderColor = color,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        badge.AddThemeStyleboxOverride("panel", badgeStyle);

        var iconRect = new TextureRect
        {
            Texture = Systems.PlaceholderSprite.CreateCircleTexture(7, color),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        badge.AddChild(iconRect);

        // Native tooltip fallback
        string fullTooltip = name;
        if (!string.IsNullOrEmpty(description)) fullTooltip += $"\n{description}";
        if (!string.IsNullOrEmpty(statsText)) fullTooltip += $"\n{statsText}";
        badge.TooltipText = fullTooltip;

        // Custom rich hover card on mouse hover
        badge.MouseEntered += () =>
        {
            ShowRelicTooltip(name, description, statsText, color, badge);
            var hoverTween = badge.CreateTween();
            hoverTween.TweenProperty(badge, "scale", new Vector2(1.2f, 1.2f), 0.1)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        };
        badge.MouseExited += () =>
        {
            HideRelicTooltip();
            var hoverTween = badge.CreateTween();
            hoverTween.TweenProperty(badge, "scale", Vector2.One, 0.1);
        };

        _relicsTray.AddChild(badge);

        // Bouncy pop-in animation
        badge.Scale = Vector2.Zero;
        var tween = badge.CreateTween();
        tween.TweenProperty(badge, "scale", Vector2.One, 0.25)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    // ══════════════════════════════════════════════
    //  HEALTH SYSTEM
    // ══════════════════════════════════════════════

    public void UpdateRally(int rallyHealth, float timer)
    {
        _currentRallyHealth = rallyHealth;
        _currentRallyTimer = timer;
        UpdateHearts(_lastCurrentHealth, _lastMaxHealth);
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        _lastCurrentHealth = currentHealth;
        _lastMaxHealth = maxHealth;

        foreach (var child in _heartsContainer.GetChildren())
        {
            child.QueueFree();
        }

        int totalContainers = Mathf.CeilToInt(maxHealth / 2.0f);

        for (int i = 0; i < totalContainers; i++)
        {
            int baseHp = i * 2;
            bool leftRed = currentHealth >= baseHp + 1;
            bool leftRally = !leftRed && (currentHealth + _currentRallyHealth >= baseHp + 1);

            bool rightRed = currentHealth >= baseHp + 2;
            bool rightRally = !rightRed && (currentHealth + _currentRallyHealth >= baseHp + 2);

            int state = 0;
            if (leftRed && rightRed) state = 2;
            else if (leftRed && rightRally) state = 3;
            else if (leftRed) state = 1;
            else if (leftRally && rightRally) state = 4;
            else if (leftRally) state = 5;
            else state = 0;

            var heartRect = new TextureRect
            {
                Texture = Systems.PlaceholderSprite.CreateHeartTexture(state),
                CustomMinimumSize = new Vector2(26, 22),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
            };

            _heartsContainer.AddChild(heartRect);
        }

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
    //  CONSUMABLES & PUNCH-SCALE TWEENS
    // ══════════════════════════════════════════════

    public void UpdateConsumables(int coins, int bombs, int keys)
    {
        if (_coinLabel != null)
        {
            if (_prevCoins != -1 && coins != _prevCoins)
            {
                int startVal = _displayCoins;
                int targetVal = coins;
                _coinTween?.Kill();
                _coinTween = CreateTween();
                _coinTween.TweenMethod(Callable.From<int>((val) =>
                {
                    _displayCoins = val;
                    if (_coinLabel != null) _coinLabel.Text = val.ToString("D2");
                }), startVal, targetVal, 0.35)
                .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

                if (coins > _prevCoins) AnimatePickupPop(_coinIcon, _coinLabel);
            }
            else
            {
                _displayCoins = coins;
                _coinLabel.Text = coins.ToString("D2");
            }
            _prevCoins = coins;
        }

        if (_bombLabel != null)
        {
            _bombLabel.Text = bombs.ToString("D2");
            if (_prevBombs != -1 && bombs > _prevBombs) AnimatePickupPop(_bombIcon, _bombLabel);
            _prevBombs = bombs;
        }

        if (_keyLabel != null)
        {
            _keyLabel.Text = keys.ToString("D2");
            if (_prevKeys != -1 && keys > _prevKeys) AnimatePickupPop(_keyIcon, _keyLabel);
            _prevKeys = keys;
        }
    }

    private void AnimatePickupPop(Control icon, Control label)
    {
        if (icon != null)
        {
            var tween = icon.CreateTween();
            tween.TweenProperty(icon, "scale", new Vector2(1.4f, 1.4f), 0.08);
            tween.TweenProperty(icon, "scale", Vector2.One, 0.12)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        if (label != null)
        {
            var tween = label.CreateTween();
            tween.TweenProperty(label, "scale", new Vector2(1.3f, 1.3f), 0.08);
            tween.TweenProperty(label, "scale", Vector2.One, 0.12)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
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
        int floor = Systems.GameManager.Instance?.CurrentFloor ?? 1;
        var cleared = Systems.RoomManager.Instance?.ClearedRooms;
        _tacticalMapOverlay?.UpdateData(grid, visited, currentPos, floor, cleared);
    }

    // ══════════════════════════════════════════════
    //  GOTHIC DUAL-LAYER GHOST BAR BOSS METER
    // ══════════════════════════════════════════════

    public void ShowBossBar(string name, float current, float max)
    {
        if (_bossBarContainer == null) return;
        _bossBarContainer.Visible = true;

        if (_bossNameLabel != null) _bossNameLabel.Text = $"💀  {name.ToUpper()}  💀";
        if (_bossHPLabel != null) _bossHPLabel.Text = $"{Mathf.Max(0, (int)current)} / {(int)max} HP";

        if (_bossProgressBar != null && _bossGhostBar != null)
        {
            _bossProgressBar.MaxValue = max;
            _bossGhostBar.MaxValue = max;

            bool isDamage = _lastBossHealth > 0 && current < _lastBossHealth;
            _bossProgressBar.Value = current;

            if (isDamage)
            {
                // Instant red drop, delayed amber ghost bar catchup!
                _ghostBarTween?.Kill();
                _ghostBarTween = CreateTween();
                _ghostBarTween.TweenInterval(0.25);
                _ghostBarTween.TweenProperty(_bossGhostBar, "value", current, 0.35)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

                // Micro horizontal jitter shake on boss bar
                var shakeTween = _bossBarContainer.CreateTween();
                shakeTween.TweenProperty(_bossBarContainer, "position:x", _bossBarContainer.Position.X - 3.0f, 0.04);
                shakeTween.TweenProperty(_bossBarContainer, "position:x", _bossBarContainer.Position.X + 3.0f, 0.04);
                shakeTween.TweenProperty(_bossBarContainer, "position:x", _bossBarContainer.Position.X, 0.04);
            }
            else
            {
                _bossGhostBar.Value = current;
            }

            _lastBossHealth = current;
        }
    }

    public void HideBossBar()
    {
        if (_bossBarContainer != null) _bossBarContainer.Visible = false;
        _lastBossHealth = -1.0f;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_stats"))
        {
            ToggleStatsDrawer();
            GetViewport().SetInputAsHandled();
        }
    }

    public void ToggleStatsDrawer()
    {
        if (_statsDrawer == null) return;
        _isDrawerOpen = !_isDrawerOpen;

        _drawerTween?.Kill();
        _drawerTween = CreateTween().SetParallel(true);

        float targetX = _isDrawerOpen ? 0.0f : -205.0f;
        _drawerTween.TweenProperty(_statsDrawer, "position:x", targetX, 0.25)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        if (_drawerTabButton != null)
        {
            _drawerTabButton.Text = _isDrawerOpen ? "◀" : "▶ STATS";
        }

        if (_isDrawerOpen)
        {
            UpdateControlsGuide();
        }
    }

    private void UpdateControlsGuide()
    {
        var sm = Systems.SettingsManager.Instance;
        if (sm == null) return;

        if (_moveGuideLabel != null)
            _moveGuideLabel.Text = $"Move: {sm.GetActionKeyName("move_up")}/{sm.GetActionKeyName("move_left")}/{sm.GetActionKeyName("move_down")}/{sm.GetActionKeyName("move_right")}";

        if (_shootGuideLabel != null)
        {
            var mode = sm.ShootingMode;
            if (mode == Systems.ShootingControlMode.SingleButton)
            {
                _shootGuideLabel.Text = $"Shoot: {sm.GetActionKeyName("shoot_facing")} (Walk Dir)";
            }
            else if (mode == Systems.ShootingControlMode.FourWay)
            {
                _shootGuideLabel.Text = $"Shoot: {sm.GetActionKeyName("shoot_up")}/{sm.GetActionKeyName("shoot_left")}/{sm.GetActionKeyName("shoot_down")}/{sm.GetActionKeyName("shoot_right")}";
            }
            else
            {
                _shootGuideLabel.Text = $"Shoot: {sm.GetActionKeyName("shoot_facing")} (Walk) or Arrows";
            }
        }

        if (_bombGuideLabel != null)
            _bombGuideLabel.Text = $"Bomb: {sm.GetActionKeyName("place_bomb")} · Map: [TAB]";
    }

    public bool IsTacticalMapOpen => _tacticalMapOverlay != null && _tacticalMapOverlay.Visible;

    public void ToggleTacticalMap()
    {
        if (_tacticalMapOverlay == null) return;
        if (_tacticalMapOverlay.Visible)
        {
            _tacticalMapOverlay.Close();
        }
        else
        {
            if (_pauseMenu != null && _pauseMenu.Visible) return;
            _tacticalMapOverlay.Open();
        }
    }

    public void CloseTacticalMap()
    {
        _tacticalMapOverlay?.Close();
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

    // ══════════════════════════════════════════════
    //  CINEMATIC BOSS INTRO & CHAMBER PURGED BANNERS
    // ══════════════════════════════════════════════

    private void SetupBossIntroModal()
    {
        _bossIntroModal = new PanelContainer
        {
            Name = "BossIntroModal",
            Position = new Vector2(0, 240),
            Size = new Vector2(1280, 150),
            Visible = false,
            Modulate = new Color(1, 1, 1, 0),
            ZIndex = 40
        };

        var box = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.04f, 0.08f, 0.94f),
            BorderColor = new Color(0.85f, 0.25f, 0.30f, 0.95f),
            BorderWidthBottom = 3,
            BorderWidthTop = 3
        };
        _bossIntroModal.AddThemeStyleboxOverride("panel", box);

        var vbox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            Size = new Vector2(1280, 150)
        };
        vbox.AddThemeConstantOverride("separation", 8);

        _bossIntroTitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _bossIntroTitle.AddThemeFontSizeOverride("font_size", 28);
        _bossIntroTitle.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.35f));
        _bossIntroTitle.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.95f));
        _bossIntroTitle.AddThemeConstantOverride("shadow_offset_x", 2);
        _bossIntroTitle.AddThemeConstantOverride("shadow_offset_y", 2);
        vbox.AddChild(_bossIntroTitle);

        _bossIntroSubtitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _bossIntroSubtitle.AddThemeFontSizeOverride("font_size", 14);
        _bossIntroSubtitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.78f, 0.92f));
        _bossIntroSubtitle.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        _bossIntroSubtitle.AddThemeConstantOverride("shadow_offset_x", 1);
        _bossIntroSubtitle.AddThemeConstantOverride("shadow_offset_y", 1);
        vbox.AddChild(_bossIntroSubtitle);

        _bossIntroModal.AddChild(vbox);
        AddChild(_bossIntroModal);
    }

    public void ShowBossIntro(string name, string title)
    {
        if (_bossIntroModal == null || _bossIntroTitle == null || _bossIntroSubtitle == null) return;

        _bossIntroTitle.Text = $"💀  — {name.ToUpper()} —  💀";
        _bossIntroSubtitle.Text = $"— {title} —";
        _bossIntroModal.Visible = true;

        Systems.CameraShaker.Shake(0.35f);

        _bossIntroTween?.Kill();
        _bossIntroTween = CreateTween();
        _bossIntroTween.TweenProperty(_bossIntroModal, "modulate:a", 1.0f, 0.25)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _bossIntroTween.TweenInterval(1.8);
        _bossIntroTween.TweenProperty(_bossIntroModal, "modulate:a", 0.0f, 0.35)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _bossIntroTween.TweenCallback(Callable.From(() =>
        {
            _bossIntroModal.Visible = false;
        }));
    }

    private void SetupRoomClearedBanner()
    {
        _roomClearedBanner = new PanelContainer
        {
            Name = "RoomClearedBanner",
            Position = new Vector2((1280 - 280) / 2.0f, 82),
            Size = new Vector2(280, 36),
            Visible = false,
            Modulate = new Color(1, 1, 1, 0),
            ZIndex = 35
        };

        var box = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.12f, 0.10f, 0.90f),
            BorderColor = new Color(0.25f, 0.85f, 0.50f, 0.90f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _roomClearedBanner.AddThemeStyleboxOverride("panel", box);

        _roomClearedLabel = new Label
        {
            Text = "❖  CHAMBER PURGED  ❖",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _roomClearedLabel.AddThemeFontSizeOverride("font_size", 13);
        _roomClearedLabel.AddThemeColorOverride("font_color", new Color(0.40f, 0.95f, 0.60f));
        _roomClearedLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        _roomClearedLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        _roomClearedLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        _roomClearedBanner.AddChild(_roomClearedLabel);

        AddChild(_roomClearedBanner);
    }

    public void ShowRoomClearedBanner()
    {
        if (_roomClearedBanner == null) return;

        _roomClearedBanner.Visible = true;
        _roomClearedTween?.Kill();
        _roomClearedTween = CreateTween();
        _roomClearedTween.TweenProperty(_roomClearedBanner, "modulate:a", 1.0f, 0.20)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _roomClearedTween.TweenInterval(1.3);
        _roomClearedTween.TweenProperty(_roomClearedBanner, "modulate:a", 0.0f, 0.30)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _roomClearedTween.TweenCallback(Callable.From(() =>
        {
            _roomClearedBanner.Visible = false;
        }));
    }
}
