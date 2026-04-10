using ConsoleRpg.Models;
using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using ConsoleRpgEntities.Models.World;
using Microsoft.EntityFrameworkCore;

namespace ConsoleRpg.Services;

/// <summary>
/// PlayerService - The exploration-mode "action handler". Wraps the
/// Player entity's operations and returns a ServiceResult so the UI layer
/// doesn't have to reach into game state or worry about exceptions.
///
/// Everything here is a thin wrapper over Player methods that already
/// exist from earlier weeks. The service layer exists to:
///   1. Provide a stable boundary between the UI and the model
///   2. Save changes to the database after each action
///   3. Translate model operations into user-facing messages
/// </summary>
public class PlayerService
{
    private readonly GameContext _context;

    public PlayerService(GameContext context)
    {
        _context = context;
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    /// <summary>
    /// Moves the player to the target room in the given direction.
    /// Handles door traversal: if a door sits between the rooms, lock/trap
    /// checks fire before the move happens.
    /// </summary>
    public ServiceResult<Room> Move(Player player, Room currentRoom, int? targetRoomId, string direction)
    {
        if (targetRoomId == null)
            return ServiceResult<Room>.Fail($"You can't go {direction.ToLower()} from here.");

        // Find the target room with its navigation loaded so the UI can
        // render it immediately after the move.
        var target = _context.Containers
            .OfType<Room>()
            .Include(r => r.Items)
            .Include(r => r.NorthRoom)
            .Include(r => r.SouthRoom)
            .Include(r => r.EastRoom)
            .Include(r => r.WestRoom)
            .FirstOrDefault(r => r.Id == targetRoomId.Value);

        if (target == null)
            return ServiceResult<Room>.Fail("The path leads nowhere.");

        // Door check: is there a door between current and target? If so,
        // apply lock + trap mechanics before allowing the move.
        var door = _context.Doors.FirstOrDefault(d =>
            (d.RoomAId == currentRoom.Id && d.RoomBId == target.Id) ||
            (d.RoomAId == target.Id && d.RoomBId == currentRoom.Id));

        if (door != null)
        {
            if (door.IsSecret && !door.IsDiscovered)
                return ServiceResult<Room>.Fail("You can't go that way.");

            if (door.IsLocked)
                return ServiceResult<Room>.Fail($"The {door.Name} is locked. Find a key or lockpick.");

            if (!player.PassThroughDoor(door))
                return ServiceResult<Room>.Fail("The door blocks your path.");
        }

        player.CurrentRoomId = target.Id;
        player.CurrentRoom = target;
        _context.SaveChanges();

        return ServiceResult<Room>.Ok(target, $"You travel {direction.ToLower()} into {target.Name}.");
    }

    // ============================================================
    // COMBAT
    // ============================================================

    public ServiceResult AttackMonster(Player player, Monster monster)
    {
        if (monster.Health <= 0)
            return ServiceResult.Fail($"{monster.Name} is already defeated.");

        player.Attack(monster);

        string counterAttack = "";
        if (monster.Health > 0)
        {
            int hpBefore = player.Health;
            monster.Attack(player);
            int damage = hpBefore - player.Health;
            counterAttack = $"\n{monster.Name} counter-attacks for {damage} damage!";
        }
        else
        {
            counterAttack = $"\n{monster.Name} has been defeated!";
        }

        _context.SaveChanges();
        return ServiceResult.Ok(
            $"You attack {monster.Name}.",
            $"You attack {monster.Name} for {player.GetTotalAttack()} damage!{counterAttack}");
    }

    public ServiceResult LootMonster(Player player, Monster monster)
    {
        player.LootMonster(monster);
        _context.SaveChanges();
        return ServiceResult.Ok($"Looted {monster.Name}.");
    }

    // ============================================================
    // INVENTORY
    // ============================================================

    public ServiceResult PickUpFromFloor(Player player, Item item)
    {
        if (player.PickUpFromFloor(item))
        {
            _context.SaveChanges();
            return ServiceResult.Ok($"Picked up {item.Name}.");
        }
        return ServiceResult.Fail($"You can't pick up {item.Name}.");
    }

    public ServiceResult Drop(Player player, Item item)
    {
        player.Drop(item);
        _context.SaveChanges();
        return ServiceResult.Ok($"Dropped {item.Name}.");
    }

    public ServiceResult UseConsumable(Player player, Consumable consumable)
    {
        int hpBefore = player.Health;
        player.UseItem(consumable);
        _context.SaveChanges();
        int restored = player.Health - hpBefore;
        return ServiceResult.Ok(
            $"Used {consumable.Name}.",
            restored > 0
                ? $"You drink {consumable.Name} and recover {restored} HP."
                : $"You use {consumable.Name}.");
    }

    public ServiceResult Equip(Player player, Item item)
    {
        player.Equip(item);
        _context.SaveChanges();
        return ServiceResult.Ok($"Equipped {item.Name}.");
    }

    // ============================================================
    // ROOM INSPECTION
    // ============================================================

    public ServiceResult InspectRoom(Player player)
    {
        var doors = _context.Doors.ToList();
        var discovered = player.InspectForSecretDoors(doors);
        if (discovered.Any())
        {
            _context.SaveChanges();
            return ServiceResult.Ok(
                $"You found {discovered.Count} hidden passage(s)!",
                $"Secret doors revealed: {string.Join(", ", discovered.Select(d => d.Name))}");
        }
        return ServiceResult.Ok("You search carefully but find nothing hidden.");
    }
}
