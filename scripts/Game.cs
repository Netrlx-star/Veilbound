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

        if (Systems.GameManager.Instance != null)
        {
            Systems.GameManager.Instance.RoomCleared += OnRoomCleared;
            Systems.GameManager.Instance.FloorChanged += OnFloorChanged;
        }

        // Initial HUD update
        OnPlayerHealthChanged(_player.Stats.CurrentHealth, _player.Stats.MaxHealth);
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        _hud?.UpdateHearts(currentHealth, maxHealth);
    }

    private void OnPlayerDied()
    {
        GD.Print("Player died! Run over.");
        // GameManager handles the transition back to menu
    }

    private void OnRoomCleared()
    {
        int roomsCleared = Systems.GameManager.Instance?.RoomsCleared ?? 0;
        _hud?.UpdateRoomsDisplay(roomsCleared);
    }

    private void OnFloorChanged(int newFloor)
    {
        _hud?.UpdateFloorDisplay(newFloor);
    }

    public override void _Input(InputEvent @event)
    {
        // Escape to pause/return to menu
        if (@event.IsActionPressed("ui_cancel"))
        {
            Systems.GameManager.Instance?.ReturnToMenu();
        }
    }
}
