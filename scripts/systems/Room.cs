using Godot;
using System.Collections.Generic;
using Veilbound.Enemies;

namespace Veilbound.Systems;

/// <summary>
/// Represents a single room in the dungeon. Handles walls, doors,
/// enemy spawning, and room clearing logic.
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

    // ── Room state ──
    public bool IsCleared { get; private set; } = false;
    public int EnemiesRemaining { get; private set; } = 0;

    // ── Door connections (null = no door on that side) ──
    public bool HasTopDoor { get; set; } = false;
    public bool HasBottomDoor { get; set; } = false;
    public bool HasLeftDoor { get; set; } = false;
    public bool HasRightDoor { get; set; } = false;

    // ── References ──
    private List<BaseEnemy> _enemies = new();
    private List<Area2D> _doors = new();
    private Node2D _wallsContainer;
    private Node2D _doorsContainer;
    private Node2D _enemiesContainer;

    // ── Signals ──
    [Signal] public delegate void RoomClearedEventHandler();
    [Signal] public delegate void DoorEnteredEventHandler(string direction);

    public override void _Ready()
    {
        _wallsContainer = new Node2D { Name = "Walls" };
        _doorsContainer = new Node2D { Name = "Doors" };
        _enemiesContainer = new Node2D { Name = "Enemies" };

        AddChild(_wallsContainer);
        AddChild(_doorsContainer);
        AddChild(_enemiesContainer);

        BuildWalls();
        BuildDoors();
        BuildFloor();
    }

    // ══════════════════════════════════════════════
    //  ROOM CONSTRUCTION
    // ══════════════════════════════════════════════

    private void BuildFloor()
    {
        // Dark floor background
        var floor = new ColorRect
        {
            Color = new Color(0.12f, 0.1f, 0.14f),
            Position = Vector2.Zero,
            Size = new Vector2(RoomWidth, RoomHeight),
            ZIndex = -10
        };
        AddChild(floor);
        MoveChild(floor, 0);
    }

    private void BuildWalls()
    {
        var wallColor = new Color(0.25f, 0.22f, 0.3f);

        // Top wall (with gap for door if needed)
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

        // Visual
        var rect = new ColorRect
        {
            Color = color,
            Position = -size / 2.0f,
            Size = size
        };
        wall.AddChild(rect);

        _wallsContainer.AddChild(wall);
    }

    private void BuildDoors()
    {
        if (HasTopDoor)    CreateDoor("top",    new Vector2(RoomWidth / 2, WallThickness / 2), "top");
        if (HasBottomDoor) CreateDoor("bottom", new Vector2(RoomWidth / 2, RoomHeight - WallThickness / 2), "bottom");
        if (HasLeftDoor)   CreateDoor("left",   new Vector2(WallThickness / 2, RoomHeight / 2), "left");
        if (HasRightDoor)  CreateDoor("right",  new Vector2(RoomWidth - WallThickness / 2, RoomHeight / 2), "right");
    }

    private void CreateDoor(string name, Vector2 position, string direction)
    {
        var door = new Area2D();
        door.Name = $"Door_{name}";
        door.Position = position;
        door.CollisionLayer = 64; // doors layer (bit 7)
        door.CollisionMask = 2;   // detect player (bit 2)

        var shape = new RectangleShape2D { Size = new Vector2(DoorWidth, DoorWidth) };
        var collision = new CollisionShape2D { Shape = shape };
        door.AddChild(collision);

        // Door visual (simple colored rectangle)
        var visual = new ColorRect
        {
            Color = new Color(0.5f, 0.4f, 0.2f),
            Position = new Vector2(-DoorWidth / 2, -DoorWidth / 2),
            Size = new Vector2(DoorWidth, DoorWidth)
        };
        door.AddChild(visual);
        visual.Name = "Visual";

        // Store direction as metadata
        door.SetMeta("direction", direction);

        door.BodyEntered += (body) =>
        {
            if (body.IsInGroup("player") && IsCleared)
            {
                EmitSignal(SignalName.DoorEntered, direction);
            }
        };

        _doorsContainer.AddChild(door);
        _doors.Add(door);
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
        EmitSignal(SignalName.RoomCleared);
        GameManager.Instance?.OnRoomCleared();
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
            var visual = door.GetNodeOrNull<ColorRect>("Visual");
            if (visual != null)
            {
                visual.Color = locked
                    ? new Color(0.6f, 0.2f, 0.2f)   // Red = locked
                    : new Color(0.3f, 0.6f, 0.3f);    // Green = open
            }
        }
    }

    /// <summary>
    /// Get the spawn position for the player when entering from a direction.
    /// </summary>
    public Vector2 GetPlayerSpawnPosition(string fromDirection)
    {
        float margin = WallThickness + 30.0f;

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
