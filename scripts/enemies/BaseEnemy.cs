using Godot;

namespace Veilbound.Enemies;

public enum ChampionType
{
    None,
    Crimson, // +60% HP, deals 2 damage, drops guaranteed Red Heart
    Zephyr,  // +40% Speed, agile pursuit, drops guaranteed Coin
    Plague,  // Leaves poison cloud/trail, drops guaranteed Bomb
    Volatile // Explodes into 6 projectiles upon death, drops guaranteed Key
}

/// <summary>
/// Base class for all enemies. Provides health, damage, knockback,
/// and death handling. Specific enemy types inherit from this.
/// Supports randomized Champion variants (Crimson, Zephyr, Plague, Volatile).
/// </summary>
public partial class BaseEnemy : CharacterBody2D
{
    // ── Stats ──
    [Export] public float MaxHealth { get; set; } = 10.0f;
    [Export] public float ContactDamage { get; set; } = 1.0f;
    [Export] public float MoveSpeed { get; set; } = 80.0f;
    public ChampionType Champion { get; set; } = ChampionType.None;

    protected float CurrentHealth;
    protected bool IsDead = false;

    // ── Knockback ──
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private const float KnockbackFriction = 600.0f;

    // ── References ──
    protected Sprite2D Sprite;
    protected CollisionShape2D CollisionShape;
    public StatusEffectManager StatusManager { get; private set; }

    // ── Signals ──
    [Signal] public delegate void EnemyDiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("enemies");

        Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        CollisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        StatusManager = new StatusEffectManager();
        AddChild(StatusManager);

        // 12% chance to become an authentic Isaac-style Champion variant (excluding bosses)
        if (Champion == ChampionType.None && !(this is Bosses.BossEnemy) && GD.Randf() < 0.12f)
        {
            var champions = new ChampionType[] {
                ChampionType.Crimson,
                ChampionType.Zephyr,
                ChampionType.Plague,
                ChampionType.Volatile
            };
            ApplyChampionVariant(champions[GD.RandRange(0, champions.Length - 1)]);
        }

        EnemyReady();
    }

    public void ApplyChampionVariant(ChampionType type)
    {
        Champion = type;

        switch (type)
        {
            case ChampionType.Crimson:
                MaxHealth *= 1.6f;
                CurrentHealth = MaxHealth;
                ContactDamage = 2.0f; // Full heart contact damage
                Scale = new Vector2(1.25f, 1.25f);
                if (Sprite != null) Sprite.Modulate = new Color(1.0f, 0.25f, 0.25f);
                break;

            case ChampionType.Zephyr:
                MoveSpeed *= 1.4f;
                Scale = new Vector2(0.9f, 0.9f);
                if (Sprite != null) Sprite.Modulate = new Color(1.0f, 0.95f, 0.25f);
                break;

            case ChampionType.Plague:
                MaxHealth *= 1.2f;
                CurrentHealth = MaxHealth;
                if (Sprite != null) Sprite.Modulate = new Color(0.35f, 1.0f, 0.35f);
                break;

            case ChampionType.Volatile:
                MaxHealth *= 1.2f;
                CurrentHealth = MaxHealth;
                if (Sprite != null) Sprite.Modulate = new Color(1.0f, 0.55f, 0.15f);
                break;
        }
    }

    /// <summary>
    /// Override in subclasses for specific enemy initialization.
    /// </summary>
    protected virtual void EnemyReady() { }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead) return;

        // Apply knockback
        if (_knockbackVelocity.Length() > 10.0f)
        {
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * (float)delta);
        }
        else
        {
            _knockbackVelocity = Vector2.Zero;
        }

        EnemyPhysicsProcess(delta);

        // Combine movement velocity with knockback
        Velocity += _knockbackVelocity;
        MoveAndSlide();

        // Contact damage with player
        CheckPlayerContact();
    }

    private void CheckPlayerContact()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is Player.PlayerController player)
            {
                player.TakeDamage((int)ContactDamage);
            }
        }
    }

    /// <summary>
    /// Override in subclasses for specific enemy movement/behavior.
    /// Set Velocity in this method (before knockback is added).
    /// </summary>
    protected virtual void EnemyPhysicsProcess(double delta) { }

    // ══════════════════════════════════════════════
    //  DAMAGE
    // ══════════════════════════════════════════════

    /// <summary>
    /// Called when this enemy takes damage (from projectiles, etc.)
    /// </summary>
    public void TakeDamage(float damage, bool spawnFct = true)
    {
        if (IsDead) return;

        CurrentHealth -= damage;
        bool isCrit = damage >= 6.0f;

        if (spawnFct)
        {
            Color numColor = isCrit ? new Color(1.0f, 0.85f, 0.2f) : new Color(1.0f, 0.95f, 0.9f);
            UI.FloatingCombatText.Spawn(GetTree().CurrentScene, GlobalPosition, damage, numColor, isCrit);
        }

        // 1. Kinetic Hitstop Freeze-Frame (2 frames normal, 4 frames crit)
        Systems.HitstopManager.Freeze(this, isCrit ? 0.065f : 0.035f);

        // 2. Camera Trauma Shake
        Systems.CameraShaker.Shake(isCrit ? 0.26f : 0.12f);

        // 3. Multi-frame White Specular Flash & Elastic Squash-and-Stretch
        if (Sprite != null)
        {
            var flashTween = CreateTween();
            flashTween.TweenProperty(Sprite, "modulate", new Color(2.5f, 2.5f, 2.5f, 1.0f), 0.03); // Instant bright white flash
            flashTween.TweenProperty(Sprite, "modulate", new Color(1.0f, 0.25f, 0.25f, 1.0f), 0.08); // Blood red tint
            flashTween.TweenProperty(Sprite, "modulate", Colors.White, 0.12);

            var baseScale = Sprite.Scale;
            var squashed = new Vector2(baseScale.X * 1.25f, baseScale.Y * 0.80f);
            var stretchTween = CreateTween();
            stretchTween.TweenProperty(Sprite, "scale", squashed, 0.03);
            stretchTween.TweenProperty(Sprite, "scale", baseScale, 0.14)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        // 4. Directional Impact Hit Sparks
        SpawnHitSparks(GlobalPosition, isCrit);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void SpawnHitSparks(Vector2 pos, bool isCrit)
    {
        var sparks = new CpuParticles2D
        {
            GlobalPosition = pos,
            Emitting = true,
            OneShot = true,
            Explosiveness = 0.95f,
            Amount = isCrit ? 12 : 6,
            Lifetime = 0.35,
            Spread = 180.0f,
            Gravity = new Vector2(0, 90),
            InitialVelocityMin = 40.0f,
            InitialVelocityMax = isCrit ? 110.0f : 75.0f,
            ScaleAmountMin = 1.5f,
            ScaleAmountMax = isCrit ? 3.0f : 2.2f,
            Color = isCrit ? new Color(1.0f, 0.88f, 0.35f) : new Color(0.95f, 0.30f, 0.35f)
        };
        GetTree().CurrentScene?.AddChild(sparks);
        sparks.GetTree()?.CreateTimer(0.5).Connect("timeout", Callable.From(sparks.QueueFree));
    }

    /// <summary>
    /// Apply knockback force to this enemy.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        _knockbackVelocity = direction.Normalized() * force;
    }

    protected virtual void Die()
    {
        IsDead = true;
        EmitSignal(SignalName.EnemyDied);

        var player = GetTree().GetFirstNodeInGroup("player") as Player.PlayerController;
        if (player != null)
        {
            player.Stats.EnemiesKilled++;
        }

        // Drop system: Hearts, Coins, Bombs, Keys
        SpawnDrops(player);

        // Volatile champion death explosion
        if (Champion == ChampionType.Volatile)
        {
            SpawnVolatileBurst();
        }

        // Simple death animation: shrink and fade
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.3);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void SpawnVolatileBurst()
    {
        var targetParent = GetParent() ?? (Node)GetTree().CurrentScene;
        int projectileCount = 6;
        float step = Mathf.Tau / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = i * step;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var enemyProj = new EnemyProjectile
            {
                Direction = dir,
                Speed = 220.0f,
                Damage = 1,
                GlobalPosition = GlobalPosition
            };
            targetParent.CallDeferred(Node.MethodName.AddChild, enemyProj);
        }

        Systems.CameraShaker.Shake(0.35f);
    }

    private void SpawnDrops(Player.PlayerController player)
    {
        var targetParent = GetParent() ?? (Node)GetTree().CurrentScene;

        // Guaranteed thematic champion rewards!
        if (Champion == ChampionType.Crimson)
        {
            var heart = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, heart);
            heart.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Heart, 0, 1);
            return;
        }
        else if (Champion == ChampionType.Zephyr)
        {
            var coin = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, coin);
            coin.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Coin, 0, 2);
            return;
        }
        else if (Champion == ChampionType.Plague)
        {
            var bomb = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, bomb);
            bomb.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Bomb, 0, 1);
            return;
        }
        else if (Champion == ChampionType.Volatile)
        {
            var key = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, key);
            key.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Key, 0, 1);
            return;
        }

        float roll = GD.Randf();

        // 1. Heart drop: 30% base (45% if hurt)
        float heartChance = (player != null && player.Stats.CurrentHealth < player.Stats.MaxHealth) ? 0.45f : 0.30f;
        if (roll < heartChance)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Heart, 0, 1);
            return;
        }

        // 2. Coin drop: 25% chance
        if (roll < heartChance + 0.25f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Coin, 0, 1);
            return;
        }

        // 3. Bomb drop: 8% chance (refills player's bombs!)
        if (roll < heartChance + 0.25f + 0.08f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Bomb, 0, 1);
            return;
        }

        // 4. Key drop: 8% chance (allows unlocking shops & chests!)
        if (roll < heartChance + 0.25f + 0.08f + 0.08f)
        {
            var pickup = new Items.ConsumablePickup();
            targetParent.CallDeferred(Node.MethodName.AddChild, pickup);
            pickup.CallDeferred(nameof(Items.ConsumablePickup.Initialize), GlobalPosition, (int)Items.ConsumableType.Key, 0, 1);
        }
    }
}
