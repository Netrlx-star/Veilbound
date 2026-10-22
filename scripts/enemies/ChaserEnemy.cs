using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// A chaser enemy that walks directly toward the player.
/// Inspired by Isaac's Gapers — slow, persistent, contact damage.
/// </summary>
public partial class ChaserEnemy : BaseEnemy
{
    [Export] public float ActivationRange { get; set; } = 400.0f;

    private Node2D _player;
    private bool _activated = false;

    protected override void EnemyReady()
    {
        MaxHealth = 15.0f;
        CurrentHealth = MaxHealth;
        MoveSpeed = 60.0f;
        ContactDamage = 1.0f;
    }

    protected override void EnemyPhysicsProcess(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (_player == null)
        {
            Velocity = Vector2.Zero;
            return;
        }

        float distToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (!_activated && distToPlayer < ActivationRange)
        {
            _activated = true;
        }

        if (_activated)
        {
            var dirToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
            Velocity = dirToPlayer * MoveSpeed;

            // Flip sprite
            if (Sprite != null && Velocity.X != 0)
            {
                Sprite.FlipH = Velocity.X < 0;
            }
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }
}
