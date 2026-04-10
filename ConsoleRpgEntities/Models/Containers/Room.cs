namespace ConsoleRpgEntities.Models.Containers;

/// <summary>
/// Room - A place in the game world. Players and monsters exist inside rooms,
/// and items dropped on the floor live here.
///
/// =======================================================
/// ROOM IS A CONTAINER - THIS IS THE BIG IDEA OF WEEK 14
/// =======================================================
/// In Week 12 we built Inventory and Equipment as Container subclasses.
/// In Week 13 we added Chest and MonsterLoot as Container subclasses.
/// This week, Room joins the hierarchy as the fifth Container subclass.
///
/// Why is that important? Because dropping an item on the floor is now
/// literally the same operation as putting it in any other container:
///
///     item.ContainerId = currentRoom.Id;   // item is now on the floor
///     item.ContainerId = player.Inventory.Id; // item is now in backpack
///     item.ContainerId = chest.Id;         // item is now inside the chest
///
/// Picking up, dropping, moving between chests - all ONE FK update.
/// This is the culmination of the "items are instances, not types" idea
/// from Week 12. Every item in the entire game world lives in exactly one
/// Container at all times, and that Container might be a backpack, an
/// equipment slot, a chest, a monster corpse, or the floor of a room.
///
/// =======================================================
/// SELF-REFERENCING NAVIGATION
/// =======================================================
/// Rooms connect to other rooms via nullable foreign keys for each compass
/// direction (North, South, East, West). This is the same self-referencing
/// pattern you've seen in earlier weeks - a Room's FK points to another Room.
///
/// If Room 1 has NorthRoomId = 2, then going "north" from Room 1 takes the
/// player to Room 2. When you set up the seed data, remember that most exits
/// should be bidirectional: if Room 1's NorthRoomId = 2, then Room 2's
/// SouthRoomId should = 1.
/// </summary>
public class Room : Container
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ============================================================
    // MAP COORDINATES (for optional ASCII minimap in the final project)
    // ============================================================
    // Not used in the base W14 assignment, but included so the seed data
    // has something to display if you (or a student) wants to render a map.
    // The w15-final template has a full Spectre.Console map implementation
    // that reads these coordinates.
    public int X { get; set; }
    public int Y { get; set; }

    // ============================================================
    // SELF-REFERENCING EXITS
    // ============================================================
    // Nullable because a room may not have an exit in every direction.
    public int? NorthRoomId { get; set; }
    public int? SouthRoomId { get; set; }
    public int? EastRoomId { get; set; }
    public int? WestRoomId { get; set; }

    public virtual Room? NorthRoom { get; set; }
    public virtual Room? SouthRoom { get; set; }
    public virtual Room? EastRoom { get; set; }
    public virtual Room? WestRoom { get; set; }
}
