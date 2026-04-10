using ConsoleRpgEntities.Models.Containers;

namespace ConsoleRpgEntities.Models.World;

/// <summary>
/// Door - A connection between two rooms that can be locked, trapped, or hidden.
///
/// =======================================================
/// THE BIG PAYOFF OF WEEK 14
/// =======================================================
/// Door implements ILockable - the exact same interface you used for Chest
/// in Week 13. That means:
///
///     Player.TryUnlock(chest, key)  // Works from W13
///     Player.TryUnlock(door,  key)  // ALSO WORKS (same method signature)
///
/// Wait... does it? Look at the TryUnlock method signature in Player.cs.
/// It takes a Chest, not an ILockable. That's an assignment task this week:
/// refactor TryUnlock to take an ILockable instead. Then your single unlock
/// implementation works for BOTH chests and doors without duplication.
///
/// This is Liskov Substitution and Open/Closed Principle hand in hand:
/// chest code and door code share one implementation because they share
/// one interface. Add a third ILockable entity later (a locked journal?
/// a portal? a gate?) and it automatically works with TryUnlock.
///
/// =======================================================
/// DOOR IS NOT A CONTAINER
/// =======================================================
/// Notice that Door is NOT a Container subclass. Doors don't hold items.
/// This is Interface Segregation Principle at work: chests have BOTH
/// IItemContainer AND ILockable, but doors only have ILockable. We don't
/// force doors to implement methods they don't need.
///
/// =======================================================
/// SINGLE ROW PER DOOR
/// =======================================================
/// A door between Room 1 and Room 2 is ONE row, not two. When navigating,
/// the GameEngine looks for a door where
///     (RoomAId, RoomBId) matches (current, target) in either order.
/// This keeps the door state (locked/unlocked, trap triggered, discovered)
/// in exactly one place.
/// </summary>
public class Door : ILockable
{
    public int Id { get; set; }

    /// <summary>
    /// Optional descriptive name, e.g. "Iron Gate", "Creaky Wooden Door".
    /// </summary>
    public string Name { get; set; } = "Door";

    // ============================================================
    // ROOM CONNECTIONS (required - both must be non-null)
    // ============================================================
    // A door always connects two rooms. Secret doors use the IsSecret flag
    // below rather than a missing side - that way the database can enforce
    // that every door is connected to two real rooms.
    public int RoomAId { get; set; }
    public virtual Room RoomA { get; set; } = null!;

    public int RoomBId { get; set; }
    public virtual Room RoomB { get; set; } = null!;

    // ============================================================
    // ILockable
    // ============================================================
    public bool IsLocked { get; set; }
    public bool IsTrapped { get; set; }
    public string? RequiredKeyId { get; set; }
    public bool IsPickable { get; set; } = true;

    /// <summary>
    /// True once the trap has been triggered or disarmed. Prevents the trap
    /// from firing repeatedly every time the door is opened.
    /// </summary>
    public bool TrapDisarmed { get; set; }

    /// <summary>
    /// Damage the trap deals on first passage. Zero means no damage trap.
    /// </summary>
    public int TrapDamage { get; set; }

    // ============================================================
    // SECRET DOORS (new W14 state not on Chest)
    // ============================================================

    /// <summary>
    /// True if this door is hidden from normal view. Secret doors don't
    /// show up in the Room display until they're discovered.
    /// </summary>
    public bool IsSecret { get; set; }

    /// <summary>
    /// True once the player has discovered a secret door. Once discovered,
    /// it appears in the room display like any other door and persists
    /// across sessions.
    /// </summary>
    public bool IsDiscovered { get; set; }

    /// <summary>
    /// Helper: returns true if the door is visible to the player right now.
    /// Regular doors are always visible; secret doors are only visible once
    /// discovered.
    /// </summary>
    public bool IsVisible => !IsSecret || IsDiscovered;
}
