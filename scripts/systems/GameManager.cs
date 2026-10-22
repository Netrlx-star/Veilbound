using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Global game manager autoload. Handles game state, scene transitions,
/// and run-level data persistence.
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    // ── Current run stats ──
    public int CurrentFloor { get; set; } = 1;
    public int RoomsCleared { get; set; } = 0;
    public bool IsRunActive { get; private set; } = false;

    // ── Signals ──
    [Signal] public delegate void RunStartedEventHandler();
    [Signal] public delegate void RunEndedEventHandler(bool victory);
    [Signal] public delegate void FloorChangedEventHandler(int newFloor);
    [Signal] public delegate void RoomClearedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // Keep running even when paused
    }

    /// <summary>
    /// Start a new roguelite run. Resets all per-run state and loads the game scene.
    /// </summary>
    public void StartNewRun()
    {
        CurrentFloor = 1;
        RoomsCleared = 0;
        IsRunActive = true;

        EmitSignal(SignalName.RunStarted);
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }

    /// <summary>
    /// End the current run (death or victory).
    /// </summary>
    public void EndRun(bool victory = false)
    {
        IsRunActive = false;
        EmitSignal(SignalName.RunEnded, victory);
    }

    /// <summary>
    /// Return to the main menu.
    /// </summary>
    public void ReturnToMenu()
    {
        IsRunActive = false;
        GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
    }

    /// <summary>
    /// Advance to the next floor.
    /// </summary>
    public void NextFloor()
    {
        CurrentFloor++;
        EmitSignal(SignalName.FloorChanged, CurrentFloor);
    }

    /// <summary>
    /// Track a room being cleared.
    /// </summary>
    public void OnRoomCleared()
    {
        RoomsCleared++;
        EmitSignal(SignalName.RoomCleared);
    }
}
