using Godot;

namespace Veilbound.Player;

/// <summary>
/// Main player controller. Handles movement (WASD) and shooting (Arrow keys).
/// Uses CharacterBody2D for physics-based movement with wall collisions.
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    // ── Stats ──
    public PlayerStats Stats { get; private set; } = new();

    // ── Shooting ──
    private double _shootCooldown = 0.0;
    private PackedScene _projectileScene;

    // ── Invincibility after being hit ──
    private double _invincibilityTimer = 0.0;
    private const double InvincibilityDuration = 1.0;
    private bool IsInvincible => _invincibilityTimer > 0;

    // ── References ──
    private Sprite2D _sprite;
    private AnimationPlayer _animationPlayer;
    private CollisionShape2D _collisionShape;
    private Node2D _projectileSpawnPoint;

    // ── Signals ──
    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void ConsumablesChangedEventHandler(int coins, int bombs, int keys);
    [Signal] public delegate void PlayerDiedEventHandler();

    public override void _Ready()
    {
        _projectileScene = GD.Load<PackedScene>("res://scenes/player/Projectile.tscn");

        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        // Try to get optional nodes
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _projectileSpawnPoint = GetNodeOrNull<Node2D>("ProjectileSpawnPoint");

        Stats.Reset();
        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
        EmitSignal(SignalName.StatsChanged);
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
    }

    public void NotifyStatsChanged()
    {
        EmitSignal(SignalName.StatsChanged);
        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
    }

    public void AddCoins(int amount)
    {
        Stats.Coins += amount;
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
    }

    public void AddBombs(int amount)
    {
        Stats.Bombs += amount;
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
    }

    public void AddKeys(int amount)
    {
        Stats.Keys += amount;
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement(delta);
        HandleShooting(delta);
        HandleInvincibility(delta);
    }

    // ══════════════════════════════════════════════
    //  MOVEMENT
    // ══════════════════════════════════════════════

    private void HandleMovement(double delta)
    {
        var inputDir = Vector2.Zero;

        if (Input.IsActionPressed("move_up"))    inputDir.Y -= 1;
        if (Input.IsActionPressed("move_down"))  inputDir.Y += 1;
        if (Input.IsActionPressed("move_left"))  inputDir.X -= 1;
        if (Input.IsActionPressed("move_right")) inputDir.X += 1;

        inputDir = inputDir.Normalized();
        Velocity = inputDir * Stats.MoveSpeed;

        MoveAndSlide();

        // Flip sprite based on movement direction
        if (inputDir.X != 0 && _sprite != null)
        {
            _sprite.FlipH = inputDir.X < 0;
        }
    }

    // ══════════════════════════════════════════════
    //  SHOOTING
    // ══════════════════════════════════════════════

    private void HandleShooting(double delta)
    {
        _shootCooldown -= delta;

        var shootDir = GetShootDirection();
        if (shootDir != Vector2.Zero && _shootCooldown <= 0)
        {
            Shoot(shootDir);
            _shootCooldown = Stats.FireRate;
        }
    }

    private Vector2 GetShootDirection()
    {
        // Arrow keys for shooting — only allow one direction at a time (last pressed wins)
        var dir = Vector2.Zero;

        if (Input.IsActionPressed("shoot_up"))    dir = Vector2.Up;
        if (Input.IsActionPressed("shoot_down"))  dir = Vector2.Down;
        if (Input.IsActionPressed("shoot_left"))  dir = Vector2.Left;
        if (Input.IsActionPressed("shoot_right")) dir = Vector2.Right;

        return dir;
    }

    private void Shoot(Vector2 direction)
    {
        if (_projectileScene == null) return;

        var projectile = _projectileScene.Instantiate<Projectile>();
        projectile.Direction = direction;
        projectile.Speed = Stats.ProjectileSpeed;
        projectile.Damage = Stats.Damage;
        projectile.MaxRange = Stats.ProjectileRange;

        // Spawn from the spawn point or player center
        var spawnPos = _projectileSpawnPoint?.GlobalPosition ?? GlobalPosition;
        projectile.GlobalPosition = spawnPos;

        // Add to the current scene (not as child of player, so it stays in world space)
        GetTree().CurrentScene.AddChild(projectile);
    }

    // ══════════════════════════════════════════════
    //  DAMAGE & HEALTH
    // ══════════════════════════════════════════════

    public void TakeDamage(int amount = 1)
    {
        if (IsInvincible) return;

        bool died = Stats.TakeDamage(amount);
        _invincibilityTimer = InvincibilityDuration;

        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);

        // Flash effect
        if (_sprite != null)
        {
            _sprite.Modulate = new Color(1, 0.3f, 0.3f);
        }

        if (died)
        {
            OnDeath();
        }
    }

    public void Heal(int amount = 1)
    {
        Stats.Heal(amount);
        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
    }

    private void HandleInvincibility(double delta)
    {
        if (!IsInvincible) return;

        _invincibilityTimer -= delta;

        // Blink effect during invincibility
        if (_sprite != null)
        {
            _sprite.Visible = ((int)(_invincibilityTimer * 10)) % 2 == 0;
        }

        if (_invincibilityTimer <= 0)
        {
            _invincibilityTimer = 0;
            if (_sprite != null)
            {
                _sprite.Visible = true;
                _sprite.Modulate = Colors.White;
            }
        }
    }

    private void OnDeath()
    {
        // Disable player input and collision
        SetPhysicsProcess(false);
        _collisionShape.SetDeferred("disabled", true);

        // Flash and collapse animation
        if (_sprite != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate:a", 0.3f, 0.5);
            tween.Parallel().TweenProperty(_sprite, "scale", new Vector2(1.3f, 0.4f), 0.5);
        }

        // Slight dramatic pause before displaying the game over screen
        var timer = GetTree().CreateTimer(0.8);
        timer.Timeout += () =>
        {
            EmitSignal(SignalName.PlayerDied);
        };
    }
}
