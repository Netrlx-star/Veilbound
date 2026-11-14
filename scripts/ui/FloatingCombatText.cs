using Godot;

namespace Veilbound.UI;

/// <summary>
/// High-performance floating combat text (FCT) that pops up on damage hits,
/// crits, and status effect ticks, drifting upward with bouncy arc physics.
/// </summary>
public partial class FloatingCombatText : Node2D
{
    private Label _label;
    private Vector2 _velocity;
    private float _lifetime = 0.55f;
    private float _timer = 0.0f;

    public static void Spawn(Node parent, Vector2 worldPos, float amount, Color color, bool isCritical = false, string customText = "")
    {
        if (parent == null) return;

        var fct = new FloatingCombatText();
        parent.AddChild(fct);
        fct.Initialize(worldPos, amount, color, isCritical, customText);
    }

    public void Initialize(Vector2 worldPos, float amount, Color color, bool isCritical, string customText)
    {
        GlobalPosition = worldPos + new Vector2((float)GD.RandRange(-8.0, 8.0), (float)GD.RandRange(-6.0, 4.0));
        ZIndex = 25; // Render above room obstacles and enemies

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        string text = !string.IsNullOrEmpty(customText) 
            ? customText 
            : (isCritical ? $"{amount:F1}!" : $"{amount:F1}");

        _label.Text = text;
        _label.AddThemeColorOverride("font_color", color);
        _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        _label.AddThemeConstantOverride("shadow_offset_x", 1);
        _label.AddThemeConstantOverride("shadow_offset_y", 1);
        _label.AddThemeFontSizeOverride("font_size", isCritical ? 14 : 11);

        AddChild(_label);

        // Center label offset
        _label.Position = new Vector2(-40, -12);
        _label.Size = new Vector2(80, 24);

        // Initial pop-scale tween
        Scale = Vector2.Zero;
        var popTween = CreateTween();
        popTween.TweenProperty(this, "scale", isCritical ? new Vector2(1.35f, 1.35f) : Vector2.One, 0.12)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        // Upward arc trajectory
        float randomX = (float)GD.RandRange(-35.0, 35.0);
        float upwardY = (float)GD.RandRange(-80.0, -110.0);
        _velocity = new Vector2(randomX, upwardY);
    }

    public override void _Process(double delta)
    {
        _timer += (float)delta;
        GlobalPosition += _velocity * (float)delta;
        _velocity.Y += 120.0f * (float)delta; // Gravity deceleration

        if (_timer >= _lifetime * 0.5f)
        {
            float fadeProgress = (_timer - _lifetime * 0.5f) / (_lifetime * 0.5f);
            Modulate = new Color(1, 1, 1, Mathf.Clamp(1.0f - fadeProgress, 0.0f, 1.0f));
        }

        if (_timer >= _lifetime)
        {
            QueueFree();
        }
    }
}
