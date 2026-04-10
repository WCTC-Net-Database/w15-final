using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using ConsoleRpgEntities.Models.World;

namespace ConsoleRpgEntities.Models.Characters;

/// <summary>
/// Player - The user's character. Holds references to two personal Containers
/// (Inventory + Equipment), a current Room (new in W14), and has operations
/// for interacting with the world (chests, monsters, doors, and floor items).
///
/// All the operations here ultimately do the same thing: move an item from
/// one Container to another by updating its ContainerId foreign key. That
/// single action is what "pick up", "equip", "drop", "loot", and "take from
/// chest" all do.
/// </summary>
public class Player : ITargetable, IPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Health { get; set; } = 100;

    // ============================================================
    // CONTAINERS
    // ============================================================
    // A Player has exactly one Inventory and one Equipment (one-to-one each).
    // Both are stored in the shared Containers table (TPH).
    public int? InventoryId { get; set; }
    public virtual Inventory? Inventory { get; set; }

    public int? EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }

    // ============================================================
    // LOCATION (new in Week 14)
    // ============================================================
    // A Player is in exactly one Room at a time. The Room is ALSO a Container
    // (TPH), so "items on the floor of the current room" is literally
    // CurrentRoom.Items filtered via LINQ.
    public int? CurrentRoomId { get; set; }
    public virtual Room? CurrentRoom { get; set; }

    // Many-to-many: Player can know many Abilities, each Ability known by many Players.
    public virtual ICollection<Ability> Abilities { get; set; } = new List<Ability>();

    // ============================================================
    // COMBAT
    // ============================================================
    public int GetTotalAttack()
    {
        int baseAttack = Level * 2;
        int weaponBonus = Equipment?.Items.OfType<Weapon>().Sum(w => w.Attack) ?? 0;
        return baseAttack + weaponBonus;
    }

    public int GetTotalDefense()
    {
        int baseDefense = Level;
        int armorBonus = Equipment?.Items.OfType<Armor>().Sum(a => a.Defense) ?? 0;
        return baseDefense + armorBonus;
    }

    public void Attack(ITargetable target)
    {
        int damage = GetTotalAttack();
        target.Health -= damage;
        Console.WriteLine($"{Name} attacks {target.Name} for {damage} damage!");
    }

    public void UseAbility(IAbility ability, ITargetable target)
    {
        if (Abilities.Any(a => a.Id == ability.Id))
        {
            ability.Activate(this, target);
        }
        else
        {
            Console.WriteLine($"{Name} does not know {ability.Name}!");
        }
    }

    // ============================================================
    // INVENTORY OPERATIONS
    // ============================================================
    // These methods mutate the in-memory graph. Call _context.SaveChanges()
    // after any of these to persist the changes to the database.

    /// <summary>
    /// Adds an item to the backpack. Returns false if the weight limit would be exceeded.
    /// </summary>
    public bool PickUp(Item item)
    {
        if (Inventory == null)
            return false;

        int newWeight = GetCurrentWeight() + (int)item.Weight;
        if (newWeight > Inventory.MaxWeight)
        {
            Console.WriteLine($"Cannot carry {item.Name} - too heavy! ({newWeight}/{Inventory.MaxWeight})");
            return false;
        }

        Inventory.AddItem(item);
        Console.WriteLine($"{Name} picked up {item.Name}.");
        return true;
    }

    /// <summary>
    /// Drops an item from the backpack onto the floor of the current room.
    /// Since Room is a Container (W14), dropping is just changing ContainerId
    /// from the backpack to the room.
    ///
    /// If the player isn't in a room (rare), the item is orphaned.
    /// </summary>
    public void Drop(Item item)
    {
        if (Inventory == null) return;

        if (Inventory.RemoveItem(item))
        {
            if (CurrentRoom != null)
            {
                CurrentRoom.AddItem(item);
                Console.WriteLine($"{Name} dropped {item.Name} on the floor.");
            }
            else
            {
                Console.WriteLine($"{Name} dropped {item.Name}.");
            }
        }
    }

    /// <summary>
    /// Picks up an item that's currently lying on the floor of the player's
    /// current room and moves it into the backpack. Respects weight limits.
    /// </summary>
    public bool PickUpFromFloor(Item item)
    {
        if (CurrentRoom == null || !CurrentRoom.Items.Contains(item))
            return false;

        if (!PickUp(item))
            return false;

        CurrentRoom.RemoveItem(item);
        return true;
    }

    /// <summary>
    /// Moves an item from inventory into an equipment slot.
    /// </summary>
    public void Equip(Item item)
    {
        if (Inventory == null || Equipment == null) return;

        if (!Inventory.Items.Contains(item))
        {
            Console.WriteLine($"{item.Name} isn't in the backpack.");
            return;
        }

        if (item is not Weapon && item is not Armor)
        {
            Console.WriteLine($"{item.Name} can't be equipped.");
            return;
        }

        Inventory.RemoveItem(item);
        Equipment.AddItem(item);
        Console.WriteLine($"{Name} equipped {item.Name}.");
    }

    /// <summary>
    /// Moves an item from an equipment slot back into the inventory.
    /// </summary>
    public void Unequip(Item item)
    {
        if (Inventory == null || Equipment == null) return;

        if (!Equipment.Items.Contains(item))
        {
            Console.WriteLine($"{item.Name} isn't equipped.");
            return;
        }

        Equipment.RemoveItem(item);
        Inventory.AddItem(item);
        Console.WriteLine($"{Name} unequipped {item.Name}.");
    }

    /// <summary>
    /// Uses a consumable item (potion, food, scroll). Decrements Uses and
    /// removes the item from the backpack when depleted.
    /// </summary>
    public void UseItem(Item item)
    {
        if (item is not Consumable consumable)
        {
            Console.WriteLine($"{item.Name} is not usable.");
            return;
        }

        switch (consumable.EffectType)
        {
            case "Heal":
                Health += consumable.EffectAmount;
                Console.WriteLine($"{Name} restored {consumable.EffectAmount} HP (now {Health}).");
                break;
            default:
                Console.WriteLine($"{Name} used {consumable.Name}.");
                break;
        }

        consumable.Uses--;
        if (consumable.Uses <= 0)
        {
            Inventory?.RemoveItem(consumable);
        }
    }

    // ============================================================
    // WEIGHT (stretch goal support)
    // ============================================================
    public int GetCurrentWeight()
    {
        return (int)(Inventory?.Items.Sum(i => i.Weight) ?? 0);
    }

    // ============================================================
    // CHEST INTERACTION (new in Week 13)
    // ============================================================

    /// <summary>
    /// Result of attempting to open a chest. Printed by the GameEngine
    /// so the in-game feedback is consistent between chests and future doors.
    /// </summary>
    public enum OpenResult
    {
        Opened,
        Locked,
        Trapped,
        AlreadyOpen
    }

    /// <summary>
    /// Attempts to open a chest. Returns an OpenResult that the caller can
    /// react to (print a message, transfer items, apply trap damage).
    ///
    /// Trap logic: if the chest is trapped AND the trap hasn't been disarmed,
    /// opening the chest fires the trap and damages the player. The trap is
    /// then marked disarmed so it doesn't fire again.
    /// </summary>
    public OpenResult OpenChest(Chest chest)
    {
        if (!chest.IsLocked && chest.TrapDisarmed)
        {
            // Already opened (unlocked + trap handled)
            return OpenResult.AlreadyOpen;
        }

        if (chest.IsLocked)
        {
            // Don't open - caller should prompt for key or pick attempt
            return OpenResult.Locked;
        }

        if (chest.IsTrapped && !chest.TrapDisarmed)
        {
            Health -= chest.TrapDamage;
            chest.TrapDisarmed = true;
            Console.WriteLine($"A trap springs! {Name} takes {chest.TrapDamage} damage.");
            return OpenResult.Trapped;
        }

        return OpenResult.Opened;
    }

    /// <summary>
    /// Attempts to unlock ANYTHING that implements ILockable using a KeyItem
    /// from the player's inventory. Works on chests (Week 13) AND doors (Week 14)
    /// with zero duplication.
    ///
    /// =======================================================
    /// THE BIG W14 PAYOFF
    /// =======================================================
    /// In Week 13 this method took a Chest parameter. In Week 14 it was
    /// refactored to take ILockable. That single-line change made it work
    /// for doors too - because doors implement the same interface.
    ///
    /// If you add a new ILockable entity later (a gate, a padlocked journal,
    /// a portal), TryUnlock will work on that too without modification.
    /// That's the Liskov Substitution Principle paying off.
    ///
    /// Rules:
    ///   - If the target has a RequiredKeyId, the KeyItem's KeyId must match
    ///   - If the KeyItem has no KeyId (it's a lockpick), the target must be
    ///     IsPickable AND must NOT require a specific key
    ///   - On success, the target becomes unlocked. Lockpicks break on use;
    ///     specific keys are kept for reuse.
    /// </summary>
    public bool TryUnlock(ILockable target, KeyItem key)
    {
        if (!target.IsLocked)
            return true;

        // Lockpick path (KeyId is null)
        if (key.KeyId == null)
        {
            if (!target.IsPickable || target.RequiredKeyId != null)
            {
                Console.WriteLine($"{Name} cannot pick this lock.");
                return false;
            }

            target.IsLocked = false;
            Inventory?.RemoveItem(key); // lockpick breaks on use
            Console.WriteLine($"{Name} picks the lock. The lockpick snaps.");
            return true;
        }

        // Specific key path
        if (target.RequiredKeyId != null && key.KeyId == target.RequiredKeyId)
        {
            target.IsLocked = false;
            Console.WriteLine($"{Name} unlocks it with the {key.Name}.");
            return true;
        }

        Console.WriteLine($"The {key.Name} doesn't fit this lock.");
        return false;
    }

    /// <summary>
    /// Moves every item from an opened chest into the player's inventory.
    /// Respects the inventory weight limit - items that don't fit stay in
    /// the chest.
    /// </summary>
    public void LootChest(Chest chest)
    {
        if (Inventory == null || chest.IsLocked)
            return;

        var items = chest.Items.ToList(); // snapshot to avoid mutation-during-enumeration
        foreach (var item in items)
        {
            if (!PickUp(item))
                continue;
            chest.RemoveItem(item);
        }
    }

    // ============================================================
    // MONSTER LOOTING (new in Week 13)
    // ============================================================

    /// <summary>
    /// Transfers items from a defeated monster's MonsterLoot container into
    /// the player's inventory. The monster must be dead and not yet looted.
    /// </summary>
    public void LootMonster(Monster monster)
    {
        if (monster.Health > 0)
        {
            Console.WriteLine($"{monster.Name} is still alive!");
            return;
        }

        if (monster.IsLooted)
        {
            Console.WriteLine($"{monster.Name} has already been looted.");
            return;
        }

        if (monster.Loot == null || !monster.Loot.Items.Any())
        {
            Console.WriteLine($"{monster.Name} had nothing worth taking.");
            monster.IsLooted = true;
            return;
        }

        var items = monster.Loot.Items.ToList();
        int taken = 0;
        foreach (var item in items)
        {
            if (!PickUp(item))
                continue;
            monster.Loot.RemoveItem(item);
            taken++;
        }

        if (taken > 0)
        {
            Console.WriteLine($"{Name} looted {taken} item(s) from {monster.Name}.");
        }

        monster.IsLooted = true;
    }

    // ============================================================
    // MOVEMENT & DOORS (new in Week 14)
    // ============================================================

    /// <summary>
    /// Attempts to pass through a door, applying any trap damage and
    /// respecting the door's lock state.
    ///
    /// Returns true if the player successfully passed through. False means
    /// the door was locked (caller should prompt for a key) or blocked.
    /// Traps fire on the first successful passage and then mark themselves
    /// disarmed so they only hit once.
    /// </summary>
    public bool PassThroughDoor(Door door)
    {
        if (door.IsLocked)
        {
            Console.WriteLine("The way is blocked by a locked door.");
            return false;
        }

        if (door.IsTrapped && !door.TrapDisarmed)
        {
            Health -= door.TrapDamage;
            door.TrapDisarmed = true;
            Console.WriteLine($"A trap! {Name} takes {door.TrapDamage} damage.");
        }

        return true;
    }

    /// <summary>
    /// Inspects the current room to discover any secret doors connected to it.
    /// Returns the list of doors newly revealed. If nothing was hidden, the
    /// list is empty.
    ///
    /// This is the Week 14 stretch goal - integrate this into the GameEngine
    /// "Look" or "Inspect" menu option.
    /// </summary>
    public List<Door> InspectForSecretDoors(IEnumerable<Door> allDoors)
    {
        if (CurrentRoom == null)
            return new List<Door>();

        var roomId = CurrentRoom.Id;
        var discovered = allDoors
            .Where(d => d.IsSecret && !d.IsDiscovered)
            .Where(d => d.RoomAId == roomId || d.RoomBId == roomId)
            .ToList();

        foreach (var door in discovered)
        {
            door.IsDiscovered = true;
            Console.WriteLine($"{Name} discovers a hidden passage: {door.Name}!");
        }

        if (!discovered.Any())
            Console.WriteLine($"{Name} searches the room but finds nothing hidden.");

        return discovered;
    }
}
