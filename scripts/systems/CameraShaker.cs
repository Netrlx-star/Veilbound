using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Camera controller with rotational trauma shake for combat juice.
/// Detonations, heavy hits, and player damage trigger dynamic micro-trauma.
/// </summary>
public partial class CameraShaker : Camera2D
{
    public static CameraShaker Instance { get; private set; }

    private float _trauma = 0.0f;
    private const float TraumaDecay = 1.8f;
    private const float MaxOffset = 14.0f;
    private const float MaxRoll = 0.04f; // Radians

    private double _time = 0.0;

    public override void _Ready()
    {
        Instance = this;
    }

    public static void Shake(float amount)
    {
        if (Instance != null)
        {
            Instance.AddTrauma(amount);
        }
    }

    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp(_trauma + amount, 0.0f, 1.0f);
    }

    public override void _Process(double delta)
    {
        if (_trauma > 0)
        {
            _time += delta * 35.0;
            _trauma = Mathf.Max(_trauma - (float)delta * TraumaDecay, 0.0f);

            // Polynomial trauma curve (trauma^2) for smooth visceral shake
            float shake = _trauma * _trauma;
            Offset = new Vector2(
                MaxOffset * shake * (float)GD.RandRange(-1.0, 1.0),
                MaxOffset * shake * (float)GD.RandRange(-1.0, 1.0)
            );
            Rotation = MaxRoll * shake * (float)GD.RandRange(-1.0, 1.0);
        }
        else
        {
            Offset = Vector2.Zero;
            Rotation = 0.0f;
        }
    }
}
