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

    // ── Enemy scenes ──
    private PackedScene _flyEnemyScene;
    private PackedScene _chaserEnemyScene;
    private PackedScene _shooterEnemyScene;

    // ── Player reference ──
    private Node2D _player;

    public override void _Ready()
    {
        _flyEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/FlyEnemy.tscn");
        _chaserEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/ChaserEnemy.tscn");
        _shooterEnemyScene = GD.Load<PackedScene>("res://scenes/enemies/ShooterEnemy.tscn");

        GenerateFloor();
        EnterRoom(_currentRoomPos, "center");
    }

    // ══════════════════════════════════════════════
    //  FLOOR GENERATION
    // ══════════════════════════════════════════════

    private void GenerateFloor()
    {
        _grid = new int[GridSize, GridSize];
        var roomPositions = new List<Vector2I>();

        // Start room at center
        var start = new Vector2I(GridCenter, GridCenter);
        _grid[start.X, start.Y] = 1;
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

            // Don't add if it would create a 2x2 block (keeps layout more interesting)
            if (CountAdjacentRooms(pos, roomPositions) <= 1)
            {
                _grid[pos.X, pos.Y] = 1;
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
        }

        // Place treasure room at another dead end
        foreach (var pos in roomPositions)
        {
            int neighbors = CountAdjacentRooms(pos, roomPositions);
            if (neighbors == 1 && pos != start && _grid[pos.X, pos.Y] == 1)
            {
                _grid[pos.X, pos.Y] = 3; // Treasure room
                break;
            }
        }

        GD.Print($"Floor generated with {roomPositions.Count} rooms");
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

        // Create new room
        var room = new Room();

        // Determine which doors exist based on neighboring rooms
        room.HasTopDoor    = HasRoomAt(gridPos + Vector2I.Up);
        room.HasBottomDoor = HasRoomAt(gridPos + Vector2I.Down);
        room.HasLeftDoor   = HasRoomAt(gridPos + Vector2I.Left);
        room.HasRightDoor  = HasRoomAt(gridPos + Vector2I.Right);

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
            room.RoomCleared += () => _clearedRooms.Add(gridPos);
            int roomType = _grid[gridPos.X, gridPos.Y];
            SpawnRoomContent(room, roomType);
        }

        // If treasure room and item wasn't taken yet, spawn pedestal
        if (_grid[gridPos.X, gridPos.Y] == 3 && !_takenItemRooms.Contains(gridPos))
        {
            SpawnTreasurePedestal(room, gridPos);
        }
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

    private void SpawnRoomContent(Room room, int roomType)
    {
        switch (roomType)
        {
            case 1: // Normal room
                var enemyScenes = new PackedScene[] { _flyEnemyScene, _chaserEnemyScene, _shooterEnemyScene };
                int enemyCount = (int)GD.RandRange(2, 5);
                room.SpawnEnemies(enemyScenes, enemyCount);
                break;

            case 2: // Boss room
                // Handled in Boss milestone
                room.SetCleared();
                break;

            case 3: // Treasure room
                room.SetCleared();
                break;

            default:
                room.SetCleared();
                break;
        }
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
