using Godot;
using System;

namespace Veilbound.Systems;

public enum ShootingControlMode
{
    SingleButton = 0,   // Shoot with 1 button in walking direction
    FourWay = 1,        // Shoot with 4 directional buttons
    Hybrid = 2          // Both active simultaneously
}

/// <summary>
/// Global Settings and Keybindings manager singleton.
/// Handles audio volume (dB math), video modes (Fullscreen/VSync),
/// and dynamic keyboard input remapping persisted to user://settings.cfg.
/// </summary>
public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    [Signal] public delegate void ShootingModeChangedEventHandler(int mode);

    private const string ConfigPath = "user://settings.cfg";
    private readonly ConfigFile _config = new();

    public static readonly string[] ConfigurableActions = new[]
    {
        "move_up", "move_down", "move_left", "move_right",
        "shoot_facing",
        "shoot_up", "shoot_down", "shoot_left", "shoot_right",
        "place_bomb",
        "toggle_stats"
    };

    public ShootingControlMode ShootingMode { get; private set; } = ShootingControlMode.SingleButton;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        LoadSettings();
    }

    public void LoadSettings()
    {
        var err = _config.Load(ConfigPath);
        if (err != Error.Ok)
        {
            ApplyDefaults();
            SaveSettings();
            return;
        }

        // Gameplay
        ShootingMode = (ShootingControlMode)(long)_config.GetValue("Gameplay", "ShootingMode", (long)ShootingControlMode.SingleButton);

        // Audio
        SetBusVolume("Master", (float)_config.GetValue("Audio", "MasterVolume", 0.8f));
        SetBusVolume("SFX", (float)_config.GetValue("Audio", "SfxVolume", 0.8f));
        SetBusVolume("Music", (float)_config.GetValue("Audio", "MusicVolume", 0.7f));

        // Video
        bool fullscreen = (bool)_config.GetValue("Video", "Fullscreen", false);
        bool vsync = (bool)_config.GetValue("Video", "VSync", true);
        SetFullscreen(fullscreen);
        SetVSync(vsync);

        // Keybindings
        foreach (var action in ConfigurableActions)
        {
            if (_config.HasSectionKey("Keybindings", action))
            {
                var keycode = (Key)(long)_config.GetValue("Keybindings", action, (long)Key.None);
                if (keycode != Key.None)
                {
                    ApplyKeybind(action, keycode, persist: false);
                }
            }
        }

        // Ensure shoot_facing is bound if not present
        if (!InputMap.HasAction("shoot_facing") || GetActionKeycode("shoot_facing") == Key.None)
        {
            ApplyKeybind("shoot_facing", Key.Space, persist: false);
        }
    }

    public void SetShootingMode(ShootingControlMode mode)
    {
        ShootingMode = mode;
        _config.SetValue("Gameplay", "ShootingMode", (int)mode);
        SaveSettings();
        EmitSignal(SignalName.ShootingModeChanged, (int)mode);
    }

    public void SaveSettings()
    {
        _config.Save(ConfigPath);
    }

    private void ApplyDefaults()
    {
        ShootingMode = ShootingControlMode.SingleButton;
        _config.SetValue("Gameplay", "ShootingMode", (int)ShootingControlMode.SingleButton);

        SetBusVolume("Master", 0.8f);
        SetBusVolume("SFX", 0.8f);
        SetBusVolume("Music", 0.7f);
        SetFullscreen(false);
        SetVSync(true);

        if (!InputMap.HasAction("shoot_facing"))
        {
            ApplyKeybind("shoot_facing", Key.Space, persist: false);
        }
    }

    // ── Audio ──
    public float GetBusVolume(string busName)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex < 0) return 0.8f;
        if (AudioServer.IsBusMute(busIndex)) return 0.0f;

        float db = AudioServer.GetBusVolumeDb(busIndex);
        return Mathf.DbToLinear(db);
    }

    public void SetBusVolume(string busName, float linearVolume)
    {
        linearVolume = Mathf.Clamp(linearVolume, 0.0f, 1.0f);
        int busIndex = AudioServer.GetBusIndex(busName);

        if (busIndex >= 0)
        {
            if (linearVolume <= 0.001f)
            {
                AudioServer.SetBusMute(busIndex, true);
                AudioServer.SetBusVolumeDb(busIndex, -80.0f);
            }
            else
            {
                AudioServer.SetBusMute(busIndex, false);
                AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(linearVolume));
            }
        }

        _config.SetValue("Audio", $"{busName}Volume", linearVolume);
    }

    // ── Video ──
    public bool IsFullscreen()
    {
        return DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
    }

    public void SetFullscreen(bool enable)
    {
        var mode = enable ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        DisplayServer.WindowSetMode(mode);
        _config.SetValue("Video", "Fullscreen", enable);
    }

    public bool IsVSync()
    {
        return DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Enabled;
    }

    public void SetVSync(bool enable)
    {
        var mode = enable ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled;
        DisplayServer.WindowSetVsyncMode(mode);
        _config.SetValue("Video", "VSync", enable);
    }

    // ── Keybindings ──
    public Key GetActionKeycode(string action)
    {
        if (!InputMap.HasAction(action)) return Key.None;

        foreach (var ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventKey keyEvent)
            {
                return keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            }
        }
        return Key.None;
    }

    public string GetActionKeyName(string action)
    {
        Key key = GetActionKeycode(action);
        return key == Key.None ? "Unassigned" : OS.GetKeycodeString(key);
    }

    public void ApplyKeybind(string action, Key newKey, bool persist = true)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        // Erase old keyboard events
        var existing = InputMap.ActionGetEvents(action);
        foreach (var ev in existing)
        {
            if (ev is InputEventKey)
            {
                InputMap.ActionEraseEvent(action, ev);
            }
        }

        // Add new key event
        var newEvent = new InputEventKey
        {
            PhysicalKeycode = newKey,
            Keycode = newKey
        };
        InputMap.ActionAddEvent(action, newEvent);

        if (persist)
        {
            _config.SetValue("Keybindings", action, (long)newKey);
            SaveSettings();
        }
    }
}
