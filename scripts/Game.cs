using Godot;

namespace Veilbound;

/// <summary>
/// Main game scene. This is the root of a roguelite run.
/// Sets up the room manager, player, camera, and HUD, then
/// connects them together.
/// </summary>
public partial class Game : Node2D
{
    private Player.PlayerController _player;
    private Systems.RoomManager _roomManager;
    private UI.HUD _hud;
    private Camera2D _camera;

    public override void _Ready()
    {
        // Create player
        var playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        _player = playerScene.Instantiate<Player.PlayerController>();
        _player.GlobalPosition = new Vector2(640, 360); // Center of viewport
        AddChild(_player);

        // Create camera following player
        _camera = new Camera2D();
        _camera.Enabled = true;
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 12.0f;
        // Lock camera to room (don't scroll beyond room bounds)
        _camera.LimitLeft = 0;
        _camera.LimitTop = 0;
        _camera.LimitRight = (int)Systems.Room.RoomWidth;
        _camera.LimitBottom = (int)Systems.Room.RoomHeight;
        _player.AddChild(_camera);

        // Create room manager
        _roomManager = new Systems.RoomManager();
        AddChild(_roomManager);

        // Create HUD
        var hudScene = GD.Load<PackedScene>("res://scenes/ui/HUD.tscn");
        _hud = hudScene.Instantiate<UI.HUD>();
        AddChild(_hud);

        // Connect signals
        _player.HealthChanged += OnPlayerHealthChanged;
        _player.PlayerDied += OnPlayerDied;
        _player.StatsChanged += OnPlayerStatsChanged;
        _player.ConsumablesChanged += OnConsumablesChanged;
        HookRoomManagerSignals();

        if (Systems.GameManager.Instance != null)
        {
            Systems.GameManager.Instance.RoomCleared += OnRoomCleared;
            Systems.GameManager.Instance.FloorChanged += OnFloorChanged;
        }

        // Initial HUD & Minimap update
        OnPlayerHealthChanged(_player.Stats.CurrentHealth, _player.Stats.MaxHealth);
        OnPlayerStatsChanged();
        OnConsumablesChanged(_player.Stats.Coins, _player.Stats.Bombs, _player.Stats.Keys);
        OnRoomEntered(_roomManager.CurrentRoomPos);
    }

    private void OnPlayerStatsChanged()
    {
        if (_player == null || _hud == null) return;
        _hud.UpdateStats(
            _player.Stats.Damage,
            _player.Stats.FireRate,
            _player.Stats.MoveSpeed,
            _player.Stats.ProjectileRange
        );
    }

    private void OnConsumablesChanged(int coins, int bombs, int keys)
    {
        _hud?.UpdateConsumables(coins, bombs, keys);
    }

    private void HookRoomManagerSignals()
    {
        if (_roomManager == null) return;
        _roomManager.RoomEntered += OnRoomEntered;
        _roomManager.BossHealthChanged += (name, cur, max) => _hud?.ShowBossBar(name, cur, max);
        _roomManager.BossDefeated += () => _hud?.HideBossBar();
    }

    private void OnRoomEntered(Vector2I currentPos)
    {
        _hud?.UpdateMinimap(_roomManager.Grid, _roomManager.VisitedRooms, currentPos);

        // Hide boss bar if entering a non-boss room
        if (_roomManager.Grid[currentPos.X, currentPos.Y] != 2)
        {
            _hud?.HideBossBar();
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        _hud?.UpdateHearts(currentHealth, maxHealth);
    }

    private void OnPlayerDied()
    {
        GD.Print("Player died! Showing Game Over summary.");
        int floor = Systems.GameManager.Instance?.CurrentFloor ?? 1;
        int rooms = Systems.GameManager.Instance?.RoomsCleared ?? 0;
        int enemies = _player?.Stats?.EnemiesKilled ?? 0;
        int items = _player?.Stats?.ItemsCollected ?? 0;

        _hud?.ShowGameOver(floor, rooms, enemies, items);
    }

    private void OnRoomCleared()
    {
        int roomsCleared = Systems.GameManager.Instance?.RoomsCleared ?? 0;
        _hud?.UpdateRoomsDisplay(roomsCleared);
    }

    private void OnFloorChanged(int newFloor)
    {
        _hud?.UpdateFloorDisplay(newFloor);
        _hud?.HideBossBar();

        // Regenerate new dungeon floor
        _roomManager?.QueueFree();
        _roomManager = new Systems.RoomManager();
        AddChild(_roomManager);
        HookRoomManagerSignals();

        // Reset player visuals and position for new floor
        if (_player != null)
        {
            _player.GlobalPosition = new Vector2(640, 360);
            _player.Scale = Vector2.One;
            _player.Modulate = Colors.White;
        }

        OnRoomEntered(_roomManager.CurrentRoomPos);
    }

    public override void _Input(InputEvent @event)
    {
        // Escape toggles pause overlay
        if (@event.IsActionPressed("ui_cancel"))
        {
            _hud?.TogglePauseMenu();
            GetViewport().SetInputAsHandled();
        }
    }
}
