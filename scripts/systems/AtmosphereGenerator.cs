using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Generates atmospheric 2D visual elements: radial darkness vignettes,
/// soft wall drop-shadows, floating dust/ember particle systems,
/// and gothic stone UI styleboxes.
/// Inspired by Hollow Knight, Darkest Dungeon, and Blasphemous.
/// </summary>
public static class AtmosphereGenerator
{
    private static Texture2D _vignetteTexture;
    private static Texture2D _bossVignetteTexture;
    private static Texture2D _wallShadowTexture;
    private static Texture2D _altarTexture;
    private static Texture2D _torchSconceTexture;

    /// <summary>
    /// Smooth radial vignette gradient that darkens edges and focuses light on the center.
    /// </summary>
    public static Texture2D GetRadialVignetteTexture(float outerDarkness = 0.78f)
    {
        if (_vignetteTexture != null) return _vignetteTexture;

        var grad = new Gradient();
        grad.AddPoint(0.0f, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        grad.AddPoint(0.60f, new Color(0.02f, 0.01f, 0.04f, outerDarkness * 0.35f));
        grad.AddPoint(1.0f, new Color(0.01f, 0.01f, 0.02f, outerDarkness));

        var tex = new GradientTexture2D
        {
            Gradient = grad,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1.0f, 1.0f),
            Width = 640,
            Height = 360
        };

        _vignetteTexture = tex;
        return _vignetteTexture;
    }

    /// <summary>
    /// Ominous deep crimson radial vignette for Boss rooms.
    /// </summary>
    public static Texture2D GetBossVignetteTexture()
    {
        if (_bossVignetteTexture != null) return _bossVignetteTexture;

        var grad = new Gradient();
        grad.AddPoint(0.0f, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        grad.AddPoint(0.55f, new Color(0.15f, 0.02f, 0.04f, 0.35f));
        grad.AddPoint(1.0f, new Color(0.12f, 0.01f, 0.02f, 0.88f));

        var tex = new GradientTexture2D
        {
            Gradient = grad,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1.0f, 1.0f),
            Width = 640,
            Height = 360
        };

        _bossVignetteTexture = tex;
        return _bossVignetteTexture;
    }

    /// <summary>
    /// Soft vertical linear drop-shadow gradient cast beneath dungeon walls.
    /// </summary>
    public static Texture2D GetWallShadowTexture(float startAlpha = 0.65f)
    {
        if (_wallShadowTexture != null) return _wallShadowTexture;

        var grad = new Gradient();
        grad.AddPoint(0.0f, new Color(0.0f, 0.0f, 0.0f, startAlpha));
        grad.AddPoint(1.0f, new Color(0.0f, 0.0f, 0.0f, 0.0f));

        var tex = new GradientTexture2D
        {
            Gradient = grad,
            Fill = GradientTexture2D.FillEnum.Linear,
            FillFrom = new Vector2(0.5f, 0.0f),
            FillTo = new Vector2(0.5f, 1.0f),
            Width = 64,
            Height = 36
        };

        _wallShadowTexture = tex;
        return _wallShadowTexture;
    }

    /// <summary>
    /// Ambient floating dust and spore particles drifting across a room or menu.
    /// </summary>
    public static CpuParticles2D CreateAmbientDustParticles(Vector2 bounds, Color tint, int count = 28)
    {
        var particles = new CpuParticles2D
        {
            Amount = count,
            Lifetime = 6.0,
            Preprocess = 2.5,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = bounds / 2.0f,
            Position = bounds / 2.0f,
            Direction = new Vector2(-0.25f, -1.0f),
            Spread = 40.0f,
            Gravity = new Vector2(-1.5f, -3.0f),
            InitialVelocityMin = 5.0f,
            InitialVelocityMax = 15.0f,
            ScaleAmountMin = 1.0f,
            ScaleAmountMax = 2.5f,
            Color = tint,
            ZIndex = 6
        };

        return particles;
    }

    /// <summary>
    /// Rising burning ember particles for soul altars or torches.
    /// </summary>
    public static CpuParticles2D CreateEmberParticles(Vector2 emitterPos, Color emberColor, int count = 22)
    {
        var particles = new CpuParticles2D
        {
            Amount = count,
            Lifetime = 2.8,
            Preprocess = 1.5,
            Position = emitterPos,
            Direction = new Vector2(0.0f, -1.0f),
            Spread = 30.0f,
            Gravity = new Vector2(0.0f, -18.0f),
            InitialVelocityMin = 25.0f,
            InitialVelocityMax = 55.0f,
            ScaleAmountMin = 1.2f,
            ScaleAmountMax = 3.2f,
            Color = emberColor,
            ZIndex = 8
        };

        return particles;
    }

    /// <summary>
    /// Generates a carved occult stone dais / altar pedestal texture.
    /// </summary>
    public static Texture2D GetAltarPedestalTexture()
    {
        if (_altarTexture != null) return _altarTexture;

        const int w = 160;
        const int h = 48;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);

        var topBevel = new Color(0.28f, 0.24f, 0.35f);
        var faceColor = new Color(0.14f, 0.12f, 0.18f);
        var bottomRim = new Color(0.08f, 0.06f, 0.10f);
        var runeGlow = new Color(0.85f, 0.65f, 0.25f, 0.85f);
        var shadow = new Color(0.04f, 0.03f, 0.05f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Inset trapezoid / pedestal shape
                int inset = (y < 12) ? (12 - y) * 2 : 0;
                if (x < inset + 4 || x >= w - inset - 4)
                {
                    img.SetPixel(x, y, new Color(0, 0, 0, 0));
                    continue;
                }

                if (y < 4)
                {
                    img.SetPixel(x, y, topBevel);
                }
                else if (y < 12)
                {
                    img.SetPixel(x, y, topBevel.Darkened(0.15f));
                }
                else if (y >= h - 4)
                {
                    img.SetPixel(x, y, bottomRim);
                }
                else
                {
                    // Altar face with carved runic lines
                    bool isRune = (y >= 20 && y <= 28 && (x % 24 >= 10 && x % 24 <= 14)) ||
                                  (y == 24 && x >= 20 && x <= w - 20);
                    if (isRune)
                    {
                        img.SetPixel(x, y, runeGlow);
                    }
                    else
                    {
                        img.SetPixel(x, y, faceColor);
                    }
                }
            }
        }

        _altarTexture = ImageTexture.CreateFromImage(img);
        return _altarTexture;
    }

    /// <summary>
    /// Creates a gothic stone stylebox with runic border accents.
    /// </summary>
    public static StyleBoxFlat CreateGothicStyleBox(Color bgColor, Color borderColor, float borderWidth = 2.0f, float cornerRadius = 4.0f)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthTop = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            CornerRadiusTopLeft = (int)cornerRadius,
            CornerRadiusTopRight = (int)cornerRadius,
            CornerRadiusBottomLeft = (int)cornerRadius,
            CornerRadiusBottomRight = (int)cornerRadius,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        return style;
    }

    /// <summary>
    /// Generates a pixel-art wrought iron wall sconce with wooden torch.
    /// </summary>
    public static Texture2D GetTorchSconceTexture()
    {
        if (_torchSconceTexture != null) return _torchSconceTexture;

        const int w = 12;
        const int h = 20;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var ironDark = new Color(0.12f, 0.10f, 0.16f);
        var ironLight = new Color(0.38f, 0.34f, 0.44f);
        var woodShaft = new Color(0.44f, 0.28f, 0.16f);
        var fireGold = new Color(1.0f, 0.88f, 0.32f);
        var fireOrange = new Color(0.95f, 0.46f, 0.16f);

        // Flame tip & core (Y: 1 to 5)
        img.SetPixel(5, 1, fireGold);
        img.SetPixel(6, 1, fireGold);
        for (int x = 4; x <= 7; x++) img.SetPixel(x, 2, fireGold);
        for (int x = 3; x <= 8; x++) img.SetPixel(x, 3, fireOrange);
        for (int x = 4; x <= 7; x++) img.SetPixel(x, 4, fireOrange);

        // Torch head / bronze cup (Y: 5 to 7)
        for (int x = 3; x <= 8; x++)
        {
            img.SetPixel(x, 5, ironLight);
            img.SetPixel(x, 6, ironDark);
        }

        // Wooden stick shaft (Y: 7 to 14)
        for (int y = 7; y <= 14; y++)
        {
            img.SetPixel(5, y, woodShaft);
            img.SetPixel(6, y, woodShaft.Darkened(0.2f));
        }

        // Wall sconce bracket (Y: 11 to 18)
        for (int y = 11; y <= 18; y++)
        {
            img.SetPixel(4, y, ironDark);
            img.SetPixel(7, y, ironLight);
        }
        for (int x = 3; x <= 8; x++)
        {
            img.SetPixel(x, 15, ironDark);
            img.SetPixel(x, 16, ironLight);
        }

        _torchSconceTexture = ImageTexture.CreateFromImage(img);
        return _torchSconceTexture;
    }

    /// <summary>
    /// Soft radial light gradient pool for torches and lamps.
    /// </summary>
    public static Texture2D CreateRadialLightTexture(Color flameColor)
    {
        var grad = new Gradient();
        grad.AddPoint(0.0f, new Color(flameColor.R, flameColor.G, flameColor.B, 0.45f));
        grad.AddPoint(0.35f, new Color(flameColor.R, flameColor.G, flameColor.B, 0.20f));
        grad.AddPoint(0.70f, new Color(flameColor.R, flameColor.G, flameColor.B, 0.05f));
        grad.AddPoint(1.0f, new Color(0, 0, 0, 0));

        var tex = new GradientTexture2D
        {
            Gradient = grad,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1.0f, 1.0f),
            Width = 160,
            Height = 160
        };
        return tex;
    }

    /// <summary>
    /// Creates a complete Wall Torch instance with sconce, dancing embers, and organic light flicker.
    /// </summary>
    public static Node2D CreateWallTorch(Vector2 position, Color flameColor)
    {
        var torch = new Node2D { Position = position, Name = "WallTorch" };

        // 1. Soft radial light glow on the wall and floor
        var glow = new TextureRect
        {
            Texture = CreateRadialLightTexture(flameColor),
            Position = new Vector2(-80, -75),
            Size = new Vector2(160, 160),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ZIndex = -1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        torch.AddChild(glow);

        // 2. Iron Sconce Bracket
        var sconce = new TextureRect
        {
            Texture = GetTorchSconceTexture(),
            Position = new Vector2(-6, -10),
            Size = new Vector2(12, 20),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ZIndex = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        torch.AddChild(sconce);

        // 3. Dancing flame embers
        var embers = new CpuParticles2D
        {
            Amount = 5,
            Lifetime = 0.65,
            Preprocess = 0.3,
            Position = new Vector2(0, -6),
            Direction = new Vector2(0, -1),
            Spread = 22.0f,
            Gravity = new Vector2(0, -20.0f),
            InitialVelocityMin = 14.0f,
            InitialVelocityMax = 30.0f,
            ScaleAmountMin = 1.0f,
            ScaleAmountMax = 2.2f,
            Color = flameColor,
            ZIndex = 2
        };
        torch.AddChild(embers);

        // 4. Smooth organic light flicker tween
        var tween = torch.CreateTween().SetLoops();
        tween.TweenProperty(glow, "modulate:a", 0.62f, 0.11);
        tween.TweenProperty(glow, "modulate:a", 0.95f, 0.13);
        tween.TweenProperty(glow, "modulate:a", 0.70f, 0.09);
        tween.TweenProperty(glow, "modulate:a", 0.88f, 0.12);

        return torch;
    }
}
