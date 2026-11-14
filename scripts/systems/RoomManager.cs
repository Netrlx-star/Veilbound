using Godot;
using System.Collections.Generic;

namespace Veilbound.Systems;

/// <summary>
/// Manages the dungeon floor layout. Generates a grid of rooms,
/// handles transitions between rooms, and tracks which rooms
/// have been visited/cleared.
/// 
/// Floor layout uses a simple grid-based generation:
/// - Start room at center
/// - Rooms branch out in cardinal directions
/// - Special rooms placed at dead ends
/// </summary>
public partial class RoomManager : Node2D
{
    // ── Floor layout ──
    private const int GridSize = 9;           // 9x9 possible positions
    private const int GridCenter = 4;         // Center of the grid
    private const int MinRooms = 6;
    private const int MaxRooms = 10;

    private int[,] _grid;                     // 0 = empty, 1 = normal, 2 = boss, 3 = treasure
    private Dictionary<Vector2I, Room> _rooms = new();
    private Vector2I _currentRoomPos;
    private Room _currentRoom;

    // ── Persistent Floor State ──
    private HashSet<Vector2I> _visitedRooms = new();
    private HashSet<Vector2I> _clearedRooms = new();
    private HashSet<Vector2I> _takenItemRooms = new();
    private HashSet<Vector2I> _unlockedRooms = new(); // Stores rooms whose locked doors were already unlocked

    public static RoomManager Instance { get; private set; }
    public int[,] Grid => _grid;
    public HashSet<Vector2I> VisitedRooms => _visitedRooms;
    public HashSet<Vector2I> ClearedRooms => _clearedRooms;
    public Dictionary<Vector2I, RoomThemeType> RoomThemes => _roomThemes;
    public Vector2I CurrentRoomPos => _currentRoomPos;

    [Signal] public delegate void RoomEnteredEventHandler(Vector2I currentPos);
    [Signal] public delegate void BossHealthChangedEventHandler(string name, float current, float max);
    [Signal] public delegate void BossDefeatedEventHandler();

    // ── Enemy scenes ──
    private PackedScene _slimeEnemyScene;
    private PackedScene _flyEnemyScene;
    private PackedScene _chaserEnemyScene;
    private PackedScene _shooterEnemyScene;
    private PackedScene _bossScene;
    private PackedScene _trapdoorScene;

    // ── Themes ──
    private Dictionary<Vector2I, RoomThemeType> _roomThemes = new();

    // ── Player reference ──
    private Node2D _player;

    public override void _Ready()
    {
        Instance = this;

        _slimeEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/SlimeEnemy.tscn");
        _flyEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/FlyEnemy.tscn");
        _chaserEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/ChaserEnemy.tscn");
        _shooterEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/ShooterEnemy.tscn");
        _bossScene = GD.Load<PackedScene>("res://scenes/bosses/BossEnemy.tscn");
        _trapdoorScene = GD.Load<PackedScene>("res://scenes/rooms/Trapdoor.tscn");

        var gm = GameManager.Instance;
        if (gm != null && gm.ShouldLoadSavedRun && gm.CachedSavedRun != null)
        {
            LoadSavedFloor(gm.CachedSavedRun);
            gm.ShouldLoadSavedRun = false;
        }
        else
        {
            GenerateFloor();
            EnterRoom(_currentRoomPos, "center");
        }
    }

    private void LoadSavedFloor(SavedRunData data)
    {
        _grid = new int[GridSize, GridSize];
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                _grid[x, y] = data.GridFlat[y * 9 + x];
            }
        }

        _visitedRooms.Clear();
        foreach (var v in data.VisitedRooms)
        {
            if (v.Length >= 2) _visitedRooms.Add(new Vector2I(v[0], v[1]));
        }

        _clearedRooms.Clear();
        foreach (var c in data.ClearedRooms)
        {
            if (c.Length >= 2) _clearedRooms.Add(new Vector2I(c[0], c[1]));
        }

        _roomThemes.Clear();
        foreach (var kvp in data.RoomThemes)
        {
            var parts = kvp.Key.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                _roomThemes[new Vector2I(x, y)] = (RoomThemeType)kvp.Value;
            }
        }

        _currentRoomPos = new Vector2I(data.CurrentRoomX, data.CurrentRoomY);

        EnterRoom(_currentRoomPos, "center");

        // Restore loaded player stats
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (_player is Player.PlayerController pc)
        {
            pc.Stats.MaxHealth = data.MaxHealth;
            pc.Stats.CurrentHealth = data.CurrentHealth;
            pc.Stats.Damage = data.Damage;
            pc.Stats.FireRate = data.FireRate;
            pc.Stats.MoveSpeed = data.MoveSpeed;
            pc.Stats.ProjectileSpeed = data.ProjectileSpeed;
            pc.Stats.ProjectileRange = data.ProjectileRange;
            pc.Stats.Multishot = data.Multishot;
            pc.Stats.Coins = data.Coins;
            pc.Stats.Bombs = data.Bombs;
            pc.Stats.Keys = data.Keys;
            pc.Stats.EnemiesKilled = data.EnemiesKilled;
            pc.Stats.ItemsCollected = data.ItemsCollected;

            pc.NotifyStatsChanged();
        }
    }

    // ══════════════════════════════════════════════
    //  FLOOR GENERATION
    // ══════════════════════════════════════════════

    private void GenerateFloor()
    {
        _grid = new int[GridSize, GridSize];
        _roomThemes.Clear();
        var roomPositions = new List<Vector2I>();

        // Start room at center
        var start = new Vector2I(GridCenter, GridCenter);
        _grid[start.X, start.Y] = 1;
        _roomThemes[start] = RoomThemeType.Dungeon;
        roomPositions.Add(start);
        _currentRoomPos = start;

        int targetRooms = (int)GD.RandRange(MinRooms, MaxRooms);
        var candidates = new List<Vector2I>();
        AddNeighborCandidates(start, candidates, roomPositions);

        // Grow rooms from candidates
        while (roomPositions.Count < targetRooms && candidates.Count > 0)
        {
            int idx = (int)GD.RandRange(0, candidates.Count - 1);
            var pos = candidates[idx];
            candidates.RemoveAt(idx);

            // Allow natural branch connections (up to 3 neighbors) without dense 2x2 blocks
            if (CountAdjacentRooms(pos, roomPositions) <= 3 && !WouldCreate2x2(pos, roomPositions))
            {
                _grid[pos.X, pos.Y] = 1;

                // Varied atmospheric themes
                RoomThemeType[] availableThemes = {
                    RoomThemeType.Dungeon,
                    RoomThemeType.Spaceship,
                    RoomThemeType.DesertTomb,
                    RoomThemeType.SweetDessert
                };
                _roomThemes[pos] = availableThemes[GD.RandRange(0, availableThemes.Length - 1)];

                roomPositions.Add(pos);
                AddNeighborCandidates(pos, candidates, roomPositions);
            }
        }

        // Place boss room at the farthest dead end from start
        Vector2I farthest = start;
        int maxDist = 0;
        foreach (var pos in roomPositions)
        {
            int dist = Mathf.Abs(pos.X - start.X) + Mathf.Abs(pos.Y - start.Y);
            int neighbors = CountAdjacentRooms(pos, roomPositions);
            if (dist > maxDist && neighbors == 1 && pos != start)
            {
                maxDist = dist;
                farthest = pos;
            }
        }
        if (farthest != start)
        {
            _grid[farthest.X, farthest.Y] = 2; // Boss room
            _roomThemes[farthest] = RoomThemeType.Dungeon;
        }

        // Place treasure room at another dead end
        foreach (var pos in roomPositions)
        {
            int neighbors = CountAdjacentRooms(pos, roomPositions);
            if (neighbors == 1 && pos != start && _grid[pos.X, pos.Y] == 1)
            {
                _grid[pos.X, pos.Y] = 3; // Treasure room
                _roomThemes[pos] = (GD.Randf() < 0.5f) ? RoomThemeType.SweetDessert : RoomThemeType.DesertTomb;
                break;
            }
        }

        // Place shop room at another dead end or branch (requires a key to unlock)
        foreach (var pos in roomPositions)
        {
            int neighbors = CountAdjacentRooms(pos, roomPositions);
            if (neighbors == 1 && pos != start && _grid[pos.X, pos.Y] == 1)
            {
                _grid[pos.X, pos.Y] = 4; // Shop room
                _roomThemes[pos] = RoomThemeType.Dungeon;
                break;
            }
        }

        GD.Print($"Floor generated with {roomPositions.Count} rooms");
    }

    private bool WouldCreate2x2(Vector2I pos, List<Vector2I> rooms)
    {
        if (rooms.Contains(pos + Vector2I.Right) && rooms.Contains(pos + Vector2I.Up) && rooms.Contains(pos + Vector2I.Right + Vector2I.Up)) return true;
        if (rooms.Contains(pos + Vector2I.Left) && rooms.Contains(pos + Vector2I.Up) && rooms.Contains(pos + Vector2I.Left + Vector2I.Up)) return true;
        if (rooms.Contains(pos + Vector2I.Right) && rooms.Contains(pos + Vector2I.Down) && rooms.Contains(pos + Vector2I.Right + Vector2I.Down)) return true;
        if (rooms.Contains(pos + Vector2I.Left) && rooms.Contains(pos + Vector2I.Down) && rooms.Contains(pos + Vector2I.Left + Vector2I.Down)) return true;

        return false;
    }

    private void AddNeighborCandidates(Vector2I pos, List<Vector2I> candidates, List<Vector2I> existing)
    {
        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
        foreach (var dir in dirs)
        {
            var neighbor = pos + dir;
            if (neighbor.X >= 0 && neighbor.X < GridSize &&
                neighbor.Y >= 0 && neighbor.Y < GridSize &&
                !existing.Contains(neighbor) && !candidates.Contains(neighbor))
            {
                candidates.Add(neighbor);
            }
        }
    }

    private int CountAdjacentRooms(Vector2I pos, List<Vector2I> rooms)
    {
        int count = 0;
        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
        foreach (var dir in dirs)
        {
            if (rooms.Contains(pos + dir)) count++;
        }
        return count;
    }

    // ══════════════════════════════════════════════
    //  ROOM TRANSITIONS
    // ══════════════════════════════════════════════

    public void EnterRoom(Vector2I gridPos, string fromDirection)
    {
        // Remove old room
        if (_currentRoom != null)
        {
            _currentRoom.QueueFree();
        }

        _currentRoomPos = gridPos;

        // Create new room with assigned theme
        var room = new Room();
        room.Theme = _roomThemes.TryGetValue(gridPos, out var theme) ? theme : RoomThemeType.Dungeon;
        room.RoomType = _grid[gridPos.X, gridPos.Y];

        // Determine which doors exist based on neighboring rooms
        room.HasTopDoor    = HasRoomAt(gridPos + Vector2I.Up);
        room.HasBottomDoor = HasRoomAt(gridPos + Vector2I.Down);
        room.HasLeftDoor   = HasRoomAt(gridPos + Vector2I.Left);
        room.HasRightDoor  = HasRoomAt(gridPos + Vector2I.Right);

        // Check door lock state (Shop room 4 requires a key to enter)
        CheckDoorLockState(room, gridPos, "top", gridPos + Vector2I.Up);
        CheckDoorLockState(room, gridPos, "bottom", gridPos + Vector2I.Down);
        CheckDoorLockState(room, gridPos, "left", gridPos + Vector2I.Left);
        CheckDoorLockState(room, gridPos, "right", gridPos + Vector2I.Right);

        AddChild(room);
        _currentRoom = room;

        // Connect door signals
        room.DoorEntered += OnDoorEntered;

        // Move player to correct spawn position
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (_player != null)
        {
            string oppositeDir = GetOppositeDirection(fromDirection);
            _player.GlobalPosition = room.GetPlayerSpawnPosition(oppositeDir);
        }

        _visitedRooms.Add(gridPos);

        // Check if room was previously cleared
        if (_clearedRooms.Contains(gridPos) || gridPos == new Vector2I(GridCenter, GridCenter))
        {
            room.SetCleared();
        }
        else
        {
            room.RoomCleared += () =>
            {
                _clearedRooms.Add(gridPos);
                // Award 1 Active Item charge pip upon clearing a room!
                if (_player is Player.PlayerController pc)
                {
                    pc.AddActiveItemCharge(1);
                }
            };
            int roomType = _grid[gridPos.X, gridPos.Y];
            SpawnRoomContent(room, roomType);
        }

        // If treasure room and item wasn't taken yet, spawn pedestal
        if (_grid[gridPos.X, gridPos.Y] == 3 && !_takenItemRooms.Contains(gridPos))
        {
            SpawnTreasurePedestal(room, gridPos);
        }

        // If shop room, spawn shop wares
        if (_grid[gridPos.X, gridPos.Y] == 4 && !_clearedRooms.Contains(gridPos))
        {
            SpawnShopWares(room, gridPos);
        }

        EmitSignal(SignalName.RoomEntered, gridPos);
        SaveManager.SaveCurrentRun();
    }

    private void SpawnTreasurePedestal(Room room, Vector2I gridPos)
    {
        var pedestalScene = GD.Load<PackedScene>("res://scenes/items/ItemPedestal.tscn");
        if (pedestalScene != null)
        {
            var pedestal = pedestalScene.Instantiate<Items.ItemPedestal>();
            pedestal.Position = new Vector2(Room.RoomWidth / 2.0f, Room.RoomHeight / 2.0f);
            room.AddChild(pedestal);

            // Record when collected so it doesn't respawn on re-entry
            _takenItemRooms.Add(gridPos);
        }
    }

    private void SpawnShopWares(Room room, Vector2I gridPos)
    {
        room.SetCleared();
        float centerX = Room.RoomWidth / 2.0f;
        float centerY = Room.RoomHeight / 2.0f;

        // 1. Red Heart (3 coins)
        var heart = new Items.ConsumablePickup();
        room.AddChild(heart);
        heart.Initialize(new Vector2(centerX - 180, centerY), Items.ConsumableType.Heart, price: 3, value: 1);

        // 2. Bomb Pickup (5 coins)
        var bomb = new Items.ConsumablePickup();
        room.AddChild(bomb);
        bomb.Initialize(new Vector2(centerX - 60, centerY), Items.ConsumableType.Bomb, price: 5, value: 1);

        // 3. Key Pickup (5 coins)
        var key = new Items.ConsumablePickup();
        room.AddChild(key);
        key.Initialize(new Vector2(centerX + 60, centerY), Items.ConsumableType.Key, price: 5, value: 1);

        // 4. Shop Item Pedestal (15 coins)
        var pedestalScene = GD.Load<PackedScene>("res://scenes/items/ItemPedestal.tscn");
        if (pedestalScene != null)
        {
            var pedestal = pedestalScene.Instantiate<Items.ItemPedestal>();
            pedestal.Price = 15;
            pedestal.Position = new Vector2(centerX + 180, centerY);
            room.AddChild(pedestal);
        }

        // Shopkeeper decorative carpet & NPC visual
        var carpet = new ColorRect
        {
            Color = new Color(0.45f, 0.15f, 0.20f, 0.75f),
            Position = new Vector2(centerX - 240, centerY - 60),
            Size = new Vector2(480, 120),
            ZIndex = -1
        };
        room.AddChild(carpet);

        // Shopkeeper sign label
        var sign = new Label
        {
            Text = "— VEIL BAZAAR —\nKeys, Bombs & Curiosities",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(centerX - 150, centerY - 110),
            Size = new Vector2(300, 40)
        };
        sign.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.40f));
        sign.AddThemeFontSizeOverride("font_size", 16);
        room.AddChild(sign);
    }

    private void CheckDoorLockState(Room room, Vector2I currentPos, string direction, Vector2I neighborPos)
    {
        if (HasRoomAt(neighborPos))
        {
            // If the neighbor room is a Shop (type 4) and has not been unlocked yet, lock the door
            if (_grid[neighborPos.X, neighborPos.Y] == 4 && !_unlockedRooms.Contains(neighborPos))
            {
                room.DoorLocked[direction] = true;
            }
        }
    }

    public void UnlockDoorConnection(Room room, string direction)
    {
        Vector2I offset = direction switch
        {
            "top" => Vector2I.Up,
            "bottom" => Vector2I.Down,
            "left" => Vector2I.Left,
            "right" => Vector2I.Right,
            _ => Vector2I.Zero
        };

        var targetPos = _currentRoomPos + offset;
        _unlockedRooms.Add(targetPos);
    }

    private void SpawnRoomContent(Room room, int roomType)
    {
        switch (roomType)
        {
            case 1: // Normal room
                var enemyScenes = new PackedScene[] {
                    _slimeEnemyScene,
                    _flyEnemyScene,
                    _chaserEnemyScene,
                    _shooterEnemyScene
                };
                int enemyCount = (int)GD.RandRange(2, 5);
                room.SpawnEnemies(enemyScenes, enemyCount);

                // Varied Isaac-style room obstacle layout
                var layouts = (Environments.RoomLayoutType[])System.Enum.GetValues(typeof(Environments.RoomLayoutType));
                var randomLayout = layouts[GD.RandRange(0, layouts.Length - 1)];
                room.SetObstacleLayout(randomLayout);
                break;

            case 2: // Boss room
                SpawnBoss(room, _currentRoomPos);
                break;

            case 3: // Treasure room
                room.SetCleared();
                break;

            case 4: // Shop room
                room.SetCleared();
                break;

            default:
                room.SetCleared();
                break;
        }
    }

    private void SpawnBoss(Room room, Vector2I gridPos)
    {
        if (_bossScene == null) return;

        var boss = _bossScene.Instantiate<Bosses.BossEnemy>();
        boss.Position = new Vector2(Room.RoomWidth / 2.0f, Room.RoomHeight / 2.0f);
        room.LockRoom();

        boss.BossHealthUpdated += (cur, max) =>
        {
            EmitSignal(SignalName.BossHealthChanged, Bosses.BossEnemy.BossName, cur, max);
        };

        boss.EnemyDied += () =>
        {
            _clearedRooms.Add(gridPos);
            EmitSignal(SignalName.BossDefeated);
            room.SetCleared();
            GameManager.Instance?.OnRoomCleared();

            // Spawn Trapdoor to next floor
            if (_trapdoorScene != null)
            {
                var trapdoor = _trapdoorScene.Instantiate<Node2D>();
                trapdoor.Position = new Vector2(Room.RoomWidth / 2.0f, Room.RoomHeight / 2.0f);
                room.AddChild(trapdoor);
            }

            // Spawn Boss Reward Item Pedestal
            SpawnTreasurePedestal(room, gridPos);
        };

        room.AddChild(boss);
    }

    private void OnDoorEntered(string direction)
    {
        Vector2I offset = direction switch
        {
            "top"    => Vector2I.Up,
            "bottom" => Vector2I.Down,
            "left"   => Vector2I.Left,
            "right"  => Vector2I.Right,
            _        => Vector2I.Zero
        };

        var newPos = _currentRoomPos + offset;
        if (HasRoomAt(newPos))
        {
            EnterRoom(newPos, direction);
        }
    }

    private bool HasRoomAt(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < GridSize &&
               pos.Y >= 0 && pos.Y < GridSize &&
               _grid[pos.X, pos.Y] > 0;
    }

    private string GetOppositeDirection(string direction)
    {
        return direction switch
        {
            "top"    => "bottom",
            "bottom" => "top",
            "left"   => "right",
            "right"  => "left",
            _        => "center"
        };
    }
}
