using Godot;
using System.Collections.Generic;

namespace Veilbound.Player;

/// <summary>
/// A dynamic sweeping melee blade slash arc executed by the Bladesmith character.
/// Slashes across a wide arc in front of the player, slicing through all enemies
/// and obstacles within range and applying heavy knockback.
/// </summary>
public partial class SwordSlashArc : Area2D
{
    public float Damage { get; set; } = 7.0f;
    public float KnockbackForce { get; set; } = 280.0f;
    public Vector2 SlashDirection { get; set; } = Vector2.Right;
    public float SlashRadius { get; set; } = 54.0f;
    public Enemies.StatusEffectType InflictedStatus { get; set; } = Enemies.StatusEffectType.None;

    private readonly HashSet<Node2D> _hitTargets = new();
    private float _slashProgress = 0.0f; // 0.0 to 1.0
    private float _startAngle;
    private float _sweepAngle = Mathf.DegToRad(120.0f);
    private const float SlashDuration = 0.20f;

    // Visual colors
    private static readonly Color ArcCoreColor = new(0.95f, 1.0f, 1.0f, 0.95f);
    private static readonly Color ArcGlowColor = new(0.25f, 0.85f, 1.0f, 0.85f);
    private static readonly Color ArcOuterColor = new(0.15f, 0.5f, 0.95f, 0.4f);

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 4 | 1 | 8; // 4 = enemies, 1 = obstacles, 8 = projectiles (or check areas)

        _startAngle = SlashDirection.Angle() - _sweepAngle / 2.0f;

        // Build precise arc collision shape
        var collisionShape = new CollisionPolygon2D();
        var points = new List<Vector2> { Vector2.Zero };
        int segments = 10;
        for (int i = 0; i <= segments; i++)
        {
            float angle = _startAngle + (i / (float)segments) * _sweepAngle;
            points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SlashRadius);
        }
        collisionShape.Polygon = points.ToArray();
        AddChild(collisionShape);

        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;

        // Animated slash sweep tween
        var tween = CreateTween();
        tween.TweenProperty(this, nameof(_slashProgress), 1.0f, SlashDuration)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
        CheckCollisions();
    }

    private void CheckCollisions()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (body is Node2D node)
            {
                OnBodyEntered(node);
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_hitTargets.Contains(body)) return;
        _hitTargets.Add(body);

        if (body is Enemies.BaseEnemy enemy)
        {
            enemy.TakeDamage(Damage);
            Vector2 hitDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
            if (hitDir == Vector2.Zero) hitDir = SlashDirection;
            enemy.ApplyKnockback(hitDir, KnockbackForce);

            // Melee sword naturally inflicts Bleed or inherited elemental status
            var status = InflictedStatus != Enemies.StatusEffectType.None ? InflictedStatus : Enemies.StatusEffectType.Bleed;
            enemy.StatusManager?.ApplyEffect(status, 4.0f, Damage * 0.35f);
        }
        else if (body is Environments.RoomObstacle obstacle)
        {
            obstacle.TakeDamage(Damage);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Deflect/destroy enemy projectiles caught in the blade's sweep (Blasphemy / Casey mechanic)
        if (area is Enemies.EnemyProjectile projectile)
        {
            projectile.IsDeflected = true;
            projectile.Direction = SlashDirection;
            projectile.Speed *= 1.4f;
            projectile.Modulate = new Color(0.4f, 1.0f, 0.9f); // Cyan reflected glow

            // Micro-screenshake on projectile parry
            Systems.CameraShaker.Shake(0.18f);
        }
    }

    public override void _Draw()
    {
        if (_slashProgress >= 1.0f) return;

        float alpha = 1.0f - _slashProgress;
        float currentMaxAngle = _startAngle + _sweepAngle * Mathf.Min(1.0f, _slashProgress * 1.5f);
        int steps = 14;

        // Draw outer energy aura
        var outerPoints = new Vector2[steps + 1];
        var corePoints = new Vector2[steps + 1];
        var innerPoints = new Vector2[steps + 1];

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = Mathf.Lerp(_startAngle, currentMaxAngle, t);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            outerPoints[i] = dir * (SlashRadius * 1.05f);
            corePoints[i]  = dir * (SlashRadius * 0.95f);
            innerPoints[i] = dir * (SlashRadius * 0.65f);
        }

        // Draw glowing sweep arc
        for (int i = 0; i < steps; i++)
        {
            var poly = new Vector2[] {
                innerPoints[i],
                outerPoints[i],
                outerPoints[i + 1],
                innerPoints[i + 1]
            };
            Color glow = new Color(ArcGlowColor.R, ArcGlowColor.G, ArcGlowColor.B, ArcGlowColor.A * alpha);
            DrawColoredPolygon(poly, glow);
        }

        // Draw razor sharp blade core edge
        for (int i = 0; i < steps; i++)
        {
            Color core = new Color(ArcCoreColor.R, ArcCoreColor.G, ArcCoreColor.B, ArcCoreColor.A * alpha);
            DrawLine(corePoints[i], corePoints[i + 1], core, 2.5f);
        }

        // Draw tip spark
        if (corePoints.Length > 0)
        {
            Vector2 tip = corePoints[corePoints.Length - 1];
            DrawCircle(tip, 3.5f * alpha, Colors.White);
        }
    }
}
