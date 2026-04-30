namespace ConsoleRpgEntities.Models.Containers;

/// <summary>
/// EquipmentSlot - one wearable position on a Character (Head, Body, Weapon, etc.).
///
/// Each Equipment container owns a fixed set of EquipmentSlots — one per
/// SlotType. <see cref="EquippedItem"/> (nullable) is the Item currently
/// occupying the slot, or null if the slot is empty.
/// </summary>
public class EquipmentSlot
{
    public int Id { get; set; }

    public SlotType SlotType { get; set; }

    public int? EquippedItemId { get; set; }
    public virtual Item? EquippedItem { get; set; }

    public int? EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }
}
