namespace ConsoleRpgEntities.Models.Containers;

/// <summary>
/// SlotType - the canonical set of equipment slots a character has.
///
/// Stored as int by EF Core (Head=0..Accessory=8). Used both by
/// <see cref="Item.EligibleSlot"/> (which slot CAN this item go in?) and
/// by <see cref="EquipmentSlot.SlotType"/> (which slot IS this slot?).
/// </summary>
public enum SlotType
{
    Head,
    Body,
    Legs,
    Feet,
    Hands,
    Weapon,
    Shield,
    Ring,
    Accessory
}
