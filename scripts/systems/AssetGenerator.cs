using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Generates rich, authentic pixel-art assets for Veilbound.
/// Automatically creates and saves PNG files to disk in `assets/`
/// and provides ready-to-use ImageTextures at runtime.
/// </summary>
public static class AssetGenerator
{
    private static bool _initialized = false;

    // Cached textures
    public static Texture2D PlayerTexture { get; private set; }
    public static Texture2D KnightTexture { get; private set; }
    public static Texture2D FlyTexture { get; private set; }
    public static Texture2D ChaserTexture { get; private set; }
    public static Texture2D ShooterTexture { get; private set; }
    public static Texture2D SlimeTexture { get; private set; }
    public static Texture2D GoliathBossTexture { get; private set; }

    // Themes
    public static Texture2D FloorTileTexture { get; private set; }
    public static Texture2D WallTileTexture { get; private set; }

    public static Texture2D SpaceshipFloorTexture { get; private set; }
    public static Texture2D SpaceshipWallTexture { get; private set; }

    public static Texture2D DesertFloorTexture { get; private set; }
    public static Texture2D DesertWallTexture { get; private set; }

    public static Texture2D DessertFloorTexture { get; private set; }
    public static Texture2D DessertWallTexture { get; private set; }

    // Decals
    public static Texture2D DecalRuneTexture { get; private set; }
    public static Texture2D DecalTechTexture { get; private set; }
    public static Texture2D DecalSandTexture { get; private set; }
    public static Texture2D DecalCandyTexture { get; private set; }
    public static Texture2D DecalBloodTexture { get; private set; }

    // Obstacles & Items
    public static Texture2D RockObstacleTexture { get; private set; }
    public static Texture2D PotObstacleTexture { get; private set; }
    public static Texture2D DoorFrameTexture { get; private set; }
    public static Texture2D BombEntityTexture { get; private set; }
    public static Texture2D BrokerTexture { get; private set; }
    public static Texture2D BrazierTexture { get; private set; }

    public static void InitializeAssets()
    {
        if (_initialized) return;
        _initialized = true;

        PlayerTexture = GenerateAndSave("res://assets/characters/player.png", BuildPlayerSprite(32, 32));
        KnightTexture = GenerateAndSave("res://assets/characters/knight.png", BuildKnightSprite(32, 32));
        FlyTexture = GenerateAndSave("res://assets/enemies/fly.png", BuildFlySprite(24, 24));
        ChaserTexture = GenerateAndSave("res://assets/enemies/chaser.png", BuildChaserSprite(32, 32));
        ShooterTexture = GenerateAndSave("res://assets/enemies/shooter.png", BuildShooterSprite(32, 32));
        SlimeTexture = GenerateAndSave("res://assets/enemies/slime.png", BuildSlimeSprite(28, 24));
        GoliathBossTexture = GenerateAndSave("res://assets/bosses/goliath.png", BuildGoliathSprite(64, 64));

        FloorTileTexture = GenerateAndSave("res://assets/environments/floor_tile.png", BuildFloorTexture(64, 64));
        WallTileTexture = GenerateAndSave("res://assets/environments/wall_stone.png", BuildWallTexture(64, 64));

        SpaceshipFloorTexture = GenerateAndSave("res://assets/environments/floor_spaceship.png", BuildSpaceshipFloor(64, 64));
        SpaceshipWallTexture = GenerateAndSave("res://assets/environments/wall_spaceship.png", BuildSpaceshipWall(64, 64));

        DesertFloorTexture = GenerateAndSave("res://assets/environments/floor_desert.png", BuildDesertFloor(64, 64));
        DesertWallTexture = GenerateAndSave("res://assets/environments/wall_desert.png", BuildDesertWall(64, 64));

        DessertFloorTexture = GenerateAndSave("res://assets/environments/floor_dessert.png", BuildDessertFloor(64, 64));
        DessertWallTexture = GenerateAndSave("res://assets/environments/wall_dessert.png", BuildDessertWall(64, 64));

        DecalRuneTexture = GenerateAndSave("res://assets/environments/decal_rune.png", BuildRuneDecal(64, 64));
        DecalTechTexture = GenerateAndSave("res://assets/environments/decal_tech.png", BuildTechDecal(64, 64));
        DecalSandTexture = GenerateAndSave("res://assets/environments/decal_sand.png", BuildSandDecal(48, 48));
        DecalCandyTexture = GenerateAndSave("res://assets/environments/decal_candy.png", BuildCandyDecal(48, 48));
        DecalBloodTexture = GenerateAndSave("res://assets/environments/decal_blood.png", BuildBloodDecal(32, 32));

        RockObstacleTexture = GenerateAndSave("res://assets/environments/rock.png", BuildRockTexture(32, 32));
        PotObstacleTexture = GenerateAndSave("res://assets/environments/pot.png", BuildPotTexture(32, 32));
        DoorFrameTexture = GenerateAndSave("res://assets/environments/door_frame.png", BuildDoorTexture(80, 80));
        BombEntityTexture = GenerateAndSave("res://assets/items/bomb.png", BuildBombTexture(24, 24));
        BrokerTexture = GenerateAndSave("res://assets/characters/broker.png", BuildBrokerSprite(40, 48));
        BrazierTexture = GenerateAndSave("res://assets/environments/brazier.png", BuildBrazierSprite(20, 20));

        GD.Print("Veilbound pixel assets successfully generated and cached!");
    }

    private static ImageTexture GenerateAndSave(string path, Image image)
    {
        string globalPath = ProjectSettings.GlobalizePath(path);
        string dir = System.IO.Path.GetDirectoryName(globalPath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        image.SavePng(path);
        return ImageTexture.CreateFromImage(image);
    }

    // ══════════════════════════════════════════════
    //  1. PLAYER CHARACTER (Cloaked Veil Wanderer)
    // ══════════════════════════════════════════════
    private static Image BuildPlayerSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var darkOutline = new Color(0.1f, 0.08f, 0.16f);
        var cloakMain = new Color(0.28f, 0.22f, 0.42f);
        var cloakShadow = new Color(0.18f, 0.14f, 0.28f);
        var cloakHighlight = new Color(0.42f, 0.35f, 0.60f);
        var faceSkin = new Color(0.85f, 0.82f, 0.90f);
        var glowEye = new Color(0.35f, 0.95f, 1.0f);
        var glowEyeShine = new Color(0.85f, 1.0f, 1.0f);
        var beltColor = new Color(0.45f, 0.32f, 0.18f);

        // Body / Cloak oval (Y: 14 to 28, X: 8 to 23)
        for (int y = 14; y <= 28; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                float dx = (x - 15.5f) / 7.5f;
                float dy = (y - 21.0f) / 7.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (y >= 26) img.SetPixel(x, y, cloakShadow);
                    else if (x <= 11) img.SetPixel(x, y, cloakHighlight);
                    else img.SetPixel(x, y, cloakMain);
                }
            }
        }

        // Head / Cowl (Y: 4 to 17, X: 7 to 24)
        for (int y = 4; y <= 17; y++)
        {
            for (int x = 7; x <= 24; x++)
            {
                float dx = (x - 15.5f) / 8.0f;
                float dy = (y - 10.5f) / 6.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    // Hood exterior
                    img.SetPixel(x, y, cloakMain);
                    if (x <= 10 || y <= 6) img.SetPixel(x, y, cloakHighlight);
                    if (x >= 21) img.SetPixel(x, y, cloakShadow);
                }
            }
        }

        // Face aperture inside cowl (Y: 9 to 14, X: 11 to 20)
        for (int y = 9; y <= 14; y++)
        {
            for (int x = 11; x <= 20; x++)
            {
                img.SetPixel(x, y, faceSkin);
            }
        }

        // Glowing Cyan Eyes (Isaac crying style glow)
        img.SetPixel(13, 11, glowEye);
        img.SetPixel(14, 11, glowEyeShine);
        img.SetPixel(13, 12, glowEye);
        img.SetPixel(14, 12, glowEye);

        img.SetPixel(17, 11, glowEye);
        img.SetPixel(18, 11, glowEyeShine);
        img.SetPixel(17, 12, glowEye);
        img.SetPixel(18, 12, glowEye);

        // Teardrop streams down cheeks
        img.SetPixel(13, 13, glowEye);
        img.SetPixel(14, 14, glowEye);
        img.SetPixel(18, 13, glowEye);
        img.SetPixel(17, 14, glowEye);

        // Leather belt at waist
        for (int x = 10; x <= 21; x++)
        {
            img.SetPixel(x, 22, beltColor);
        }
        img.SetPixel(15, 22, new Color(0.9f, 0.8f, 0.3f)); // belt buckle

        AddDarkOutline(img, darkOutline);
        return img;
    }

    // ══════════════════════════════════════════════
    //  1B. MELEE CHARACTER (Bladesmith Knight)
    // ══════════════════════════════════════════════
    private static Image BuildKnightSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var darkOutline = new Color(0.10f, 0.10f, 0.14f);
        var armorMain = new Color(0.48f, 0.52f, 0.58f);
        var armorHighlight = new Color(0.72f, 0.76f, 0.84f);
        var armorShadow = new Color(0.26f, 0.28f, 0.34f);
        var visorEmber = new Color(1.0f, 0.35f, 0.20f);
        var visorShine = new Color(1.0f, 0.85f, 0.40f);
        var capeRed = new Color(0.72f, 0.15f, 0.22f);
        var goldTrim = new Color(0.92f, 0.78f, 0.25f);
        var bladeSteel = new Color(0.85f, 0.90f, 0.98f);

        // Crimson Cape / Tabard (Y: 15 to 28, X: 7 to 24)
        for (int y = 15; y <= 28; y++)
        {
            for (int x = 7; x <= 24; x++)
            {
                if (x <= 9 || x >= 22 || y >= 25)
                {
                    img.SetPixel(x, y, capeRed);
                }
            }
        }

        // Steel Cuirass (Breastplate) (Y: 15 to 26, X: 10 to 21)
        for (int y = 15; y <= 26; y++)
        {
            for (int x = 10; x <= 21; x++)
            {
                if (x <= 12) img.SetPixel(x, y, armorHighlight);
                else if (x >= 19) img.SetPixel(x, y, armorShadow);
                else img.SetPixel(x, y, armorMain);
            }
        }

        // Golden Trim Belt
        for (int x = 10; x <= 21; x++)
        {
            img.SetPixel(x, 23, goldTrim);
        }

        // Great Helmet (Y: 4 to 15, X: 8 to 23)
        for (int y = 4; y <= 15; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                float dx = (x - 15.5f) / 7.5f;
                float dy = (y - 9.5f) / 5.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (x <= 11) img.SetPixel(x, y, armorHighlight);
                    else if (x >= 20) img.SetPixel(x, y, armorShadow);
                    else img.SetPixel(x, y, armorMain);
                }
            }
        }

        // Helmet Visor Slit with burning ember glow (Y: 10 to 11, X: 11 to 20)
        for (int x = 11; x <= 20; x++)
        {
            img.SetPixel(x, 10, darkOutline);
            img.SetPixel(x, 11, visorEmber);
        }
        img.SetPixel(13, 11, visorShine);
        img.SetPixel(17, 11, visorShine);

        // Sword on back (diagonal blade)
        img.SetPixel(23, 5, goldTrim);  // Pommel
        img.SetPixel(22, 6, goldTrim);  // Crossguard
        img.SetPixel(24, 6, goldTrim);
        img.SetPixel(21, 7, bladeSteel);
        img.SetPixel(20, 8, bladeSteel);
        img.SetPixel(19, 9, bladeSteel);

        AddDarkOutline(img, darkOutline);
        return img;
    }

    // ══════════════════════════════════════════════
    //  2. FLY ENEMY
    // ══════════════════════════════════════════════
    private static Image BuildFlySprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.12f, 0.05f, 0.08f);
        var bodyRed = new Color(0.85f, 0.18f, 0.22f);
        var bodyDark = new Color(0.55f, 0.10f, 0.15f);
        var eyeColor = new Color(0.95f, 0.85f, 0.2f);
        var wingColor = new Color(0.85f, 0.95f, 1.0f, 0.75f);

        // Wings (spread out top-left and top-right)
        for (int x = 3; x <= 9; x++)
        {
            for (int y = 4; y <= 9; y++)
            {
                if (x + y >= 8 && x + y <= 15) img.SetPixel(x, y, wingColor);
            }
        }
        for (int x = 14; x <= 20; x++)
        {
            for (int y = 4; y <= 9; y++)
            {
                if (x - y >= 7 && x - y <= 14) img.SetPixel(x, y, wingColor);
            }
        }

        // Swollen Blood Body
        for (int y = 8; y <= 19; y++)
        {
            for (int x = 7; x <= 16; x++)
            {
                float dx = (x - 11.5f) / 4.5f;
                float dy = (y - 13.5f) / 5.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (dx > 0.2f || dy > 0.3f) img.SetPixel(x, y, bodyDark);
                    else img.SetPixel(x, y, bodyRed);
                }
            }
        }

        // Large compound fly eyes
        img.SetPixel(9, 10, eyeColor);
        img.SetPixel(10, 10, eyeColor);
        img.SetPixel(9, 11, eyeColor);

        img.SetPixel(13, 10, eyeColor);
        img.SetPixel(14, 10, eyeColor);
        img.SetPixel(14, 11, eyeColor);

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  3. CHASER ENEMY (Ghoulish Shambler)
    // ══════════════════════════════════════════════
    private static Image BuildChaserSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.14f, 0.08f, 0.06f);
        var flesh = new Color(0.72f, 0.62f, 0.52f);
        var fleshShadow = new Color(0.48f, 0.38f, 0.30f);
        var hollow = new Color(0.10f, 0.05f, 0.05f);
        var teeth = new Color(0.9f, 0.88f, 0.75f);

        // Body
        for (int y = 14; y <= 27; y++)
        {
            for (int x = 10; x <= 21; x++)
            {
                float dx = (x - 15.5f) / 5.5f;
                float dy = (y - 20.5f) / 6.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, (x > 16) ? fleshShadow : flesh);
                }
            }
        }

        // Head (Isaac Gaper style)
        for (int y = 4; y <= 16; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                float dx = (x - 15.5f) / 7.5f;
                float dy = (y - 10.0f) / 6.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, (x > 16 || y > 13) ? fleshShadow : flesh);
                }
            }
        }

        // Hollow empty eye sockets
        for (int y = 7; y <= 10; y++)
        {
            img.SetPixel(11, y, hollow);
            img.SetPixel(12, y, hollow);
            img.SetPixel(18, y, hollow);
            img.SetPixel(19, y, hollow);
        }

        // Screaming gaping mouth
        for (int y = 12; y <= 15; y++)
        {
            for (int x = 13; x <= 17; x++)
            {
                img.SetPixel(x, y, hollow);
            }
        }
        img.SetPixel(14, 12, teeth);
        img.SetPixel(16, 12, teeth);

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  4. SHOOTER ENEMY (Bloated Polyp Maw)
    // ══════════════════════════════════════════════
    private static Image BuildShooterSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.18f, 0.05f, 0.15f);
        var purpleSkin = new Color(0.68f, 0.28f, 0.65f);
        var shadowSkin = new Color(0.42f, 0.15f, 0.40f);
        var orifice = new Color(0.25f, 0.04f, 0.10f);
        var redVein = new Color(0.95f, 0.15f, 0.25f);

        // Bulbous head/maw
        for (int y = 6; y <= 26; y++)
        {
            for (int x = 6; x <= 25; x++)
            {
                float dx = (x - 15.5f) / 9.5f;
                float dy = (y - 16.0f) / 10.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, (dy > 0.2f || dx > 0.3f) ? shadowSkin : purpleSkin);
                }
            }
        }

        // Veins
        img.SetPixel(10, 10, redVein);
        img.SetPixel(11, 11, redVein);
        img.SetPixel(20, 11, redVein);
        img.SetPixel(21, 12, redVein);

        // Central Gaping Orifice that fires projectiles
        for (int y = 13; y <= 19; y++)
        {
            for (int x = 13; x <= 18; x++)
            {
                float dx = (x - 15.5f) / 2.8f;
                float dy = (y - 16.0f) / 3.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, orifice);
                }
            }
        }
        img.SetPixel(15, 15, redVein);
        img.SetPixel(16, 15, redVein);

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  5. CRIMSON SLIME ENEMY (Gelatinous Blob)
    // ══════════════════════════════════════════════
    private static Image BuildSlimeSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.24f, 0.04f, 0.08f);
        var slimeRuby = new Color(0.92f, 0.18f, 0.28f);
        var slimeDark = new Color(0.65f, 0.10f, 0.18f);
        var slimeHighlight = new Color(1.0f, 0.50f, 0.60f);
        var shineWhite = new Color(1.0f, 0.90f, 0.95f);
        var nucleusGlow = new Color(1.0f, 0.70f, 0.20f);
        var eyeColor = new Color(1.0f, 1.0f, 1.0f);
        var pupilColor = new Color(0.12f, 0.02f, 0.04f);

        // Gelatinous dome (w=28, h=24)
        for (int y = 4; y <= 21; y++)
        {
            for (int x = 2; x <= 25; x++)
            {
                // Oval squished at bottom
                float dx = (x - 13.5f) / 10.5f;
                float dy = (y - 14.5f) / (y < 14 ? 9.5f : 6.5f);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (y >= 18 || (x > 18 && y > 12)) img.SetPixel(x, y, slimeDark);
                    else if (x <= 8 && y <= 10) img.SetPixel(x, y, slimeHighlight);
                    else img.SetPixel(x, y, slimeRuby);
                }
            }
        }

        // Internal glowing core / nucleus
        for (int y = 13; y <= 16; y++)
        {
            for (int x = 12; x <= 16; x++)
            {
                float dx = (x - 14.0f) / 1.8f;
                float dy = (y - 14.5f) / 1.5f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, nucleusGlow);
                }
            }
        }

        // Specular glossy curves on upper-left
        img.SetPixel(6, 7, shineWhite);
        img.SetPixel(7, 7, shineWhite);
        img.SetPixel(8, 6, shineWhite);
        img.SetPixel(9, 6, shineWhite);
        img.SetPixel(10, 6, shineWhite);
        img.SetPixel(6, 8, slimeHighlight);

        // Cute/fierce Isaac-style eyes
        // Left eye
        for (int y = 10; y <= 13; y++)
        {
            img.SetPixel(8, y, eyeColor);
            img.SetPixel(9, y, eyeColor);
        }
        img.SetPixel(9, 12, pupilColor);
        img.SetPixel(8, 10, shineWhite);

        // Right eye
        for (int y = 10; y <= 13; y++)
        {
            img.SetPixel(16, y, eyeColor);
            img.SetPixel(17, y, eyeColor);
        }
        img.SetPixel(17, 12, pupilColor);
        img.SetPixel(16, 10, shineWhite);

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  6. GOLIATH BOSS (Monstro-Inspired Colossus)
    // ══════════════════════════════════════════════
    private static Image BuildGoliathSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.08f, 0.05f, 0.12f);
        var stone = new Color(0.48f, 0.38f, 0.52f);
        var stoneShadow = new Color(0.28f, 0.20f, 0.34f);
        var stoneHighlight = new Color(0.68f, 0.58f, 0.72f);
        var runeGlow = new Color(0.95f, 0.35f, 0.65f);
        var eyeGlow = new Color(1.0f, 0.2f, 0.3f);
        var hornColor = new Color(0.18f, 0.12f, 0.22f);

        // Huge monolithic body (64x64)
        for (int y = 14; y <= 56; y++)
        {
            for (int x = 10; x <= 53; x++)
            {
                float dx = (x - 31.5f) / 21.5f;
                float dy = (y - 35.0f) / 21.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (y < 24 && x < 28) img.SetPixel(x, y, stoneHighlight);
                    else if (y > 42 || x > 42) img.SetPixel(x, y, stoneShadow);
                    else img.SetPixel(x, y, stone);
                }
            }
        }

        // Curved Horns
        for (int i = 0; i < 12; i++)
        {
            // Left horn
            img.SetPixel(16 - i / 2, 16 - i, hornColor);
            img.SetPixel(17 - i / 2, 16 - i, hornColor);
            // Right horn
            img.SetPixel(47 + i / 2, 16 - i, hornColor);
            img.SetPixel(46 + i / 2, 16 - i, hornColor);
        }

        // Angry monolithic glowing eyes
        for (int y = 24; y <= 27; y++)
        {
            for (int x = 20; x <= 26; x++) img.SetPixel(x, y, eyeGlow);
            for (int x = 37; x <= 43; x++) img.SetPixel(x, y, eyeGlow);
        }

        // Cracks glowing with ethereal purple/pink Veil energy
        int[] crackX = { 31, 32, 33, 32, 31, 30, 29, 28, 29, 30 };
        for (int i = 0; i < crackX.Length; i++)
        {
            int y = 30 + i;
            if (crackX[i] < w && y < h) img.SetPixel(crackX[i], y, runeGlow);
        }

        // Massive fanged maw
        for (int y = 42; y <= 50; y++)
        {
            for (int x = 22; x <= 41; x++)
            {
                img.SetPixel(x, y, dark);
            }
        }
        // Jagged teeth
        for (int x = 24; x <= 39; x += 3)
        {
            img.SetPixel(x, 43, Colors.White);
            img.SetPixel(x + 1, 49, Colors.White);
        }

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  6. DUNGEON TILES & ENVIRONMENT MODELS
    // ══════════════════════════════════════════════
    private static Image BuildFloorTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var baseDark = new Color(0.14f, 0.12f, 0.17f);
        var tileColor = new Color(0.18f, 0.16f, 0.22f);
        var mortar = new Color(0.09f, 0.07f, 0.11f);
        var highlight = new Color(0.24f, 0.21f, 0.29f);

        img.Fill(tileColor);

        // Grid of 4 large stone pavers
        int midX = w / 2;
        int midY = h / 2;

        for (int x = 0; x < w; x++)
        {
            img.SetPixel(x, 0, mortar);
            img.SetPixel(x, midY, mortar);
            img.SetPixel(x, h - 1, mortar);
        }
        for (int y = 0; y < h; y++)
        {
            img.SetPixel(0, y, mortar);
            img.SetPixel(midX, y, mortar);
            img.SetPixel(w - 1, y, mortar);
        }

        // Stone bevels
        for (int x = 1; x < midX; x++)
        {
            img.SetPixel(x, 1, highlight);
            img.SetPixel(x + midX, 1, highlight);
            img.SetPixel(x, midY + 1, highlight);
            img.SetPixel(x + midX, midY + 1, highlight);
        }

        // Subtle stone weathering dots
        img.SetPixel(12, 14, highlight);
        img.SetPixel(13, 14, baseDark);
        img.SetPixel(45, 38, highlight);
        img.SetPixel(22, 50, baseDark);

        return img;
    }

    private static Image BuildWallTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var mortar = new Color(0.10f, 0.08f, 0.13f);
        var brickLight = new Color(0.38f, 0.32f, 0.44f);
        var brickMid = new Color(0.28f, 0.23f, 0.33f);
        var brickDark = new Color(0.18f, 0.14f, 0.22f);

        img.Fill(brickMid);

        // Masonry rows (4 horizontal courses of bricks)
        int courseH = h / 4;
        for (int row = 0; row < 4; row++)
        {
            int yStart = row * courseH;
            for (int x = 0; x < w; x++)
            {
                img.SetPixel(x, yStart, mortar);
                if (yStart + 1 < h) img.SetPixel(x, yStart + 1, brickLight);
                if (yStart + courseH - 1 < h) img.SetPixel(x, yStart + courseH - 1, brickDark);
            }

            // Staggered vertical joints
            int offset = (row % 2 == 0) ? 0 : w / 4;
            for (int x = offset; x < w; x += w / 2)
            {
                for (int y = yStart; y < yStart + courseH; y++)
                {
                    img.SetPixel(x, y, mortar);
                }
            }
        }

        return img;
    }

    private static Image BuildRockTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.12f, 0.10f, 0.15f);
        var stoneLight = new Color(0.52f, 0.48f, 0.58f);
        var stoneMid = new Color(0.38f, 0.34f, 0.44f);
        var stoneDark = new Color(0.24f, 0.20f, 0.28f);

        // Boulder geometry
        for (int y = 4; y <= 28; y++)
        {
            for (int x = 4; x <= 27; x++)
            {
                float dx = (x - 15.5f) / 11.5f;
                float dy = (y - 16.0f) / 12.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (y < 12 && x < 14) img.SetPixel(x, y, stoneLight);
                    else if (y > 20 || x > 20) img.SetPixel(x, y, stoneDark);
                    else img.SetPixel(x, y, stoneMid);
                }
            }
        }

        // Jagged crack in rock
        img.SetPixel(14, 10, dark);
        img.SetPixel(15, 11, dark);
        img.SetPixel(15, 12, dark);
        img.SetPixel(16, 13, dark);
        img.SetPixel(16, 14, dark);

        AddDarkOutline(img, dark);
        return img;
    }

    private static Image BuildPotTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.16f, 0.08f, 0.05f);
        var potClay = new Color(0.72f, 0.45f, 0.25f);
        var potShadow = new Color(0.45f, 0.25f, 0.12f);
        var potHighlight = new Color(0.88f, 0.60f, 0.35f);
        var runeColor = new Color(0.95f, 0.85f, 0.45f);

        // Pot neck and rim
        for (int x = 11; x <= 20; x++)
        {
            img.SetPixel(x, 6, potHighlight);
            img.SetPixel(x, 7, potClay);
        }

        // Pot belly
        for (int y = 8; y <= 26; y++)
        {
            for (int x = 7; x <= 24; x++)
            {
                float dx = (x - 15.5f) / 8.5f;
                float dy = (y - 17.0f) / 9.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (x <= 11) img.SetPixel(x, y, potHighlight);
                    else if (x >= 20) img.SetPixel(x, y, potShadow);
                    else img.SetPixel(x, y, potClay);
                }
            }
        }

        // Runic band
        for (int x = 11; x <= 20; x += 2)
        {
            img.SetPixel(x, 16, runeColor);
        }

        AddDarkOutline(img, dark);
        return img;
    }

    private static Image BuildDoorTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var archStone = new Color(0.40f, 0.35f, 0.48f);
        var archShadow = new Color(0.22f, 0.18f, 0.28f);
        var ironGrate = new Color(0.15f, 0.12f, 0.18f);

        // Arched frame
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x < 12 || x >= w - 12 || y < 12);
                if (isBorder)
                {
                    img.SetPixel(x, y, (x < 6 || y < 6) ? archStone : archShadow);
                }
                else
                {
                    // Dark interior entryway
                    img.SetPixel(x, y, new Color(0.04f, 0.03f, 0.06f));
                }
            }
        }

        return img;
    }

    private static Image BuildBombTexture(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var dark = new Color(0.08f, 0.08f, 0.12f);
        var iron = new Color(0.25f, 0.28f, 0.34f);
        var ironShine = new Color(0.60f, 0.65f, 0.72f);
        var fuse = new Color(0.85f, 0.75f, 0.40f);
        var spark = new Color(1.0f, 0.4f, 0.1f);

        // Spherical iron bomb body
        for (int y = 7; y <= 21; y++)
        {
            for (int x = 4; x <= 19; x++)
            {
                float dx = (x - 11.5f) / 7.5f;
                float dy = (y - 14.0f) / 7.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (x <= 9 && y <= 11) img.SetPixel(x, y, ironShine);
                    else img.SetPixel(x, y, iron);
                }
            }
        }

        // Fuse & Spark
        img.SetPixel(12, 5, fuse);
        img.SetPixel(13, 4, fuse);
        img.SetPixel(14, 3, fuse);
        img.SetPixel(15, 2, spark);
        img.SetPixel(16, 2, new Color(1.0f, 0.9f, 0.2f));

        AddDarkOutline(img, dark);
        return img;
    }

    // ══════════════════════════════════════════════
    //  7. THEMED FLOORS & WALLS (Spaceship, Desert, Dessert)
    // ══════════════════════════════════════════════

    private static Image BuildSpaceshipFloor(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var baseDark = new Color(0.10f, 0.11f, 0.15f);
        var plateColor = new Color(0.15f, 0.17f, 0.22f);
        var seamColor = new Color(0.06f, 0.07f, 0.10f);
        var neonCyan = new Color(0.20f, 0.85f, 0.95f);
        var neonDim = new Color(0.08f, 0.40f, 0.50f);
        var boltColor = new Color(0.35f, 0.42f, 0.50f);

        img.Fill(plateColor);

        // Grid panels
        for (int x = 0; x < w; x++)
        {
            img.SetPixel(x, 0, seamColor);
            img.SetPixel(x, h / 2, seamColor);
            img.SetPixel(x, h - 1, seamColor);
        }
        for (int y = 0; y < h; y++)
        {
            img.SetPixel(0, y, seamColor);
            img.SetPixel(w / 2, y, seamColor);
            img.SetPixel(w - 1, y, seamColor);
        }

        // Neon cyan energy conduits along panel edges
        for (int x = 6; x < w - 6; x++)
        {
            if (x == w / 2) continue;
            img.SetPixel(x, h / 2 - 2, neonDim);
            if (x % 4 == 0) img.SetPixel(x, h / 2 - 2, neonCyan);
        }

        // Bolts in corners of 4 sub-plates
        int[] pts = { 3, w / 2 - 4, w / 2 + 3, w - 4 };
        foreach (int px in pts)
        {
            foreach (int py in pts)
            {
                img.SetPixel(px, py, boltColor);
            }
        }

        return img;
    }

    private static Image BuildSpaceshipWall(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var darkHull = new Color(0.08f, 0.09f, 0.12f);
        var plateLight = new Color(0.28f, 0.32f, 0.40f);
        var plateMid = new Color(0.18f, 0.21f, 0.28f);
        var conduitGlow = new Color(0.25f, 0.90f, 1.0f);

        img.Fill(plateMid);

        // Horizontal steel support beams
        for (int x = 0; x < w; x++)
        {
            img.SetPixel(x, 0, darkHull);
            img.SetPixel(x, 1, plateLight);
            img.SetPixel(x, h / 2, darkHull);
            img.SetPixel(x, h / 2 + 1, plateLight);
            img.SetPixel(x, h - 1, darkHull);

            // Glowing cyan conduit stripe along center of lower beam
            if (x % 3 != 0)
                img.SetPixel(x, h / 2 + 6, conduitGlow);
        }

        // Vertical bulkhead ribs
        for (int y = 0; y < h; y++)
        {
            img.SetPixel(0, y, darkHull);
            img.SetPixel(1, y, plateLight);
            img.SetPixel(w / 2, y, darkHull);
            img.SetPixel(w / 2 + 1, y, plateLight);
            img.SetPixel(w - 1, y, darkHull);
        }

        return img;
    }

    private static Image BuildDesertFloor(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var sandBase = new Color(0.68f, 0.52f, 0.34f);
        var sandLight = new Color(0.78f, 0.64f, 0.44f);
        var sandDark = new Color(0.50f, 0.36f, 0.22f);
        var mortar = new Color(0.38f, 0.26f, 0.16f);

        img.Fill(sandBase);

        // Flagstone stone joints
        int midX = w / 2;
        int midY = h / 2;
        for (int x = 0; x < w; x++)
        {
            img.SetPixel(x, 0, mortar);
            img.SetPixel(x, midY, mortar);
            img.SetPixel(x, h - 1, mortar);
        }
        for (int y = 0; y < h; y++)
        {
            img.SetPixel(0, y, mortar);
            img.SetPixel(midX, y, mortar);
            img.SetPixel(w - 1, y, mortar);
        }

        // Sandstone sun-bleached bevels
        for (int x = 1; x < midX; x++)
        {
            img.SetPixel(x, 1, sandLight);
            img.SetPixel(x + midX, 1, sandLight);
            img.SetPixel(x, midY + 1, sandLight);
            img.SetPixel(x + midX, midY + 1, sandLight);
        }

        // Windblown sand dunes texture
        img.SetPixel(10, 12, sandDark);
        img.SetPixel(11, 12, sandLight);
        img.SetPixel(42, 35, sandLight);
        img.SetPixel(43, 36, sandDark);

        return img;
    }

    private static Image BuildDesertWall(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var mortar = new Color(0.32f, 0.20f, 0.12f);
        var sandstoneLight = new Color(0.82f, 0.65f, 0.42f);
        var sandstoneMid = new Color(0.64f, 0.48f, 0.30f);
        var sandstoneDark = new Color(0.48f, 0.32f, 0.18f);
        var hieroglyphColor = new Color(0.35f, 0.22f, 0.12f);

        img.Fill(sandstoneMid);

        int courseH = h / 4;
        for (int row = 0; row < 4; row++)
        {
            int yStart = row * courseH;
            for (int x = 0; x < w; x++)
            {
                img.SetPixel(x, yStart, mortar);
                if (yStart + 1 < h) img.SetPixel(x, yStart + 1, sandstoneLight);
                if (yStart + courseH - 1 < h) img.SetPixel(x, yStart + courseH - 1, sandstoneDark);
            }

            int offset = (row % 2 == 0) ? 0 : w / 4;
            for (int x = offset; x < w; x += w / 2)
            {
                for (int y = yStart; y < yStart + courseH; y++)
                {
                    img.SetPixel(x, y, mortar);
                }
            }
        }

        // Subtle carved hieroglyphic marks on center brick
        img.SetPixel(28, 20, hieroglyphColor);
        img.SetPixel(29, 21, hieroglyphColor);
        img.SetPixel(28, 22, hieroglyphColor);
        img.SetPixel(34, 20, hieroglyphColor);
        img.SetPixel(34, 21, hieroglyphColor);
        img.SetPixel(35, 20, hieroglyphColor);

        return img;
    }

    private static Image BuildDessertFloor(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var waferBase = new Color(0.88f, 0.72f, 0.52f);
        var waferLight = new Color(0.96f, 0.84f, 0.65f);
        var frostingPink = new Color(0.95f, 0.48f, 0.65f);
        var sprinkleBlue = new Color(0.35f, 0.85f, 0.95f);
        var sprinkleYellow = new Color(1.0f, 0.90f, 0.25f);
        var sprinkleWhite = new Color(1.0f, 1.0f, 1.0f);

        img.Fill(waferBase);

        // Waffle grid lines
        for (int x = 0; x < w; x += 16)
        {
            for (int y = 0; y < h; y++)
            {
                img.SetPixel(x, y, frostingPink);
            }
        }
        for (int y = 0; y < h; y += 16)
        {
            for (int x = 0; x < w; x++)
            {
                img.SetPixel(x, y, frostingPink);
            }
        }

        // Wafer cell highlights
        for (int x = 0; x < w; x += 16)
        {
            for (int y = 0; y < h; y += 16)
            {
                if (x + 1 < w && y + 1 < h) img.SetPixel(x + 1, y + 1, waferLight);
            }
        }

        // Rainbow sugar sprinkle dots
        img.SetPixel(8, 8, sprinkleBlue);
        img.SetPixel(9, 8, sprinkleBlue);
        img.SetPixel(24, 24, sprinkleYellow);
        img.SetPixel(40, 10, sprinkleWhite);
        img.SetPixel(54, 42, sprinkleBlue);

        return img;
    }

    private static Image BuildDessertWall(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var chocoDark = new Color(0.24f, 0.12f, 0.08f);
        var chocoMid = new Color(0.42f, 0.24f, 0.16f);
        var chocoLight = new Color(0.58f, 0.35f, 0.24f);
        var glazeWhite = new Color(0.98f, 0.94f, 0.90f);

        img.Fill(chocoMid);

        // Chocolate bar segments (4x2 rectangles)
        int blockW = w / 2;
        int blockH = h / 2;

        for (int bx = 0; bx < 2; bx++)
        {
            for (int by = 0; by < 2; by++)
            {
                int startX = bx * blockW;
                int startY = by * blockH;

                for (int x = startX; x < startX + blockW; x++)
                {
                    img.SetPixel(x, startY, chocoLight);
                    img.SetPixel(x, startY + blockH - 1, chocoDark);
                }
                for (int y = startY; y < startY + blockH; y++)
                {
                    img.SetPixel(startX, y, chocoLight);
                    img.SetPixel(startX + blockW - 1, y, chocoDark);
                }
            }
        }

        // Sugar glaze drippings on top row
        for (int x = 6; x < 26; x++) img.SetPixel(x, 2, glazeWhite);
        img.SetPixel(12, 3, glazeWhite);
        img.SetPixel(12, 4, glazeWhite);
        img.SetPixel(20, 3, glazeWhite);

        return img;
    }

    // ══════════════════════════════════════════════
    //  8. DECORATIVE DECALS (Occult Runes, Tech Cores, Splatters)
    // ══════════════════════════════════════════════

    private static Image BuildRuneDecal(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        var glow = new Color(0.85f, 0.25f, 0.75f, 0.75f);
        var centerGlow = new Color(1.0f, 0.50f, 0.90f, 0.90f);

        float cx = w / 2.0f;
        float cy = h / 2.0f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = new Vector2(x - cx, y - cy).Length();
                if (Mathf.Abs(dist - 24.0f) < 1.4f || Mathf.Abs(dist - 16.0f) < 1.0f)
                {
                    img.SetPixel(x, y, glow);
                }
                else if (dist < 4.0f)
                {
                    img.SetPixel(x, y, centerGlow);
                }
            }
        }

        // 4 Cardinal rune hashes
        for (int i = 18; i <= 28; i++)
        {
            img.SetPixel((int)cx, (int)(cy - i), glow);
            img.SetPixel((int)cx, (int)(cy + i), glow);
            img.SetPixel((int)(cx - i), (int)cy, glow);
            img.SetPixel((int)(cx + i), (int)cy, glow);
        }

        return img;
    }

    private static Image BuildTechDecal(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        var cyan = new Color(0.20f, 0.85f, 1.0f, 0.80f);
        var darkCyan = new Color(0.08f, 0.40f, 0.55f, 0.70f);

        float cx = w / 2.0f;
        float cy = h / 2.0f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = new Vector2(x - cx, y - cy).Length();
                if (Mathf.Abs(dist - 22.0f) < 1.5f)
                {
                    img.SetPixel(x, y, (x % 3 == 0) ? cyan : darkCyan);
                }
                else if (Mathf.Abs(dist - 8.0f) < 1.2f)
                {
                    img.SetPixel(x, y, cyan);
                }
            }
        }

        return img;
    }

    private static Image BuildSandDecal(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        var sand = new Color(0.85f, 0.72f, 0.48f, 0.65f);
        var sandDark = new Color(0.65f, 0.48f, 0.28f, 0.50f);

        for (int y = 4; y < h - 4; y++)
        {
            for (int x = 4; x < w - 4; x++)
            {
                float dx = (x - w / 2.0f) / (w * 0.4f);
                float dy = (y - h / 2.0f) / (h * 0.3f);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    if (y % 6 == 0) img.SetPixel(x, y, sandDark);
                    else if ((x + y) % 3 == 0) img.SetPixel(x, y, sand);
                }
            }
        }
        return img;
    }

    private static Image BuildCandyDecal(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        var pink = new Color(0.98f, 0.45f, 0.65f, 0.90f);
        var cyan = new Color(0.35f, 0.85f, 0.95f, 0.90f);
        var gold = new Color(1.0f, 0.88f, 0.25f, 0.90f);

        int[,] pts = { { 10, 12 }, { 18, 24 }, { 34, 15 }, { 28, 36 }, { 14, 38 }, { 38, 28 } };
        Color[] cols = { pink, cyan, gold, pink, cyan, gold };

        for (int i = 0; i < 6; i++)
        {
            int px = pts[i, 0];
            int py = pts[i, 1];
            Color c = cols[i];
            img.SetPixel(px, py, c);
            img.SetPixel(px + 1, py, c);
            img.SetPixel(px, py + 1, c);
        }
        return img;
    }

    private static Image BuildBloodDecal(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        var bloodDark = new Color(0.48f, 0.06f, 0.12f, 0.70f);
        var bloodRed = new Color(0.75f, 0.12f, 0.20f, 0.85f);

        for (int y = 4; y < h - 4; y++)
        {
            for (int x = 4; x < w - 4; x++)
            {
                float dx = (x - w / 2.0f) / (w * 0.35f);
                float dy = (y - h / 2.0f) / (h * 0.35f);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    img.SetPixel(x, y, ((x + y) % 4 == 0) ? bloodRed : bloodDark);
                }
            }
        }
        // Little splatter droplets
        img.SetPixel(5, 6, bloodRed);
        img.SetPixel(26, 8, bloodRed);
        img.SetPixel(24, 25, bloodDark);
        img.SetPixel(7, 24, bloodDark);

        return img;
    }

    // ══════════════════════════════════════════════
    //  21. THE VEIL BROKER (Occult Shop Merchant)
    // ══════════════════════════════════════════════
    private static Image BuildBrokerSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var darkRobes = new Color(0.12f, 0.09f, 0.18f);
        var robeMid = new Color(0.22f, 0.16f, 0.32f);
        var robeHighlight = new Color(0.34f, 0.26f, 0.48f);
        var hoodShadow = new Color(0.06f, 0.04f, 0.09f);
        var goldTrim = new Color(0.85f, 0.70f, 0.25f);
        var eyeGlow = new Color(1.0f, 0.88f, 0.35f);
        var eyeShine = new Color(1.0f, 0.98f, 0.75f);
        var talismanColor = new Color(0.65f, 0.50f, 0.30f);

        // Lower body / seated robes (Y: 22 to 44)
        for (int y = 22; y <= 44; y++)
        {
            float progress = (y - 22.0f) / 22.0f;
            int halfWidth = (int)Mathf.Lerp(10.0f, 17.0f, progress);
            int startX = (w / 2) - halfWidth;
            int endX = (w / 2) + halfWidth;

            for (int x = startX; x <= endX; x++)
            {
                // Robe drape shading
                float distFromCenter = Mathf.Abs(x - (w / 2.0f));
                Color col = (distFromCenter < 5.0f && y % 3 == 0) ? robeHighlight : ((distFromCenter > halfWidth - 3) ? darkRobes : robeMid);
                img.SetPixel(x, y, col);
            }
        }

        // Gold trim on bottom robe hem
        for (int x = (w / 2) - 16; x <= (w / 2) + 16; x++)
        {
            if (x >= 0 && x < w) img.SetPixel(x, 43, goldTrim);
        }

        // Hood / Shoulders (Y: 6 to 24)
        for (int y = 6; y <= 24; y++)
        {
            float radius = (y < 14) ? Mathf.Sqrt(Mathf.Max(0, 49 - (y - 13) * (y - 13))) + 4 : 11.5f;
            int startX = (int)((w / 2.0f) - radius);
            int endX = (int)((w / 2.0f) + radius);

            for (int x = startX; x <= endX; x++)
            {
                float dx = (x - (w / 2.0f)) / radius;
                Color col = (dx < -0.5f || dx > 0.6f) ? darkRobes : robeMid;
                if (y < 10) col = robeHighlight;
                img.SetPixel(x, y, col);
            }
        }

        // Hood face void opening (deep black shadow)
        for (int y = 11; y <= 21; y++)
        {
            int halfVoid = (y < 16) ? (y - 10) + 3 : (21 - y) + 3;
            for (int x = (w / 2) - halfVoid; x <= (w / 2) + halfVoid; x++)
            {
                img.SetPixel(x, y, hoodShadow);
            }
        }

        // Gold embroidery rim along hood
        for (int y = 9; y <= 22; y++)
        {
            int halfVoid = (y < 16) ? (y - 10) + 4 : (21 - y) + 4;
            int leftRim = (w / 2) - halfVoid;
            int rightRim = (w / 2) + halfVoid;
            if (leftRim >= 0 && leftRim < w) img.SetPixel(leftRim, y, goldTrim);
            if (rightRim >= 0 && rightRim < w) img.SetPixel(rightRim, y, goldTrim);
        }

        // Glowing slitted golden eyes
        int eyeY = 15;
        img.SetPixel((w / 2) - 4, eyeY, eyeGlow);
        img.SetPixel((w / 2) - 3, eyeY, eyeShine);
        img.SetPixel((w / 2) + 3, eyeY, eyeShine);
        img.SetPixel((w / 2) + 4, eyeY, eyeGlow);

        // Occult Talisman / Skull Bead in lap
        for (int ty = 30; ty <= 34; ty++)
        {
            for (int tx = (w / 2) - 3; tx <= (w / 2) + 3; tx++)
            {
                img.SetPixel(tx, ty, talismanColor);
            }
        }
        img.SetPixel((w / 2), 32, goldTrim);

        AddDarkOutline(img, new Color(0.06f, 0.04f, 0.08f));
        return img;
    }

    // ══════════════════════════════════════════════
    //  22. INCENSE BRAZIER (Occult Shop Decor)
    // ══════════════════════════════════════════════
    private static Image BuildBrazierSprite(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var ironDark = new Color(0.12f, 0.10f, 0.15f);
        var ironMid = new Color(0.24f, 0.20f, 0.28f);
        var ironLight = new Color(0.38f, 0.32f, 0.44f);
        var emberCore = new Color(1.0f, 0.92f, 0.65f);
        var emberViolet = new Color(0.72f, 0.25f, 0.95f);
        var emberMagenta = new Color(0.95f, 0.22f, 0.50f);

        // Tripod legs (Y: 12 to 18)
        for (int y = 12; y <= 18; y++)
        {
            float prog = (y - 12.0f) / 6.0f;
            int spread = (int)(prog * 5.0f);
            img.SetPixel((w / 2) - spread, y, ironDark);
            img.SetPixel((w / 2), y, ironMid);
            img.SetPixel((w / 2) + spread, y, ironDark);
        }

        // Metal Bowl (Y: 6 to 12)
        for (int y = 6; y <= 12; y++)
        {
            float radius = (y < 9) ? 6.5f : Mathf.Max(2.0f, 6.5f - (y - 8.5f));
            for (int x = (int)((w / 2.0f) - radius); x <= (int)((w / 2.0f) + radius); x++)
            {
                Color col = (y == 6) ? ironLight : ((x == (int)((w / 2.0f) - radius) || x == (int)((w / 2.0f) + radius)) ? ironDark : ironMid);
                img.SetPixel(x, y, col);
            }
        }

        // Embers inside bowl (Y: 4 to 6)
        for (int x = (w / 2) - 4; x <= (w / 2) + 4; x++)
        {
            img.SetPixel(x, 5, emberViolet);
            if (Mathf.Abs(x - (w / 2)) <= 2)
            {
                img.SetPixel(x, 4, emberCore);
                img.SetPixel(x, 5, emberMagenta);
            }
        }

        AddDarkOutline(img, new Color(0.06f, 0.05f, 0.08f));
        return img;
    }

    private static void AddDarkOutline(Image img, Color outlineColor)
    {
        int w = img.GetWidth();
        int h = img.GetHeight();
        var copy = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        copy.CopyFrom(img);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (copy.GetPixel(x, y).A > 0.01f) continue;

                // Check 4 cardinal neighbors
                bool hasSolidNeighbor =
                    (x > 0 && copy.GetPixel(x - 1, y).A > 0.5f) ||
                    (x < w - 1 && copy.GetPixel(x + 1, y).A > 0.5f) ||
                    (y > 0 && copy.GetPixel(x, y - 1).A > 0.5f) ||
                    (y < h - 1 && copy.GetPixel(x, y + 1).A > 0.5f);

                if (hasSolidNeighbor)
                {
                    img.SetPixel(x, y, outlineColor);
                }
            }
        }
    }
}
