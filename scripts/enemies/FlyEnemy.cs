using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// A basic fly enemy that moves randomly and occasionally chases the player.
/// Inspired by Isaac's Fly enemies — simple, erratic movement.
/// </summary>
public partial class FlyEnemy : BaseEnemy
{
    private enum FlyState { Idle, Wandering, Chasing }

    [Export] public float ChaseRange { get; set; } = 250.0f;
    [Export] public float WanderChangeTime { get; set; } = 1.5f;

    private FlyState _state = FlyState.Wandering;
    private Vector2 _wanderDirection = Vector2.Zero;
    private double _wanderTimer = 0.0;
    private Node2D _player;

    protected override void EnemyReady()
    {
        MaxHealth = 8.0f;
        CurrentHealth = MaxHealth;
        MoveSpeed = 90.0f;
        ContactDamage = 1.0f;

        // Randomize initial wander direction
        _wanderDirection = new Vector2(
            (float)GD.RandRange(-1.0, 1.0),
            (float)GD.RandRange(-1.0, 1.0)
        ).Normalized();
    }

    protected override void EnemyPhysicsProcess(double delta)
    {
        // Find player if not cached
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (_player == null)
        {
            Wander(delta);
            return;
        }

        float distToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (distToPlayer < ChaseRange)
        {
            _state = FlyState.Chasing;
            ChasePlayer();
        }
        else
        {
            _state = FlyState.Wandering;
            Wander(delta);
        }

        // Flip sprite toward movement
        if (Sprite != null && Velocity.X != 0)
        {
            Sprite.FlipH = Velocity.X < 0;
        }
    }

    private void Wander(double delta)
    {
        _wanderTimer -= delta;

        if (_wanderTimer <= 0)
        {
            _wanderDirection = new Vector2(
                (float)GD.RandRange(-1.0, 1.0),
                (float)GD.RandRange(-1.0, 1.0)
            ).Normalized();
            _wanderTimer = WanderChangeTime + GD.RandRange(-0.5, 0.5);
        }

        Velocity = _wanderDirection * MoveSpeed * 0.5f;
    }

    private void ChasePlayer()
    {
        var dirToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();

        // Add slight randomness to make movement less perfectly linear
        var noise = new Vector2(
            (float)GD.RandRange(-0.2, 0.2),
            (float)GD.RandRange(-0.2, 0.2)
        );

        Velocity = (dirToPlayer + noise).Normalized() * MoveSpeed;
    }
}
