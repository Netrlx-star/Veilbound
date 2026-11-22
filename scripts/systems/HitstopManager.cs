using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Manages micro freeze-frames (hitstop) on projectile impacts, critical strikes, and heavy hits.
/// Brief temporal dampening gives physical inertia and kinetic weight to 2D pixel combat.
/// </summary>
public static class HitstopManager
{
    private static bool _isFrozen = false;

    /// <summary>
    /// Freezes global simulation timescale for the specified duration (in wall-clock seconds).
    /// Standard hit: ~0.035s (2 frames). Critical hit: ~0.065s (4 frames).
    /// </summary>
    public static void Freeze(Node caller, float durationSeconds = 0.035f)
    {
        if (_isFrozen || caller == null) return;

        var tree = caller.GetTree();
        if (tree == null) return;

        _isFrozen = true;
        Engine.TimeScale = 0.05f;

        var timer = tree.CreateTimer(durationSeconds, processAlways: true, processInPhysics: false, ignoreTimeScale: true);
        timer.Timeout += () =>
        {
            Engine.TimeScale = 1.0f;
            _isFrozen = false;
        };
    }
}
