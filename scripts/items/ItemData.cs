using Godot;
using System.Collections.Generic;

namespace Veilbound.Items;

/// <summary>
/// Definition of a passive item that permanently upgrades the player during a run.
/// </summary>
public class ItemData
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Color ItemColor { get; set; }

    // Stat Modifiers
    public float DamageBonus { get; set; } = 0.0f;
    public float FireRateReduction { get; set; } = 0.0f; // Lowers shoot cooldown (Tears up)
    public float SpeedBonus { get; set; } = 0.0f;
    public int MaxHealthBonus { get; set; } = 0;
    public float RangeBonus { get; set; } = 0.0f;
    public int MultishotBonus { get; set; } = 0;

    /// <summary>
    /// Pool of items available in treasure rooms.
    /// </summary>
    public static readonly List<ItemData> ItemCatalog = new()
    {
        new ItemData
        {
            Name = "Veil Tear",
            Description = "Damage Up!",
            ItemColor = new Color(0.3f, 0.7f, 1.0f),
            DamageBonus = 1.5f
        },
        new ItemData
        {
            Name = "Quickening Shard",
            Description = "Tears Up!",
            ItemColor = new Color(0.4f, 1.0f, 0.5f),
            FireRateReduction = 0.1f
        },
        new ItemData
        {
            Name = "Astral Boots",
            Description = "Speed Up!",
            ItemColor = new Color(1.0f, 0.85f, 0.2f),
            SpeedBonus = 50.0f
        },
        new ItemData
        {
            Name = "Soul Vessel",
            Description = "Max HP Up!",
            ItemColor = new Color(0.9f, 0.2f, 0.35f),
            MaxHealthBonus = 2
        },
        new ItemData
        {
            Name = "Ghost Needle",
            Description = "Range & Damage Up!",
            ItemColor = new Color(0.75f, 0.4f, 0.95f),
            DamageBonus = 0.8f,
            RangeBonus = 120.0f
        },
        new ItemData
        {
            Name = "Twin Prism",
            Description = "Double Shot! (2 Parallel Tears)",
            ItemColor = new Color(0.25f, 0.95f, 0.85f),
            MultishotBonus = 1,
            DamageBonus = 0.5f
        },
        new ItemData
        {
            Name = "Trinity Core",
            Description = "Triple Shot! (3 Spreading Tears)",
            ItemColor = new Color(0.98f, 0.45f, 0.15f),
            MultishotBonus = 2,
            FireRateReduction = 0.04f
        }
    };

    public static ItemData GetRandomItem()
    {
        int index = GD.RandRange(0, ItemCatalog.Count - 1);
        return ItemCatalog[index];
    }
}
