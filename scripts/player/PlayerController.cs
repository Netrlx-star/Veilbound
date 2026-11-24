using Godot;

namespace Veilbound.Player;

/// <summary>
/// Main player controller. Handles movement (WASD) and shooting (Arrow keys).
/// Uses CharacterBody2D for physics-based movement with wall collisions.
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    public static PlayerController Instance { get; private set; }

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
    private Vector2 _lastMoveDirection = Vector2.Right;
    private float _walkTimer = 0.0f;

    // ── Signals ──
    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void RallyChangedEventHandler(int rallyHealth, float timer);
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void ConsumablesChangedEventHandler(int coins, int bombs, int keys);
    [Signal] public delegate void ActiveItemChangedEventHandler(string name, int currentCharges, int maxCharges);
    [Signal] public delegate void PlayerDiedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        _projectileScene = GD.Load<PackedScene>("res://scenes/player/Projectile.tscn");

        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        // Try to get optional nodes
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _projectileSpawnPoint = GetNodeOrNull<Node2D>("ProjectileSpawnPoint");

        Stats.Reset();

        // Apply character class specific archetype
        var charClass = Systems.GameManager.Instance?.SelectedClass ?? Systems.PlayerClass.Wanderer;
        if (charClass == Systems.PlayerClass.Bladesmith)
        {
            if (_sprite != null)
            {
                Systems.AssetGenerator.InitializeAssets();
                _sprite.Texture = Systems.AssetGenerator.KnightTexture;
            }

            Stats.MaxHealth = 8;       // 4 Red Hearts
            Stats.CurrentHealth = 8;
            Stats.Damage = 7.0f;       // High melee blade damage
            Stats.FireRate = 0.30f;     // Responsive slash interval
            Stats.MoveSpeed = 215.0f;
            Stats.KnockbackForce = 280.0f;
        }

        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
        EmitSignal(SignalName.StatsChanged);
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
        NotifyActiveItemChanged();
    }

    public void NotifyActiveItemChanged()
    {
        string itemName = Stats.HeldActiveItem != null ? Stats.HeldActiveItem.Name : "None";
        int max = Stats.HeldActiveItem != null ? Stats.HeldActiveItem.MaxCharges : 0;
        EmitSignal(SignalName.ActiveItemChanged, itemName, Stats.ActiveItemCharges, max);
    }

    public void NotifyStatsChanged()
    {
        EmitSignal(SignalName.StatsChanged);
        EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
        EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
        NotifyActiveItemChanged();
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

    public bool UseKey()
    {
        if (Stats.Keys > 0)
        {
            Stats.Keys--;
            EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);
            return true;
        }
        return false;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement(delta);
        HandleShooting(delta);
        HandleBombPlacement();
        HandleActiveItem();
        HandleInvincibility(delta);
        HandleRallyDecay(delta);
    }

    private void HandleBombPlacement()
    {
        if (Input.IsActionJustPressed("place_bomb") && Stats.Bombs > 0)
        {
            Stats.Bombs--;
            EmitSignal(SignalName.ConsumablesChanged, Stats.Coins, Stats.Bombs, Stats.Keys);

            var bombScene = GD.Load<PackedScene>("res://scenes/items/BombEntity.tscn");
            if (bombScene != null)
            {
                var bomb = bombScene.Instantiate<Node2D>();
                bomb.GlobalPosition = GlobalPosition;
                GetTree().CurrentScene.AddChild(bomb);
            }
        }
    }

    private void HandleActiveItem()
    {
        if (Input.IsActionJustPressed("use_item") || Input.IsKeyPressed(Key.Space))
        {
            UseActiveItem();
        }
    }

    public void AddActiveItemCharge(int amount = 1)
    {
        if (Stats.HeldActiveItem == null) return;
        Stats.ActiveItemCharges = Mathf.Min(Stats.HeldActiveItem.MaxCharges, Stats.ActiveItemCharges + amount);
        NotifyActiveItemChanged();
    }

    public void SetActiveItem(Items.ActiveItemData item)
    {
        Stats.HeldActiveItem = item;
        Stats.ActiveItemCharges = item != null ? item.MaxCharges : 0;
        NotifyActiveItemChanged();
    }

    private void UseActiveItem()
    {
        if (Stats.HeldActiveItem == null) return;

        // Check if fully charged
        if (Stats.ActiveItemCharges < Stats.HeldActiveItem.MaxCharges)
        {
            // Flash charge meter feedback
            Systems.CameraShaker.Shake(0.08f);
            return;
        }

        // Execute Active Item
        switch (Stats.HeldActiveItem.Type)
        {
            case Items.ActiveItemType.AstralBurst:
                // Shockwave clears all enemy bullets & knocks enemies back
                var projectiles = GetTree().GetNodesInGroup("enemy_projectiles");
                foreach (Node p in projectiles) p.QueueFree();

                foreach (Node enemyNode in GetTree().GetNodesInGroup("enemies"))
                {
                    if (enemyNode is Enemies.BaseEnemy e)
                    {
                        e.TakeDamage(10.0f);
                        Vector2 pushDir = (e.GlobalPosition - GlobalPosition).Normalized();
                        e.ApplyKnockback(pushDir, 350.0f);
                    }
                }
                Systems.CameraShaker.Shake(0.40f);
                break;

            case Items.ActiveItemType.HourglassOfTime:
                // Freeze/slow all enemies heavily
                foreach (Node enemyNode in GetTree().GetNodesInGroup("enemies"))
                {
                    if (enemyNode is Enemies.BaseEnemy e)
                    {
                        e.StatusManager?.ApplyEffect(Enemies.StatusEffectType.Slow, 4.0f, 0.5f);
                    }
                }
                Systems.CameraShaker.Shake(0.20f);
                break;

            case Items.ActiveItemType.CrucibleOfGreed:
                // Convert pots and rocks into coins & pickups
                foreach (Node obsNode in GetTree().GetNodesInGroup("obstacles"))
                {
                    if (obsNode is Environments.RoomObstacle obs)
                    {
                        obs.Explode();
                    }
                }
                Systems.CameraShaker.Shake(0.30f);
                break;

            case Items.ActiveItemType.BloodRitual:
                // Pay 1 health for a 360° fan of 10 piercing spectral tears
                TakeDamage(1);
                int count = 10;
                float step = Mathf.Tau / count;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * step;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    SpawnSingleProjectile(GlobalPosition + dir * 14.0f, dir);
                }
                Systems.CameraShaker.Shake(0.35f);
                break;
        }

        // Consume charges
        Stats.ActiveItemCharges = 0;
        NotifyActiveItemChanged();
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

        if (inputDir != Vector2.Zero)
        {
            _lastMoveDirection = inputDir.Normalized();
        }

        Velocity = inputDir.Normalized() * Stats.MoveSpeed;

        MoveAndSlide();

        // Flip sprite based on movement direction
        if (inputDir.X != 0 && _sprite != null)
        {
            _sprite.FlipH = inputDir.X < 0;
        }

        AnimateWalk(delta, inputDir);
    }

    private void AnimateWalk(double delta, Vector2 inputDir)
    {
        if (_sprite == null) return;

        if (inputDir != Vector2.Zero)
        {
            // Organic Isaac-style step cadence
            _walkTimer += (float)delta * (Stats.MoveSpeed / 16.0f);

            // Subtle body tilt swaying with steps
            _sprite.Rotation = Mathf.Sin(_walkTimer) * 0.12f;

            // Rhythmic vertical step bounce
            float bounceY = -Mathf.Abs(Mathf.Sin(_walkTimer)) * 3.5f;
            _sprite.Position = new Vector2(0, bounceY);

            // Subtle squash-and-stretch step compression
            float squashX = 1.0f + Mathf.Cos(_walkTimer * 2.0f) * 0.04f;
            float squashY = 1.0f - Mathf.Cos(_walkTimer * 2.0f) * 0.04f;
            _sprite.Scale = new Vector2(squashX, squashY);
        }
        else
        {
            // Smoothly ease back to neutral idle
            _walkTimer = 0.0f;
            _sprite.Rotation = Mathf.Lerp(_sprite.Rotation, 0.0f, (float)delta * 14.0f);
            _sprite.Position = _sprite.Position.Lerp(Vector2.Zero, (float)delta * 14.0f);
            _sprite.Scale = _sprite.Scale.Lerp(Vector2.One, (float)delta * 14.0f);
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
            var charClass = Systems.GameManager.Instance?.SelectedClass ?? Systems.PlayerClass.Wanderer;
            if (charClass == Systems.PlayerClass.Bladesmith)
            {
                SlashSword(shootDir);
            }
            else
            {
                Shoot(shootDir);
            }

            _shootCooldown = Stats.FireRate;
        }
    }

    private void SlashSword(Vector2 direction)
    {
        if (direction.X != 0 && _sprite != null)
        {
            _sprite.FlipH = direction.X < 0;
        }

        var slash = new SwordSlashArc
        {
            SlashDirection = direction,
            Damage = Stats.Damage,
            KnockbackForce = Stats.KnockbackForce,
            InflictedStatus = Stats.InflictedStatus,
            GlobalPosition = GlobalPosition + direction * 16.0f
        };
        GetTree().CurrentScene.AddChild(slash);

        // Subtle thrust impulse forward with the swing
        Velocity += direction * 70.0f;
    }

    private Vector2 GetShootDirection()
    {
        var sm = Systems.SettingsManager.Instance;
        var mode = sm != null ? sm.ShootingMode : Systems.ShootingControlMode.SingleButton;

        // 1. Single-button shooting in walking/facing direction
        if (mode == Systems.ShootingControlMode.SingleButton || mode == Systems.ShootingControlMode.Hybrid)
        {
            if (Input.IsActionPressed("shoot_facing"))
            {
                return _lastMoveDirection != Vector2.Zero ? _lastMoveDirection : Vector2.Right;
            }
        }

        // 2. 4-way independent shooting (Arrow keys or configured keys)
        if (mode == Systems.ShootingControlMode.FourWay || mode == Systems.ShootingControlMode.Hybrid)
        {
            var dir = Vector2.Zero;

            if (Input.IsActionPressed("shoot_up"))    dir = Vector2.Up;
            if (Input.IsActionPressed("shoot_down"))  dir = Vector2.Down;
            if (Input.IsActionPressed("shoot_left"))  dir = Vector2.Left;
            if (Input.IsActionPressed("shoot_right")) dir = Vector2.Right;

            if (dir != Vector2.Zero) return dir;
        }

        return Vector2.Zero;
    }

    private void Shoot(Vector2 direction)
    {
        if (_projectileScene == null) return;

        // Flip sprite to face projectile direction if horizontal
        if (direction.X != 0 && _sprite != null)
        {
            _sprite.FlipH = direction.X < 0;
        }

        var spawnPos = _projectileSpawnPoint?.GlobalPosition ?? GlobalPosition;
        int count = Mathf.Max(1, Stats.Multishot);

        if (count == 1)
        {
            SpawnSingleProjectile(spawnPos, direction);
        }
        else if (count == 2)
        {
            // Twin parallel shots offset along normal vector
            Vector2 normal = new Vector2(-direction.Y, direction.X).Normalized();
            SpawnSingleProjectile(spawnPos + normal * 8.0f, direction);
            SpawnSingleProjectile(spawnPos - normal * 8.0f, direction);
        }
        else
        {
            // Triple / multi-shot spread in fan cone
            float baseAngle = direction.Angle();
            float spreadStep = Mathf.DegToRad(14.0f);
            float startAngle = baseAngle - ((count - 1) * spreadStep) / 2.0f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * spreadStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnSingleProjectile(spawnPos + dir * 6.0f, dir);
            }
        }
    }

    private void SpawnSingleProjectile(Vector2 position, Vector2 direction)
    {
        var projectile = _projectileScene.Instantiate<Projectile>();
        projectile.Direction = direction;
        projectile.Speed = Stats.ProjectileSpeed;
        projectile.Damage = Stats.Damage;
        projectile.MaxRange = Stats.ProjectileRange;
        projectile.IsPiercing = Stats.HasPiercing;
        projectile.BouncesRemaining = Stats.HasBouncing ? 2 : 0;
        projectile.InflictedStatus = Stats.InflictedStatus;
        projectile.GlobalPosition = position;
        GetTree().CurrentScene.AddChild(projectile);
    }

    // ══════════════════════════════════════════════
    //  DAMAGE & HEALTH
    // ══════════════════════════════════════════════

    public void TakeDamage(int amount = 1)
    {
        if (IsInvincible) return;

        int lostHp = Mathf.Min(amount, Stats.CurrentHealth);
        bool died = Stats.TakeDamage(amount);
        _invincibilityTimer = InvincibilityDuration;

        // Visceral camera trauma on taking damage
        Systems.CameraShaker.Shake(0.45f);

        // Bloodbound Rally window (recoverable lost health)
        if (!died && lostHp > 0)
        {
            Stats.RallyHealth = Mathf.Min(Stats.MaxHealth - Stats.CurrentHealth, Stats.RallyHealth + lostHp);
            Stats.RallyTimer = 2.5f;
            EmitSignal(SignalName.RallyChanged, Stats.RallyHealth, Stats.RallyTimer);
        }

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

    public void OnSuccessfulAttackHit()
    {
        if (Stats.RallyHealth > 0)
        {
            Stats.RallyHealth = Mathf.Max(0, Stats.RallyHealth - 1);
            Stats.CurrentHealth = Mathf.Min(Stats.MaxHealth, Stats.CurrentHealth + 1);
            Stats.RallyTimer = 2.5f; // Refresh window
            EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
            EmitSignal(SignalName.RallyChanged, Stats.RallyHealth, Stats.RallyTimer);
            UI.FloatingCombatText.Spawn(GetTree().CurrentScene, GlobalPosition, 0.5f, new Color(1.0f, 0.75f, 0.20f), false, "+RALLY");
        }
    }

    private void HandleRallyDecay(double delta)
    {
        if (Stats.RallyHealth > 0)
        {
            Stats.RallyTimer -= (float)delta;
            if (Stats.RallyTimer <= 0.0f)
            {
                Stats.RallyHealth = 0;
                Stats.RallyTimer = 0.0f;
                EmitSignal(SignalName.HealthChanged, Stats.CurrentHealth, Stats.MaxHealth);
                EmitSignal(SignalName.RallyChanged, 0, 0.0f);
            }
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

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }
}
