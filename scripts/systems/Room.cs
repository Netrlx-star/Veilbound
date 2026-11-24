using Godot;
using System.Collections.Generic;
using Veilbound.Enemies;

namespace Veilbound.Systems;

public enum RoomThemeType
{
    Dungeon,
    Spaceship,
    DesertTomb,
    SweetDessert
}

/// <summary>
/// Represents a single room in the dungeon. Handles walls, doors,
/// themed decorations, enemy spawning, and room clearing logic.
/// 
/// Room layout:
///   ┌────────[Top Door]────────┐
///   │                          │
///  [L]       Playable         [R]
///  [D]        Area            [D]
///   │                          │
///   └───────[Bottom Door]──────┘
/// 
/// Room size: 1152 x 624 (playable area within 1280x720 viewport)
/// Wall thickness: 64px
/// </summary>
public partial class Room : Node2D
{
    // ── Room dimensions ──
    public const float RoomWidth = 1280.0f;
    public const float RoomHeight = 720.0f;
    public const float WallThickness = 64.0f;
    public const float DoorWidth = 80.0f;

    // ── Theme & Decor ──
    public RoomThemeType Theme { get; set; } = RoomThemeType.Dungeon;
    public int RoomType { get; set; } = 1; // 1 = Normal, 2 = Boss, 3 = Treasure, 4 = Shop

    // ── Room state ──
    public bool IsCleared { get; private set; } = false;
    public int EnemiesRemaining { get; private set; } = 0;
    private double _transitionCooldown = 0.35;

    // ── Door connections (null = no door on that side) ──
    public bool HasTopDoor { get; set; } = false;
    public bool HasBottomDoor { get; set; } = false;
    public bool HasLeftDoor { get; set; } = false;
    public bool HasRightDoor { get; set; } = false;

    // ── Door locks (requires key to enter target room) ──
    public Dictionary<string, bool> DoorLocked { get; } = new();
    public Dictionary<string, int> DoorTargetType { get; } = new(); // 2 = Boss, 3 = Treasure, 4 = Shop

    // ── References ──
    private List<BaseEnemy> _enemies = new();
    private List<Area2D> _doors = new();
    private Node2D _wallsContainer;
    private Node2D _doorsContainer;
    private Node2D _decorContainer;
    private Node2D _enemiesContainer;
    private Node2D _atmosphereContainer;

    // ── Signals ──
    [Signal] public delegate void RoomClearedEventHandler();
    [Signal] public delegate void DoorEnteredEventHandler(string direction);

    public override void _Ready()
    {
        _wallsContainer = new Node2D { Name = "Walls" };
        _doorsContainer = new Node2D { Name = "Doors" };
        _decorContainer = new Node2D { Name = "Decorations" };
        _enemiesContainer = new Node2D { Name = "Enemies" };
        _atmosphereContainer = new Node2D { Name = "Atmosphere" };

        AddChild(_wallsContainer);
        AddChild(_doorsContainer);
        AddChild(_decorContainer);
        AddChild(_enemiesContainer);
        AddChild(_atmosphereContainer);

        BuildWalls();
        BuildDoors();
        BuildFloor();
        BuildDecorations();
        BuildAtmosphere();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_transitionCooldown > 0)
        {
            _transitionCooldown -= delta;
            return;
        }

        if (!IsCleared) return;

        // Active overlap check so player never gets stuck in doorways
        foreach (var door in _doors)
        {
            foreach (var body in door.GetOverlappingBodies())
            {
                if (body.IsInGroup("player"))
                {
                    _transitionCooldown = 0.5;
                    EmitSignal(SignalName.DoorEntered, (string)door.GetMeta("direction"));
                    return;
                }
            }
        }
    }

    // ══════════════════════════════════════════════
    //  ROOM CONSTRUCTION
    // ══════════════════════════════════════════════

    private void BuildFloor()
    {
        AssetGenerator.InitializeAssets();

        Texture2D floorTex = Theme switch
        {
            RoomThemeType.Spaceship => AssetGenerator.SpaceshipFloorTexture,
            RoomThemeType.DesertTomb => AssetGenerator.DesertFloorTexture,
            RoomThemeType.SweetDessert => AssetGenerator.DessertFloorTexture,
            _ => AssetGenerator.FloorTileTexture
        };

        var floor = new TextureRect
        {
            Texture = floorTex,
            Position = Vector2.Zero,
            Size = new Vector2(RoomWidth, RoomHeight),
            StretchMode = TextureRect.StretchModeEnum.Tile,
            ZIndex = -10
        };
        AddChild(floor);
        MoveChild(floor, 0);
    }

    private void BuildDecorations()
    {
        Texture2D mainDecal = Theme switch
        {
            RoomThemeType.Spaceship => AssetGenerator.DecalTechTexture,
            RoomThemeType.DesertTomb => AssetGenerator.DecalSandTexture,
            RoomThemeType.SweetDessert => AssetGenerator.DecalCandyTexture,
            _ => AssetGenerator.DecalRuneTexture
        };

        Texture2D secondaryDecal = Theme switch
        {
            RoomThemeType.SweetDessert => AssetGenerator.DecalCandyTexture,
            RoomThemeType.DesertTomb => AssetGenerator.DecalSandTexture,
            RoomThemeType.Spaceship => AssetGenerator.DecalTechTexture,
            _ => AssetGenerator.DecalBloodTexture
        };

        // Scatter 4-7 ambient floor decals across room
        int count = GD.RandRange(4, 7);
        for (int i = 0; i < count; i++)
        {
            var decalTex = (i % 2 == 0) ? mainDecal : secondaryDecal;
            var decalRect = new TextureRect
            {
                Texture = decalTex,
                Position = new Vector2(
                    (float)GD.RandRange(WallThickness + 60, RoomWidth - WallThickness - 120),
                    (float)GD.RandRange(WallThickness + 60, RoomHeight - WallThickness - 120)
                ),
                Modulate = new Color(1, 1, 1, (float)GD.RandRange(0.6, 0.95)),
                ZIndex = -5,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _decorContainer.AddChild(decalRect);
        }
    }

    private void BuildAtmosphere()
    {
        // 1. Ambient Floating Dust / Spore / Ember Particles
        // (Vignette & corner lighting decor belong exclusively to Main Menu & Pause Menu, not in-run rooms)
        if (RoomType == 2) // Boss Room: Rising crimson soul embers
        {
            var bossEmbers = AtmosphereGenerator.CreateEmberParticles(
                new Vector2(RoomWidth / 2.0f, RoomHeight * 0.78f),
                new Color(0.95f, 0.28f, 0.18f, 0.65f),
                26
            );
            _atmosphereContainer.AddChild(bossEmbers);
        }
        else if (RoomType == 4) // Shop ("Veil Bazaar"): Warm golden coin sparkles
        {
            var shopEmbers = AtmosphereGenerator.CreateEmberParticles(
                new Vector2(RoomWidth / 2.0f, RoomHeight * 0.72f),
                new Color(1.0f, 0.85f, 0.30f, 0.50f),
                18
            );
            _atmosphereContainer.AddChild(shopEmbers);
        }

        // Ambient airborne dust motes tailored per room biome
        Color dustTint = Theme switch
        {
            RoomThemeType.DesertTomb => new Color(0.88f, 0.78f, 0.48f, 0.24f),
            RoomThemeType.Spaceship => new Color(0.35f, 0.85f, 1.0f, 0.24f),
            RoomThemeType.SweetDessert => new Color(0.95f, 0.65f, 0.85f, 0.28f),
            _ => (RoomType == 2) ? new Color(0.85f, 0.25f, 0.25f, 0.28f) : new Color(0.65f, 0.75f, 0.90f, 0.22f)
        };
        var dustParticles = AtmosphereGenerator.CreateAmbientDustParticles(new Vector2(RoomWidth, RoomHeight), dustTint, 26);
        _atmosphereContainer.AddChild(dustParticles);
    }

    private void BuildWalls()
    {
        var wallColor = new Color(0.25f, 0.22f, 0.3f);

        // Top wall
        if (HasTopDoor)
        {
            float doorStart = (RoomWidth - DoorWidth) / 2.0f;
            CreateWallSegment(new Vector2(0, 0), new Vector2(doorStart, WallThickness), wallColor);
            CreateWallSegment(new Vector2(doorStart + DoorWidth, 0), new Vector2(RoomWidth - doorStart - DoorWidth, WallThickness), wallColor);
        }
        else
        {
            CreateWallSegment(new Vector2(0, 0), new Vector2(RoomWidth, WallThickness), wallColor);
        }

        // Bottom wall
        if (HasBottomDoor)
        {
            float doorStart = (RoomWidth - DoorWidth) / 2.0f;
            CreateWallSegment(new Vector2(0, RoomHeight - WallThickness), new Vector2(doorStart, WallThickness), wallColor);
            CreateWallSegment(new Vector2(doorStart + DoorWidth, RoomHeight - WallThickness), new Vector2(RoomWidth - doorStart - DoorWidth, WallThickness), wallColor);
        }
        else
        {
            CreateWallSegment(new Vector2(0, RoomHeight - WallThickness), new Vector2(RoomWidth, WallThickness), wallColor);
        }

        // Left wall
        if (HasLeftDoor)
        {
            float doorStart = (RoomHeight - DoorWidth) / 2.0f;
            CreateWallSegment(new Vector2(0, 0), new Vector2(WallThickness, doorStart), wallColor);
            CreateWallSegment(new Vector2(0, doorStart + DoorWidth), new Vector2(WallThickness, RoomHeight - doorStart - DoorWidth), wallColor);
        }
        else
        {
            CreateWallSegment(new Vector2(0, 0), new Vector2(WallThickness, RoomHeight), wallColor);
        }

        // Right wall
        if (HasRightDoor)
        {
            float doorStart = (RoomHeight - DoorWidth) / 2.0f;
            CreateWallSegment(new Vector2(RoomWidth - WallThickness, 0), new Vector2(WallThickness, doorStart), wallColor);
            CreateWallSegment(new Vector2(RoomWidth - WallThickness, doorStart + DoorWidth), new Vector2(WallThickness, RoomHeight - doorStart - DoorWidth), wallColor);
        }
        else
        {
            CreateWallSegment(new Vector2(RoomWidth - WallThickness, 0), new Vector2(WallThickness, RoomHeight), wallColor);
        }
    }

    private void CreateWallSegment(Vector2 position, Vector2 size, Color color)
    {
        var wall = new StaticBody2D();
        wall.Position = position + size / 2.0f;
        wall.CollisionLayer = 1; // walls layer

        var shape = new RectangleShape2D { Size = size };
        var collision = new CollisionShape2D { Shape = shape };
        wall.AddChild(collision);

        Texture2D wallTex = Theme switch
        {
            RoomThemeType.Spaceship => AssetGenerator.SpaceshipWallTexture,
            RoomThemeType.DesertTomb => AssetGenerator.DesertWallTexture,
            RoomThemeType.SweetDessert => AssetGenerator.DessertWallTexture,
            _ => AssetGenerator.WallTileTexture
        };

        var texture = new TextureRect
        {
            Texture = wallTex,
            Position = -size / 2.0f,
            Size = size,
            StretchMode = TextureRect.StretchModeEnum.Tile
        };
        wall.AddChild(texture);

        _wallsContainer.AddChild(wall);
    }

    private void BuildDoors()
    {
        if (HasTopDoor)    CreateDoor("top",    new Vector2(RoomWidth / 2, WallThickness / 2), "top");
        if (HasBottomDoor) CreateDoor("bottom", new Vector2(RoomWidth / 2, RoomHeight - WallThickness / 2), "bottom");
        if (HasLeftDoor)   CreateDoor("left",   new Vector2(WallThickness / 2, RoomHeight / 2), "left");
        if (HasRightDoor)  CreateDoor("right",  new Vector2(RoomWidth - WallThickness / 2, RoomHeight / 2), "right");
    }

    private void CreateDoor(string name, Vector2 visualPosition, string direction)
    {
        var door = new Area2D();
        door.Name = $"Door_{name}";
        door.CollisionLayer = 64; // doors layer (bit 7)
        door.CollisionMask = 2;   // detect player (bit 2)

        // Calculate generous trigger area extending safely into room
        Vector2 triggerPos = visualPosition;
        Vector2 triggerSize = new Vector2(DoorWidth + 24, DoorWidth + 30);

        switch (direction)
        {
            case "top":
                triggerPos.Y += 15.0f;
                break;
            case "bottom":
                triggerPos.Y -= 15.0f;
                break;
            case "left":
                triggerPos.X += 15.0f;
                triggerSize = new Vector2(DoorWidth + 30, DoorWidth + 24);
                break;
            case "right":
                triggerPos.X -= 15.0f;
                triggerSize = new Vector2(DoorWidth + 30, DoorWidth + 24);
                break;
        }

        door.Position = triggerPos;

        var shape = new RectangleShape2D { Size = triggerSize };
        var collision = new CollisionShape2D { Shape = shape };
        door.AddChild(collision);

        // Gothic door archway texture
        bool isLocked = DoorLocked.TryGetValue(direction, out bool locked) && locked;
        Color doorColor = isLocked ? new Color(0.95f, 0.75f, 0.20f) : new Color(0.5f, 0.9f, 0.5f);

        var visual = new TextureRect
        {
            Texture = AssetGenerator.DoorFrameTexture,
            Position = visualPosition - triggerPos - new Vector2(DoorWidth / 2, DoorWidth / 2),
            Size = new Vector2(DoorWidth, DoorWidth),
            Modulate = doorColor
        };
        door.AddChild(visual);
        visual.Name = "Visual";

        // If locked, add a brass padlock icon
        if (isLocked)
        {
            var lockIcon = new Sprite2D
            {
                Name = "LockIcon",
                Texture = PlaceholderSprite.CreateKeyTexture(),
                Position = visualPosition - triggerPos,
                Scale = new Vector2(1.2f, 1.2f)
            };
            door.AddChild(lockIcon);
        }
        else if (DoorTargetType.TryGetValue(direction, out int targetType))
        {
            // Diegetic doorway heraldry for special rooms (Boss, Treasure, Shop)
            string sigil = targetType switch
            {
                2 => "💀",
                3 => "👑",
                4 => "⚖️",
                _ => ""
            };

            if (!string.IsNullOrEmpty(sigil))
            {
                var sigilLabel = new Label
                {
                    Text = sigil,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Position = visualPosition - triggerPos - new Vector2(16, 16),
                    Size = new Vector2(32, 32)
                };
                sigilLabel.AddThemeFontSizeOverride("font_size", 16);

                Color sigilCol = targetType switch
                {
                    2 => new Color(0.95f, 0.25f, 0.30f),
                    3 => new Color(1.0f, 0.88f, 0.30f),
                    4 => new Color(0.90f, 0.78f, 0.35f),
                    _ => Colors.White
                };
                sigilLabel.AddThemeColorOverride("font_color", sigilCol);
                door.AddChild(sigilLabel);

                if (targetType == 2)
                {
                    // Pulsating boss doom aura on door
                    var pulseTween = sigilLabel.CreateTween().SetLoops();
                    pulseTween.TweenProperty(sigilLabel, "scale", new Vector2(1.25f, 1.25f), 0.5)
                        .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    pulseTween.TweenProperty(sigilLabel, "scale", Vector2.One, 0.5)
                        .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                }
            }
        }

        // Store direction as metadata
        door.SetMeta("direction", direction);
        door.SetMeta("locked", isLocked);

        door.BodyEntered += (body) =>
        {
            if (body is Player.PlayerController player && IsCleared && _transitionCooldown <= 0)
            {
                bool doorIsLocked = door.HasMeta("locked") && (bool)door.GetMeta("locked");
                if (doorIsLocked)
                {
                    // Check if player has a key
                    if (player.Stats.Keys > 0)
                    {
                        // Consume key and unlock door
                        player.UseKey();
                        door.SetMeta("locked", false);
                        DoorLocked[direction] = false;

                        // Pop unlock effect on visual
                        visual.Modulate = new Color(0.5f, 0.9f, 0.5f);
                        var lockNode = door.GetNodeOrNull<Sprite2D>("LockIcon");
                        if (lockNode != null)
                        {
                            var lockTween = lockNode.CreateTween();
                            lockTween.TweenProperty(lockNode, "scale", Vector2.One * 2.0f, 0.15);
                            lockTween.Parallel().TweenProperty(lockNode, "modulate:a", 0.0f, 0.15);
                            lockTween.Chain().TweenCallback(Callable.From(lockNode.QueueFree));
                        }

                        // Unlock target room in manager so returning is free
                        RoomManager.Instance?.UnlockDoorConnection(this, direction);

                        _transitionCooldown = 0.5;
                        EmitSignal(SignalName.DoorEntered, direction);
                    }
                    else
                    {
                        // Flash lock red to indicate locked door needing key
                        var lockNode = door.GetNodeOrNull<Sprite2D>("LockIcon");
                        if (lockNode != null)
                        {
                            var deniedTween = lockNode.CreateTween();
                            deniedTween.TweenProperty(lockNode, "modulate", new Color(1, 0.1f, 0.1f), 0.1);
                            deniedTween.TweenProperty(lockNode, "modulate", Colors.White, 0.15);
                        }
                    }
                    return;
                }

                _transitionCooldown = 0.5;
                EmitSignal(SignalName.DoorEntered, direction);
            }
        };

        _doorsContainer.AddChild(door);
        _doors.Add(door);
    }

    /// <summary>
    /// Applies an Isaac-style obstacle layout pattern (Rocks and Pots).
    /// </summary>
    public void SetObstacleLayout(Environments.RoomLayoutType type)
    {
        Environments.RoomLayoutGenerator.GenerateLayout(this, type);
    }

    // ══════════════════════════════════════════════
    //  ENEMY MANAGEMENT
    // ══════════════════════════════════════════════

    /// <summary>
    /// Spawn enemies in the room. Call this after room is built.
    /// </summary>
    public void SpawnEnemies(PackedScene[] enemyScenes, int count)
    {
        EnemiesRemaining = count;

        for (int i = 0; i < count; i++)
        {
            var scene = enemyScenes[GD.RandRange(0, enemyScenes.Length - 1)];
            var enemy = scene.Instantiate<BaseEnemy>();

            // Random position within playable area
            float margin = WallThickness + 40.0f;
            enemy.GlobalPosition = new Vector2(
                (float)GD.RandRange(margin, RoomWidth - margin),
                (float)GD.RandRange(margin, RoomHeight - margin)
            );

            enemy.EnemyDied += OnEnemyDied;
            _enemiesContainer.AddChild(enemy);
            _enemies.Add(enemy);
        }

        // Lock doors when enemies are present
        UpdateDoorVisuals(locked: true);
    }

    private void OnEnemyDied()
    {
        EnemiesRemaining--;

        if (EnemiesRemaining <= 0)
        {
            ClearRoom();
        }
    }

    private void ClearRoom()
    {
        IsCleared = true;
        UpdateDoorVisuals(locked: false);

        // Visceral combat juice: camera shake + door unseal pop
        CameraShaker.Shake(0.20f);

        // Chamber purged celebration banner
        UI.HUD.Instance?.ShowRoomClearedBanner();

        foreach (var door in _doors)
        {
            var visual = door.GetNodeOrNull<TextureRect>("Visual");
            if (visual != null)
            {
                visual.PivotOffset = visual.Size / 2.0f;
                var unsealTween = visual.CreateTween();
                unsealTween.TweenProperty(visual, "scale", new Vector2(1.22f, 1.22f), 0.12)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                unsealTween.TweenProperty(visual, "scale", Vector2.One, 0.15)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            }

            SpawnDoorUnsealParticles(door.GlobalPosition);
        }

        EmitSignal(SignalName.RoomCleared);
        GameManager.Instance?.OnRoomCleared();
    }

    private void SpawnDoorUnsealParticles(Vector2 pos)
    {
        var particles = new CpuParticles2D
        {
            GlobalPosition = pos,
            Emitting = true,
            OneShot = true,
            Explosiveness = 0.95f,
            Amount = 14,
            Lifetime = 0.45,
            Direction = new Vector2(0, -1),
            Spread = 180.0f,
            Gravity = new Vector2(0, 40),
            InitialVelocityMin = 35.0f,
            InitialVelocityMax = 80.0f,
            ScaleAmountMin = 2.0f,
            ScaleAmountMax = 3.5f,
            Color = new Color(0.40f, 0.95f, 0.55f, 0.9f)
        };
        GetTree().CurrentScene?.AddChild(particles);
        particles.GetTree()?.CreateTimer(0.6).Connect("timeout", Callable.From(particles.QueueFree));
    }

    /// <summary>
    /// Lock the room (doors become red/solid) during combat.
    /// </summary>
    public void LockRoom()
    {
        IsCleared = false;
        UpdateDoorVisuals(locked: true);
    }

    /// <summary>
    /// Mark this room as already cleared (for revisiting).
    /// </summary>
    public void SetCleared()
    {
        IsCleared = true;
        UpdateDoorVisuals(locked: false);
    }

    private void UpdateDoorVisuals(bool locked)
    {
        foreach (var door in _doors)
        {
            var visual = door.GetNodeOrNull<TextureRect>("Visual");
            if (visual != null)
            {
                visual.Modulate = locked
                    ? new Color(0.9f, 0.25f, 0.25f)   // Red = locked
                    : new Color(0.45f, 0.95f, 0.5f);  // Green = open
            }
        }
    }

    /// <summary>
    /// Get the spawn position for the player when entering from a direction.
    /// </summary>
    public Vector2 GetPlayerSpawnPosition(string fromDirection)
    {
        float margin = WallThickness + 55.0f;

        return fromDirection switch
        {
            "top"    => new Vector2(RoomWidth / 2, margin),
            "bottom" => new Vector2(RoomWidth / 2, RoomHeight - margin),
            "left"   => new Vector2(margin, RoomHeight / 2),
            "right"  => new Vector2(RoomWidth - margin, RoomHeight / 2),
            _        => new Vector2(RoomWidth / 2, RoomHeight / 2)
        };
    }
}

/// <summary>
/// Forward reference — needed for Room to reference BaseEnemy.
/// Actual class is in BaseEnemy.cs
/// </summary>
// Note: BaseEnemy is already defined in enemies/BaseEnemy.cs
