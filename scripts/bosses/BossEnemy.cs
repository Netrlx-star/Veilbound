using Godot;

namespace Veilbound.Bosses;

/// <summary>
/// First major boss: Veil Goliath.
/// Inspired by Monstro from The Binding of Isaac.
/// Features high HP, leap-slam attacks with projectile bursts, and aimed spit attacks.
/// </summary>
public partial class BossEnemy : Enemies.BaseEnemy
{
    public const string BossName = "VEIL GOLIATH";

    private enum BossState { Idle, PreparingLeap, InAir, Landing, SpewingTears }

    private BossState _state = BossState.Idle;
    private double _stateTimer = 1.5;
    private Node2D _player;
    private Vector2 _leapTarget;
    private Vector2 _leapStart;
    private float _leapProgress = 0.0f;
    private PackedScene _projectileScene;

    [Signal] public delegate void BossHealthUpdatedEventHandler(float current, float max);

    protected override void EnemyReady()
    {
        MaxHealth = 80.0f;
        CurrentHealth = MaxHealth;
        MoveSpeed = 40.0f;
        ContactDamage = 2.0f;

        _projectileScene = GD.Load<PackedScene>("res://scenes/enemies/EnemyProjectile.tscn");
    }

    protected override void EnemyPhysicsProcess(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;
        _stateTimer -= delta;

        switch (_state)
        {
            case BossState.Idle:
                HandleIdleState();
                break;

            case BossState.PreparingLeap:
                HandlePreparingLeap();
                break;

            case BossState.InAir:
                HandleInAir((float)delta);
                break;

            case BossState.Landing:
                HandleLanding();
                break;

            case BossState.SpewingTears:
                HandleSpewing();
                break;
        }

        // Emit current health for HUD
        EmitSignal(SignalName.BossHealthUpdated, CurrentHealth, MaxHealth);
    }

    private void HandleIdleState()
    {
        if (_player != null)
        {
            // Slowly creep towards player
            var dir = (_player.GlobalPosition - GlobalPosition).Normalized();
            Velocity = dir * MoveSpeed;

            if (Sprite != null)
            {
                Sprite.FlipH = dir.X < 0;
            }
        }

        if (_stateTimer <= 0)
        {
            // Choose next attack: 60% chance leap, 40% chance spit
            if (GD.Randf() < 0.6f)
            {
                StartLeapPreparation();
            }
            else
            {
                StartSpewAttack();
            }
        }
    }

    private void StartLeapPreparation()
    {
        _state = BossState.PreparingLeap;
        _stateTimer = 0.7; // Windup duration
        Velocity = Vector2.Zero;

        // Visual squash anticipation
        if (Sprite != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(Sprite, "scale", new Vector2(1.3f, 0.7f), 0.6);
        }
    }

    private void HandlePreparingLeap()
    {
        if (_stateTimer <= 0)
        {
            _state = BossState.InAir;
            _leapStart = GlobalPosition;
            _leapTarget = _player != null ? _player.GlobalPosition : GlobalPosition;
            _leapProgress = 0.0f;
            _stateTimer = 1.0; // Air time

            if (Sprite != null)
            {
                Sprite.Scale = new Vector2(1.2f, 1.2f);
            }
        }
    }

    private void HandleInAir(float delta)
    {
        _leapProgress += delta * 1.0f; // 1 second total arc
        float t = Mathf.Clamp(_leapProgress, 0.0f, 1.0f);

        // Move towards target in X/Y plane
        GlobalPosition = _leapStart.Lerp(_leapTarget, t);

        // Simulate height using sprite offset
        float height = Mathf.Sin(t * Mathf.Pi) * 80.0f;
        if (Sprite != null)
        {
            Sprite.Position = new Vector2(0, -height);
        }

        if (t >= 1.0f)
        {
            _state = BossState.Landing;
            _stateTimer = 0.4;
        }
    }

    private void HandleLanding()
    {
        if (Sprite != null)
        {
            Sprite.Position = Vector2.Zero;
            Sprite.Scale = new Vector2(1.4f, 0.6f);
            var tween = CreateTween();
            tween.TweenProperty(Sprite, "scale", Vector2.One, 0.3);
        }

        // Radial burst of 8 projectiles upon landing
        SpawnRadialBurst(8);

        _state = BossState.Idle;
        _stateTimer = 1.5; // Brief rest
    }

    private void StartSpewAttack()
    {
        _state = BossState.SpewingTears;
        _stateTimer = 0.8;
        Velocity = Vector2.Zero;

        // Fire fan of 5 tears towards player
        if (_player != null)
        {
            Vector2 baseDir = (_player.GlobalPosition - GlobalPosition).Normalized();
            float baseAngle = baseDir.Angle();

            for (int i = -2; i <= 2; i++)
            {
                float angle = baseAngle + Mathf.DegToRad(i * 18.0f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnBossTear(dir, 200.0f + (float)GD.RandRange(-20, 20));
            }
        }

        _state = BossState.Idle;
        _stateTimer = 2.0; // Cooldown after burst
    }

    private void HandleSpewing()
    {
        // Transition handled in StartSpewAttack
    }

    private void SpawnRadialBurst(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.Tau / count) * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnBossTear(dir, 180.0f);
        }
    }

    private void SpawnBossTear(Vector2 direction, float speed)
    {
        if (_projectileScene == null) return;

        var proj = _projectileScene.Instantiate<Enemies.EnemyProjectile>();
        proj.Direction = direction;
        proj.Speed = speed;
        proj.Damage = 1;
        proj.GlobalPosition = GlobalPosition;

        GetTree().CurrentScene.AddChild(proj);
    }
}
