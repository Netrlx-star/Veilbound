using Godot;
using System.Collections.Generic;

namespace Veilbound.Enemies;

public enum StatusEffectType
{
    None,
    Poison, // Toxic DoT: deals damage every 0.7s, sickly green glow
    Burn,   // Fiery DoT: rapid ticking damage, fiery orange glow
    Slow,   // Frost/Web: cuts movement speed & projectile speed by 45%, cyan glow
    Bleed   // Blood rupture: stacks up to 3; on 3rd stack explodes for 12 burst damage
}

public class ActiveStatus
{
    public StatusEffectType Type;
    public float Duration;
    public float TickTimer;
    public float DamagePerTick;
    public int Stacks;
}

/// <summary>
/// Status effect controller that runs on enemies.
/// Manages Poison, Burn, Slow, and Bleed stack effects, color modulation, and damage ticks.
/// </summary>
public partial class StatusEffectManager : Node
{
    private readonly Dictionary<StatusEffectType, ActiveStatus> _activeEffects = new();
    private BaseEnemy _enemy;
    private Sprite2D _sprite;
    private float _originalSpeed;

    public override void _Ready()
    {
        _enemy = GetParent<BaseEnemy>();
        _sprite = _enemy?.GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_enemy != null)
        {
            _originalSpeed = _enemy.MoveSpeed;
        }
    }

    public void ApplyEffect(StatusEffectType type, float duration, float damagePerTick = 1.0f)
    {
        if (_enemy == null || type == StatusEffectType.None) return;

        if (_activeEffects.TryGetValue(type, out var existing))
        {
            existing.Duration = Mathf.Max(existing.Duration, duration);
            if (type == StatusEffectType.Bleed)
            {
                existing.Stacks++;
                if (existing.Stacks >= 3)
                {
                    // Bleed rupture burst!
                    _enemy.TakeDamage(12.0f, false);
                    UI.FloatingCombatText.Spawn(GetTree().CurrentScene, _enemy.GlobalPosition, 12.0f, new Color(0.95f, 0.2f, 0.3f), true, "RUPTURE! 12");
                    Systems.CameraShaker.Shake(0.22f);
                    _activeEffects.Remove(type);
                    UpdateModulation();
                    return;
                }
            }
        }
        else
        {
            _activeEffects[type] = new ActiveStatus
            {
                Type = type,
                Duration = duration,
                TickTimer = 0.0f,
                DamagePerTick = damagePerTick,
                Stacks = 1
            };
        }

        UpdateModulation();
    }

    public override void _Process(double delta)
    {
        if (_enemy == null || _activeEffects.Count == 0) return;

        var expired = new List<StatusEffectType>();

        // Handle Slow modifier
        if (_activeEffects.ContainsKey(StatusEffectType.Slow))
        {
            _enemy.MoveSpeed = _originalSpeed * 0.55f;
        }
        else
        {
            _enemy.MoveSpeed = _originalSpeed;
        }

        foreach (var kvp in _activeEffects)
        {
            var effect = kvp.Value;
            effect.Duration -= (float)delta;
            effect.TickTimer += (float)delta;

            float tickInterval = effect.Type == StatusEffectType.Burn ? 0.40f : 0.75f;

            if (effect.TickTimer >= tickInterval)
            {
                effect.TickTimer = 0.0f;
                // Deal DoT damage with themed floating text
                if (effect.Type == StatusEffectType.Poison)
                {
                    _enemy.TakeDamage(effect.DamagePerTick, false);
                    UI.FloatingCombatText.Spawn(GetTree().CurrentScene, _enemy.GlobalPosition, effect.DamagePerTick, new Color(0.35f, 0.95f, 0.35f));
                }
                else if (effect.Type == StatusEffectType.Burn)
                {
                    _enemy.TakeDamage(effect.DamagePerTick, false);
                    UI.FloatingCombatText.Spawn(GetTree().CurrentScene, _enemy.GlobalPosition, effect.DamagePerTick, new Color(1.0f, 0.55f, 0.15f));
                }
            }

            if (effect.Duration <= 0)
            {
                expired.Add(kvp.Key);
            }
        }

        foreach (var type in expired)
        {
            _activeEffects.Remove(type);
        }

        if (expired.Count > 0)
        {
            UpdateModulation();
        }
    }

    private void UpdateModulation()
    {
        if (_sprite == null) return;

        if (_activeEffects.ContainsKey(StatusEffectType.Poison))
        {
            _sprite.Modulate = new Color(0.4f, 1.0f, 0.4f); // Sickly Green
        }
        else if (_activeEffects.ContainsKey(StatusEffectType.Burn))
        {
            _sprite.Modulate = new Color(1.0f, 0.55f, 0.2f); // Flaming Orange
        }
        else if (_activeEffects.ContainsKey(StatusEffectType.Slow))
        {
            _sprite.Modulate = new Color(0.4f, 0.85f, 1.0f); // Frost Cyan
        }
        else if (_activeEffects.ContainsKey(StatusEffectType.Bleed))
        {
            _sprite.Modulate = new Color(0.95f, 0.25f, 0.35f); // Crimson Bleed
        }
        else
        {
            _sprite.Modulate = Colors.White;
        }
    }
}
