using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// Ranged shooter enemy. Maintains distance from the player and fires
/// blood tears periodically.
/// Inspired by Isaac's Clotties / Shooters.
/// </summary>
public partial class ShooterEnemy : BaseEnemy
{
    [Export] public float PreferredDistance { get; set; } = 280.0f;
    [Export] public float FireInterval { get; set; } = 1.8f;

    private double _fireTimer = 0.0;
    private PackedScene _projectileScene;
    private Node2D _player;

    protected override void EnemyReady()
    {
        MaxHealth = 12.0f;
        CurrentHealth = MaxHealth;
        MoveSpeed = 50.0f;
        ContactDamage = 1.0f;

        _fireTimer = FireInterval * 0.7f; // slight offset before first shot
        _projectileScene = GD.Load<PackedScene>("res://scenes/enemies/EnemyProjectile.tscn");
    }

    protected override void EnemyPhysicsProcess(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (_player == null)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();
        Vector2 dirToPlayer = toPlayer.Normalized();

        // Spacing AI: Back up if too close, advance if too far, strafe if in ideal zone
        if (distance < PreferredDistance - 40.0f)
        {
            // Back away from player
            Velocity = -dirToPlayer * MoveSpeed;
        }
        else if (distance > PreferredDistance + 60.0f)
        {
            // Move closer
            Velocity = dirToPlayer * MoveSpeed;
        }
        else
        {
            // Strafe perpendicular to player
            Vector2 perpendicular = new Vector2(-dirToPlayer.Y, dirToPlayer.X);
            Velocity = perpendicular * (MoveSpeed * 0.6f);
        }

        // Shooting logic
        _fireTimer -= delta;
        if (_fireTimer <= 0.0)
        {
            ShootAtPlayer(dirToPlayer);
            _fireTimer = FireInterval + (float)GD.RandRange(-0.3, 0.3);
        }

        // Flip sprite
        if (Sprite != null && dirToPlayer.X != 0)
        {
            Sprite.FlipH = dirToPlayer.X < 0;
        }
    }

    private void ShootAtPlayer(Vector2 direction)
    {
        if (_projectileScene == null) return;

        var proj = _projectileScene.Instantiate<EnemyProjectile>();
        proj.Direction = direction;
        proj.GlobalPosition = GlobalPosition;

        GetTree().CurrentScene.AddChild(proj);
    }
}
