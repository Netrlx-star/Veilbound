using Godot;

namespace Veilbound.Items;

public enum ActiveItemType
{
    None,
    HourglassOfTime, // Freeze all enemies for 4.0s (3 charges)
    AstralBurst,     // Detonates shockwave, disintegrating enemy bullets & knockback (2 charges)
    CrucibleOfGreed, // Converts all pots & rocks in room into coins/hearts (4 charges)
    BloodRitual      // Consumes 1 half-heart to fire a 10-blade 360° fan of piercing tears (1 charge)
}

/// <summary>
/// Data definition for rechargeable Spacebar Active Items.
/// </summary>
public class ActiveItemData
{
    public ActiveItemType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int MaxCharges { get; set; }
    public Color ItemColor { get; set; }

    public static readonly System.Collections.Generic.List<ActiveItemData> ActiveItemCatalog = new()
    {
        new ActiveItemData
        {
            Type = ActiveItemType.AstralBurst,
            Name = "Astral Burst",
            Description = "Clear bullets & blast enemies! (2 Charges)",
            MaxCharges = 2,
            ItemColor = new Color(0.3f, 0.85f, 1.0f)
        },
        new ActiveItemData
        {
            Type = ActiveItemType.HourglassOfTime,
            Name = "Hourglass of Time",
            Description = "Freeze all enemies for 4s! (3 Charges)",
            MaxCharges = 3,
            ItemColor = new Color(0.95f, 0.8f, 0.25f)
        },
        new ActiveItemData
        {
            Type = ActiveItemType.CrucibleOfGreed,
            Name = "Alchemist's Crucible",
            Description = "Turn rocks & pots into riches! (4 Charges)",
            MaxCharges = 4,
            ItemColor = new Color(0.85f, 0.35f, 0.95f)
        },
        new ActiveItemData
        {
            Type = ActiveItemType.BloodRitual,
            Name = "Bloodbound Dagger",
            Description = "Pay blood for a 360° blade storm! (1 Charge)",
            MaxCharges = 1,
            ItemColor = new Color(0.95f, 0.15f, 0.25f)
        }
    };

    public static ActiveItemData GetRandomActiveItem()
    {
        int index = GD.RandRange(0, ActiveItemCatalog.Count - 1);
        return ActiveItemCatalog[index];
    }
}
