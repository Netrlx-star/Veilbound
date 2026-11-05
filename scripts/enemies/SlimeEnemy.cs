using Godot;

namespace Veilbound.Enemies;

/// <summary>
/// A crimson slime enemy that hops and squishes towards the player.
/// Features lively squash-and-stretch hopping physics and slime splatter impacts.
/// </summary>
public partial class SlimeEnemy : BaseEnemy
{
    private enum SlimeState { Idle, Windup, Leaping, Landing }

    [Export] public float HopInterval { get; set; } = 1.2f;
    [Export] public float LeapSpeed { get; set; } = 180.0f;

    private SlimeState _state = SlimeState.Idle;
    private double _stateTimer = 0.5;
    private Node2D _player;
    private Vector2 _leapDirection = Vector2.Zero;
    private float _jigglePhase = 0.0f;

    protected override void EnemyReady()
    {
        MaxHealth = 12.0f;
        CurrentHealth = MaxHealth;
        MoveSpeed = LeapSpeed;
        ContactDamage = 1.0f;

        if (Sprite != null)
        {
            Sprite.Texture = Systems.AssetGenerator.SlimeTexture;
        }
    }

    protected override void EnemyPhysicsProcess(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;
        _stateTimer -= delta;
        _jigglePhase += (float)delta * 6.0f;

        switch (_state)
        {
            case SlimeState.Idle:
                // Gentle breathing squish
                if (Sprite != null)
                {
                    float squish = Mathf.Sin(_jigglePhase) * 0.06f;
                    Sprite.Scale = new Vector2(1.0f + squish, 1.0f - squish);
                }
                Velocity = Velocity.MoveToward(Vector2.Zero, 400.0f * (float)delta);

                if (_stateTimer <= 0 && _player != null)
                {
                    _state = SlimeState.Windup;
                    _stateTimer = 0.25; // Windup duration
                    _leapDirection = (_player.GlobalPosition - GlobalPosition).Normalized();

                    // Windup squash wide
                    if (Sprite != null)
                    {
                        var tween = CreateTween();
                        tween.TweenProperty(Sprite, "scale", new Vector2(1.35f, 0.65f), 0.22)
                            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    }
                }
                break;

            case SlimeState.Windup:
                Velocity = Vector2.Zero;
                if (_stateTimer <= 0)
                {
                    _state = SlimeState.Leaping;
                    _stateTimer = 0.45; // Leap duration
                    Velocity = _leapDirection * LeapSpeed;

                    // Stretch tall while airborne
                    if (Sprite != null)
                    {
                        Sprite.FlipH = _leapDirection.X < 0;
                        var tween = CreateTween();
                        tween.TweenProperty(Sprite, "scale", new Vector2(0.75f, 1.35f), 0.15)
                            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                    }
                }
                break;

            case SlimeState.Leaping:
                // Decelerate slightly near end of leap
                Velocity = Velocity.MoveToward(_leapDirection * (LeapSpeed * 0.4f), 120.0f * (float)delta);

                if (_stateTimer <= 0)
                {
                    _state = SlimeState.Landing;
                    _stateTimer = 0.2; // Landing squish duration

                    // Land with impact squish
                    if (Sprite != null)
                    {
                        var tween = CreateTween();
                        tween.TweenProperty(Sprite, "scale", new Vector2(1.4f, 0.6f), 0.12)
                            .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
                    }
                }
                break;

            case SlimeState.Landing:
                Velocity = Velocity.MoveToward(Vector2.Zero, 600.0f * (float)delta);
                if (_stateTimer <= 0)
                {
                    _state = SlimeState.Idle;
                    _stateTimer = (float)GD.RandRange(0.6, HopInterval);

                    // Spring back to normal scale
                    if (Sprite != null)
                    {
                        var tween = CreateTween();
                        tween.TweenProperty(Sprite, "scale", Vector2.One, 0.15)
                            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                    }
                }
                break;
        }
    }
}
