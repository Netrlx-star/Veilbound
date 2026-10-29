using Godot;
using Veilbound.Systems;

namespace Veilbound.UI;

/// <summary>
/// Full-featured dark fantasy Settings and Keybindings menu.
/// Supports Master/SFX/Music sliders, Fullscreen/VSync toggles,
/// and interactive keyboard input remapping.
/// </summary>
public partial class SettingsMenu : Control
{
    private HSlider _masterSlider;
    private HSlider _sfxSlider;
    private HSlider _musicSlider;
    private CheckBox _fullscreenCheck;
    private CheckBox _vsyncCheck;
    private VBoxContainer _keybindContainer;
    private Button _backButton;

    private string _actionAwaitingKey = null;
    private Button _activeRemapButton = null;

    [Signal] public delegate void ClosedEventHandler();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _masterSlider = GetNode<HSlider>("%MasterSlider");
        _sfxSlider = GetNode<HSlider>("%SfxSlider");
        _musicSlider = GetNode<HSlider>("%MusicSlider");
        _fullscreenCheck = GetNode<CheckBox>("%FullscreenCheck");
        _vsyncCheck = GetNode<CheckBox>("%VsyncCheck");
        _keybindContainer = GetNode<VBoxContainer>("%KeybindContainer");
        _backButton = GetNode<Button>("%BackButton");

        var sm = SettingsManager.Instance;
        if (sm != null)
        {
            _masterSlider.Value = sm.GetBusVolume("Master");
            _sfxSlider.Value = sm.GetBusVolume("SFX");
            _musicSlider.Value = sm.GetBusVolume("Music");
            _fullscreenCheck.ButtonPressed = sm.IsFullscreen();
            _vsyncCheck.ButtonPressed = sm.IsVSync();
        }

        _masterSlider.ValueChanged += v => sm?.SetBusVolume("Master", (float)v);
        _sfxSlider.ValueChanged += v => sm?.SetBusVolume("SFX", (float)v);
        _musicSlider.ValueChanged += v => sm?.SetBusVolume("Music", (float)v);
        _fullscreenCheck.Toggled += toggled => sm?.SetFullscreen(toggled);
        _vsyncCheck.Toggled += toggled => sm?.SetVSync(toggled);
        _backButton.Pressed += OnBackPressed;

        BuildKeybindUI();
        Visible = false;
    }

    public void Open()
    {
        Visible = true;
        _backButton?.GrabFocus();

        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.2);
    }

    private void BuildKeybindUI()
    {
        foreach (Node child in _keybindContainer.GetChildren())
            child.QueueFree();

        var sm = SettingsManager.Instance;

        foreach (var action in SettingsManager.ConfigurableActions)
        {
            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 34);

            var label = new Label
            {
                Text = FormatActionName(action),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.90f));

            var remapBtn = new Button
            {
                Text = sm?.GetActionKeyName(action) ?? "Unbound",
                CustomMinimumSize = new Vector2(130, 28)
            };

            string capturedAction = action;
            remapBtn.Pressed += () => StartListeningForKey(capturedAction, remapBtn);

            row.AddChild(label);
            row.AddChild(remapBtn);
            _keybindContainer.AddChild(row);
        }
    }

    private void StartListeningForKey(string action, Button button)
    {
        if (_actionAwaitingKey != null && _activeRemapButton != null)
        {
            _activeRemapButton.Text = SettingsManager.Instance?.GetActionKeyName(_actionAwaitingKey) ?? "";
        }

        _actionAwaitingKey = action;
        _activeRemapButton = button;
        button.Text = "Press key...";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_actionAwaitingKey == null) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            GetViewport().SetInputAsHandled();

            Key key = keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;

            if (key == Key.Escape)
            {
                _activeRemapButton.Text = SettingsManager.Instance?.GetActionKeyName(_actionAwaitingKey) ?? "";
            }
            else
            {
                SettingsManager.Instance?.ApplyKeybind(_actionAwaitingKey, key);
                _activeRemapButton.Text = OS.GetKeycodeString(key);
            }

            _actionAwaitingKey = null;
            _activeRemapButton = null;
        }
    }

    private static string FormatActionName(string action) => action switch
    {
        "move_up" => "Move Up",
        "move_down" => "Move Down",
        "move_left" => "Move Left",
        "move_right" => "Move Right",
        "shoot_up" => "Shoot Up (Arrow)",
        "shoot_down" => "Shoot Down (Arrow)",
        "shoot_left" => "Shoot Left (Arrow)",
        "shoot_right" => "Shoot Right (Arrow)",
        "place_bomb" => "Place Bomb",
        "toggle_stats" => "Toggle Stats & Guide",
        _ => action.Replace("_", " ")
    };

    private void OnBackPressed()
    {
        SettingsManager.Instance?.SaveSettings();
        Visible = false;
        EmitSignal(SignalName.Closed);
    }
}
