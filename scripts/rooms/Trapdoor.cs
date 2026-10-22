using Godot;

namespace Veilbound.Systems;

/// <summary>
/// A trapdoor leading to the next floor. Appears in the Boss Room
/// after the floor boss is defeated.
/// </summary>
public partial class Trapdoor : Area2D
{
    private bool _activated = false;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_activated) return;

        if (body is Player.PlayerController player)
        {
            _activated = true;

            // Player stepping into trapdoor animation
            var tween = CreateTween();
            tween.Parallel().TweenProperty(player, "scale", Vector2.Zero, 0.4);
            tween.Parallel().TweenProperty(player, "modulate:a", 0.0f, 0.4);

            tween.Chain().TweenCallback(Callable.From(() =>
            {
                GameManager.Instance?.NextFloor();
            }));
        }
    }
}
