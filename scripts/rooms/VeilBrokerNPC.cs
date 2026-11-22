using Godot;
using System.Collections.Generic;

namespace Veilbound.Rooms;

/// <summary>
/// The Veil Broker — An atmospheric occult merchant residing in the Veil Bazaar.
/// Features breathing idle animation, dynamic player eye-tracking, ambient incense smoke,
/// protective spectral ward deflection, and dynamic dialogue barks.
/// </summary>
public partial class VeilBrokerNPC : Area2D
{
    public static VeilBrokerNPC Instance { get; private set; }

    private Sprite2D _brokerSprite;
    private Sprite2D _wardSprite;
    private PanelContainer _dialogueBalloon;
    private Label _dialogueLabel;
    private Tween _dialogueTween;
    private Tween _wardTween;

    private float _breatheTimer = 0.0f;
    private float _barkTimer = 0.0f;
    private bool _hasGreeted = false;

    private static readonly string[] WelcomeBarks = {
        "The Void offers, wanderer... if your coin is heavy.",
        "Touch nothing you cannot pay for.",
        "All debts are paid in the deep.",
        "Curiosities from shattered timelines... take your pick."
    };

    private static readonly string[] PurchaseBarks = {
        "A wise transaction.",
        "May that artifact stay your execution.",
        "Coin well spent, wanderer.",
        "The Veil hungers for gold as much as souls."
    };

    private static readonly string[] DeflectBarks = {
        "Test me not, spark.",
        "Your tears mean nothing in the Bazaar.",
        "I was ancient when this dungeon was dust.",
        "Keep your weapons sheathed, mortal."
    };

    public override void _Ready()
    {
        Instance = this;

        // Collision layers: layer 4 (enemies/targets) and layer 16 (obstacles)
        CollisionLayer = 16;
        CollisionMask = 4 | 2; // Player projectiles (bit 3) and player body (bit 2)
        Monitoring = true;
        Monitorable = true;

        Systems.AssetGenerator.InitializeAssets();

        // 1. Collision shape
        var col = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(56, 50) }
        };
        AddChild(col);

        // 2. Main Broker Sprite
        _brokerSprite = new Sprite2D
        {
            Texture = Systems.AssetGenerator.BrokerTexture,
            Scale = new Vector2(1.25f, 1.25f),
            Position = new Vector2(0, -6)
        };
        AddChild(_brokerSprite);

        // 3. Incense Braziers on left and right
        SetupBrazier(new Vector2(-60, 6));
        SetupBrazier(new Vector2(60, 6));

        // 4. Spectral Deflection Ward
        _wardSprite = new Sprite2D
        {
            Texture = Systems.PlaceholderSprite.CreateCircleTexture(36, new Color(0.4f, 0.8f, 1.0f, 0.45f)),
            Scale = Vector2.Zero,
            Visible = false
        };
        AddChild(_wardSprite);

        // 5. Dialogue Balloon Panel
        SetupDialogueBalloon();

        // Connect projectile collision
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        // Greet player after a short pause
        GetTree().CreateTimer(0.3).Timeout += () =>
        {
            if (!_hasGreeted)
            {
                _hasGreeted = true;
                string greet = WelcomeBarks[(int)(GD.Randi() % WelcomeBarks.Length)];
                ShowBark(greet, 4.0f);
            }
        };
    }

    private void SetupBrazier(Vector2 pos)
    {
        var brazier = new Node2D { Position = pos };
        AddChild(brazier);

        var spr = new Sprite2D
        {
            Texture = Systems.AssetGenerator.BrazierTexture,
            Scale = new Vector2(1.2f, 1.2f)
        };
        brazier.AddChild(spr);

        // Ambient rising smoke particles
        var smoke = new CpuParticles2D
        {
            Amount = 16,
            Lifetime = 1.6,
            Preprocess = 1.0,
            SpeedScale = 1.0f,
            Direction = new Vector2(0, -1),
            Spread = 18.0f,
            Gravity = new Vector2(0, -12),
            InitialVelocityMin = 18.0f,
            InitialVelocityMax = 32.0f,
            ScaleAmountMin = 2.5f,
            ScaleAmountMax = 5.0f,
            Color = new Color(0.6f, 0.35f, 0.85f, 0.45f)
        };
        brazier.AddChild(smoke);
    }

    private void SetupDialogueBalloon()
    {
        _dialogueBalloon = new PanelContainer
        {
            Position = new Vector2(-150, -68),
            Size = new Vector2(300, 32),
            Visible = false,
            Modulate = new Color(1, 1, 1, 0)
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.15f, 0.92f),
            BorderColor = new Color(0.85f, 0.70f, 0.25f, 0.85f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _dialogueBalloon.AddThemeStyleboxOverride("panel", style);

        _dialogueLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _dialogueLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.65f));
        _dialogueLabel.AddThemeFontSizeOverride("font_size", 12);
        _dialogueBalloon.AddChild(_dialogueLabel);

        AddChild(_dialogueBalloon);
    }

    public void ShowBark(string text, float duration = 3.5f)
    {
        if (_dialogueBalloon == null || _dialogueLabel == null) return;

        _dialogueLabel.Text = text;
        _dialogueBalloon.Visible = true;

        _dialogueTween?.Kill();
        _dialogueTween = CreateTween();
        _dialogueTween.TweenProperty(_dialogueBalloon, "modulate:a", 1.0f, 0.2)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _dialogueTween.TweenInterval(duration);
        _dialogueTween.TweenProperty(_dialogueBalloon, "modulate:a", 0.0f, 0.35)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _dialogueTween.TweenCallback(Callable.From(() =>
        {
            _dialogueBalloon.Visible = false;
        }));
    }

    public static void NotifyItemPurchased()
    {
        if (Instance != null)
        {
            string bark = PurchaseBarks[(int)(GD.Randi() % PurchaseBarks.Length)];
            Instance.ShowBark(bark, 3.2f);
        }
    }

    public override void _Process(double delta)
    {
        _breatheTimer += (float)delta * 2.2f;

        // Subtle breathing animation on broker sprite
        if (_brokerSprite != null)
        {
            float sine = Mathf.Sin(_breatheTimer);
            _brokerSprite.Scale = new Vector2(1.25f + sine * 0.02f, 1.25f - sine * 0.02f);
            _brokerSprite.Position = new Vector2(0, -6.0f + sine * 1.5f);
        }

        // Slight eye/torso shift towards player
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player != null && _brokerSprite != null)
        {
            float dx = player.GlobalPosition.X - GlobalPosition.X;
            float leanAngle = Mathf.Clamp(dx * 0.0003f, -0.06f, 0.06f);
            _brokerSprite.Rotation = Mathf.Lerp(_brokerSprite.Rotation, leanAngle, (float)delta * 4.0f);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Deflect player projectiles cleanly
        if (area is Player.Projectile proj)
        {
            TriggerDeflection(proj.GlobalPosition);
            proj.QueueFree();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player.Projectile proj)
        {
            TriggerDeflection(proj.GlobalPosition);
            proj.QueueFree();
        }
    }

    private void TriggerDeflection(Vector2 hitPos)
    {
        Systems.CameraShaker.Shake(0.12f);

        if (_wardSprite != null)
        {
            _wardSprite.Visible = true;
            _wardSprite.Scale = new Vector2(0.5f, 0.5f);
            _wardSprite.Modulate = new Color(0.85f, 0.70f, 0.25f, 0.9f);

            _wardTween?.Kill();
            _wardTween = CreateTween();
            _wardTween.TweenProperty(_wardSprite, "scale", new Vector2(1.4f, 1.4f), 0.15)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            _wardTween.Parallel().TweenProperty(_wardSprite, "modulate:a", 0.0f, 0.25);
            _wardTween.TweenCallback(Callable.From(() =>
            {
                _wardSprite.Visible = false;
            }));
        }

        string bark = DeflectBarks[(int)(GD.Randi() % DeflectBarks.Length)];
        ShowBark(bark, 2.5f);
    }
}
