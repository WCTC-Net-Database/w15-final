using ConsoleRpg.Helpers;
using ConsoleRpg.Models;
using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using ConsoleRpgEntities.Models.World;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ConsoleRpg.Services;

/// <summary>
/// GameEngine - The Week 15 final-project entry point.
///
/// =========================================================
/// DUAL MODE DESIGN
/// =========================================================
/// The engine runs in one of two modes:
///
///   EXPLORATION  - the player walks around the world, fights monsters,
///                  picks up items, opens chests. Uses ExplorationUI to
///                  render a split-panel Spectre layout and PlayerService
///                  to execute actions.
///
///   ADMIN        - the developer menu for CRUD operations. Uses AdminService
///                  to add characters, inspect rooms, run LINQ queries, etc.
///
/// Switching modes is a single menu entry in each.
///
/// =========================================================
/// WHY SEPARATE THE SERVICES?
/// =========================================================
/// GameEngine is deliberately tiny - it's a dispatcher. It doesn't know
/// how to add a character, how to move, how to attack. It delegates all
/// of that to PlayerService / AdminService / ExplorationUI. That's the
/// Single Responsibility Principle: GameEngine orchestrates, services do
/// the actual work.
/// </summary>
public class GameEngine
{
    private readonly GameContext _context;
    private readonly PlayerService _playerService;
    private readonly AdminService _adminService;
    private readonly ExplorationUI _explorationUI;
    private readonly ILogger<GameEngine> _logger;

    private Player? _player;
    private List<Monster> _monsters = new();
    private List<Room> _rooms = new();
    private List<Chest> _chests = new();

    private GameMode _mode = GameMode.Exploration;
    private bool _playerDead;

    // A sentinel value used by sub-prompts so the player can back out
    // of a SelectionPrompt without being forced to pick an item.
    private const string CancelLabel = "(Cancel)";

    public GameEngine(
        GameContext context,
        PlayerService playerService,
        AdminService adminService,
        ExplorationUI explorationUI,
        ILogger<GameEngine> logger)
    {
        _context = context;
        _playerService = playerService;
        _adminService = adminService;
        _explorationUI = explorationUI;
        _logger = logger;
    }

    public void Run()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("ConsoleRPG").Centered().Color(Color.Yellow));
        AnsiConsole.Write(new Rule("[dim]Week 15 - Final Project[/]").Centered());
        AnsiConsole.WriteLine();

        LoadWorld();

        if (_player == null)
        {
            AnsiConsole.MarkupLine("[red]No player found. Run 'dotnet ef database update' first.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Welcome, {Markup.Escape(_player.Name)}![/]");
        AnsiConsole.MarkupLine("[dim]Press any key to begin...[/]");
        Console.ReadKey(true);

        // Main loop - runs until the player quits or dies.
        while (true)
        {
            // Reload cached data between turns so we see DB changes.
            ReloadGameState();

            // Death check - if the player died last turn, show a game-over
            // screen and exit the loop.
            if (_playerDead || (_player?.Health ?? 0) <= 0)
            {
                ShowGameOver();
                return;
            }

            if (_mode == GameMode.Exploration)
            {
                if (ExplorationTurn() == TurnResult.Quit) return;
            }
            else
            {
                if (AdminTurn() == TurnResult.Quit) return;
            }
        }
    }

    /// <summary>
    /// Renders a simple Spectre game-over screen when the player's HP hits zero.
    /// Students can extend this - add a respawn flow, a retry prompt, a final
    /// score display, etc.
    /// </summary>
    private void ShowGameOver()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Game Over").Centered().Color(Color.Red));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(_player?.Name ?? "The adventurer")} has fallen in battle.[/]");
        AnsiConsole.MarkupLine("[dim]Run 'dotnet ef database update 0' and 'dotnet ef database update' to start fresh.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
        Console.ReadKey(true);
    }

    // ================================================================
    // DATA LOADING
    // ================================================================

    private void LoadWorld()
    {
        _player = _context.Players
            .Include(p => p.Inventory!).ThenInclude(i => i.Items)
            .Include(p => p.Equipment!).ThenInclude(e => e.Items)
            .Include(p => p.CurrentRoom!).ThenInclude(r => r.Items)
            .Include(p => p.Abilities)
            .FirstOrDefault();

        _monsters = _context.Monsters
            .Include(m => m.Loot!).ThenInclude(l => l.Items)
            .ToList();

        _rooms = _context.Containers
            .OfType<Room>()
            .Include(r => r.Items)
            .Include(r => r.NorthRoom)
            .Include(r => r.SouthRoom)
            .Include(r => r.EastRoom)
            .Include(r => r.WestRoom)
            .ToList();

        // Chests placed in the world (via Chest.LocationRoomId from W15).
        _chests = _context.Containers
            .OfType<Chest>()
            .Include(c => c.Items)
            .Where(c => c.LocationRoomId.HasValue)
            .ToList();
    }

    /// <summary>
    /// Refreshes the in-memory entities from the database. Called between
    /// turns so that after an action saves changes we can see the new state.
    /// </summary>
    private void ReloadGameState()
    {
        // Rather than try to track a bunch of individual reloads, we just
        // re-run the initial load queries. It's a small world and this
        // keeps the code straightforward. For a larger game you'd manage
        // dirty state more carefully.
        LoadWorld();
    }

    // ================================================================
    // EXPLORATION MODE
    // ================================================================

    private TurnResult ExplorationTurn()
    {
        if (_player == null) return TurnResult.Quit;

        // Filter the rooms list so the map only shows what the player has
        // discovered. Rooms behind undiscovered secret doors stay hidden
        // until Inspect Room reveals them - the reveal moment is the whole
        // point of secret doors.
        var visibleIds = _playerService.GetVisibleRoomIds(_player.CurrentRoomId);
        var visibleRooms = _rooms.Where(r => visibleIds.Contains(r.Id)).ToList();

        var choice = _explorationUI.RenderAndPrompt(_player, _player.CurrentRoom, visibleRooms, _monsters, _chests);

        switch (choice)
        {
            case "Go North": HandleMove(_player.CurrentRoom?.NorthRoomId, "North"); break;
            case "Go South": HandleMove(_player.CurrentRoom?.SouthRoomId, "South"); break;
            case "Go East":  HandleMove(_player.CurrentRoom?.EastRoomId,  "East"); break;
            case "Go West":  HandleMove(_player.CurrentRoom?.WestRoomId,  "West"); break;

            case "Attack Monster": HandleAttack(); break;
            case "Pick Up Item":   HandlePickUp(); break;
            case "Drop Item":      HandleDrop(); break;
            case "Equip Item":     HandleEquip(); break;
            case "Unequip Item":   HandleUnequip(); break;
            case "Use Consumable": HandleUseConsumable(); break;
            case "Open Chest":     HandleChest(); break;
            case "View Inventory": HandleViewInventory(); break;
            case "Inspect Room":
                var r = _playerService.InspectRoom(_player);
                _explorationUI.ShowMessage(r.DetailedOutput, r.Success ? ConsoleColor.Green : ConsoleColor.Yellow);
                break;

            case "Switch to Admin Mode":
                _mode = GameMode.Admin;
                break;

            case "Quit":
                AnsiConsole.MarkupLine("[yellow]Farewell, adventurer.[/]");
                return TurnResult.Quit;
        }

        return TurnResult.Continue;
    }

    private void HandleMove(int? targetId, string direction)
    {
        if (_player?.CurrentRoom == null) return;

        // Check for a door between here and the target. If the door is locked,
        // give the player a chance to unlock it BEFORE calling Move.
        var door = _playerService.FindDoorBetween(_player.CurrentRoom.Id, targetId);
        if (door != null && door.IsLocked)
        {
            if (!PromptUnlockDoor(door))
            {
                // Player cancelled or failed to unlock; don't attempt the move
                return;
            }
        }

        var result = _playerService.Move(_player, _player.CurrentRoom, targetId, direction);
        _explorationUI.ShowMessage(result.DetailedOutput, result.Success ? ConsoleColor.Green : ConsoleColor.Red);
    }

    /// <summary>
    /// Prompts the player to pick a key from their inventory and attempts
    /// to unlock the given door. Returns true if the door is now unlocked
    /// (whether freshly or because it was already unlocked), false if the
    /// player cancelled or the unlock attempt failed.
    /// </summary>
    private bool PromptUnlockDoor(Door door)
    {
        if (_player?.Inventory == null) return false;

        var keys = _player.Inventory.Items.OfType<KeyItem>().ToList();
        if (!keys.Any())
        {
            _explorationUI.ShowMessage($"The {door.Name} is locked and you have no keys or lockpicks.", ConsoleColor.Red);
            return false;
        }

        var labels = keys
            .Select(k => k.KeyId == null
                ? $"{k.Name} (lockpick)"
                : $"{k.Name} (key: {k.KeyId})")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]The {Markup.Escape(door.Name)} is locked. Try which key?[/]")
                .AddChoices(labels.Append(CancelLabel)));

        if (pick == CancelLabel) return false;

        var chosenIndex = labels.IndexOf(pick);
        var key = keys[chosenIndex];

        var result = _playerService.TryUnlockDoor(_player, door, key);
        _explorationUI.ShowMessage(result.DetailedOutput, result.Success ? ConsoleColor.Green : ConsoleColor.Red);
        return result.Success;
    }

    private void HandleAttack()
    {
        if (_player?.CurrentRoom == null) return;
        var monstersHere = _monsters.Where(m => m.CurrentRoomId == _player.CurrentRoom.Id && m.Health > 0).ToList();
        if (!monstersHere.Any())
        {
            _explorationUI.ShowMessage("No monsters here to fight.", ConsoleColor.Yellow);
            return;
        }

        // Single target: no prompt needed. Multiple: let the player pick,
        // and always offer a Cancel escape hatch so they can back out.
        Monster target;
        if (monstersHere.Count == 1)
        {
            target = monstersHere[0];
        }
        else
        {
            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Attack which monster?[/]")
                    .AddChoices(monstersHere.Select(x => x.Name).Append(CancelLabel)));
            if (pick == CancelLabel) return;
            target = monstersHere.First(m => m.Name == pick);
        }

        var result = _playerService.AttackMonster(_player, target);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Red);

        // Check for player death from the counterattack
        if (_player.Health <= 0)
        {
            _playerDead = true;
            return;
        }

        if (target.Health <= 0 && !target.IsLooted && target.Loot != null && target.Loot.Items.Any())
        {
            if (AnsiConsole.Confirm($"Loot the {target.Name}?"))
            {
                var loot = _playerService.LootMonster(_player, target);
                _explorationUI.ShowMessage(loot.DetailedOutput, ConsoleColor.Green);
            }
        }
    }

    private void HandlePickUp()
    {
        if (_player?.CurrentRoom == null) return;
        var floorItems = _player.CurrentRoom.Items.ToList();
        if (!floorItems.Any())
        {
            _explorationUI.ShowMessage("Nothing on the floor.", ConsoleColor.Yellow);
            return;
        }

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Pick up which item?[/]")
                .AddChoices(floorItems.Select(i => i.Name).Append(CancelLabel)));
        if (pick == CancelLabel) return;
        var item = floorItems.First(i => i.Name == pick);

        var result = _playerService.PickUpFromFloor(_player, item);
        _explorationUI.ShowMessage(result.DetailedOutput, result.Success ? ConsoleColor.Green : ConsoleColor.Yellow);
    }

    private void HandleDrop()
    {
        if (_player?.Inventory == null) return;
        var bagItems = _player.Inventory.Items.ToList();
        if (!bagItems.Any())
        {
            _explorationUI.ShowMessage("Your backpack is empty.", ConsoleColor.Yellow);
            return;
        }

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Drop which item?[/]")
                .AddChoices(bagItems.Select(i => i.Name).Append(CancelLabel)));
        if (pick == CancelLabel) return;
        var item = bagItems.First(i => i.Name == pick);

        var result = _playerService.Drop(_player, item);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Green);
    }

    private void HandleEquip()
    {
        if (_player?.Inventory == null) return;
        var equippable = _player.Inventory.Items
            .Where(i => i is Weapon || i is Armor)
            .ToList();
        if (!equippable.Any())
        {
            _explorationUI.ShowMessage("Nothing in your backpack can be equipped.", ConsoleColor.Yellow);
            return;
        }

        var labels = equippable.Select(i => $"{i.Name} ({i.ItemType})").ToList();
        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Equip which item?[/]")
                .AddChoices(labels.Append(CancelLabel)));
        if (pick == CancelLabel) return;

        var chosen = equippable[labels.IndexOf(pick)];
        var result = _playerService.Equip(_player, chosen);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Green);
    }

    private void HandleUnequip()
    {
        if (_player?.Equipment == null) return;
        var equipped = _player.Equipment.Items.ToList();
        if (!equipped.Any())
        {
            _explorationUI.ShowMessage("Nothing is currently equipped.", ConsoleColor.Yellow);
            return;
        }

        var labels = equipped.Select(i => $"{i.Name} ({i.ItemType})").ToList();
        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Unequip which item?[/]")
                .AddChoices(labels.Append(CancelLabel)));
        if (pick == CancelLabel) return;

        var chosen = equipped[labels.IndexOf(pick)];
        var result = _playerService.Unequip(_player, chosen);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Green);
    }

    private void HandleUseConsumable()
    {
        if (_player?.Inventory == null) return;
        var consumables = _player.Inventory.Items.OfType<Consumable>().ToList();
        if (!consumables.Any())
        {
            _explorationUI.ShowMessage("No consumables in your backpack.", ConsoleColor.Yellow);
            return;
        }

        var labels = consumables.Select(c => $"{c.Name} ({c.EffectType} {c.EffectAmount})").ToList();
        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Use which consumable?[/]")
                .AddChoices(labels.Append(CancelLabel)));
        if (pick == CancelLabel) return;
        var chosen = consumables.First(c => $"{c.Name} ({c.EffectType} {c.EffectAmount})" == pick);

        var result = _playerService.UseConsumable(_player, chosen);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Green);
    }

    /// <summary>
    /// Full inventory sub-screen. The exploration view shows only a compact
    /// summary in the Character panel (to keep the layout from scrolling),
    /// so this gives the player a way to see the per-item list on demand.
    /// </summary>
    private void HandleViewInventory()
    {
        if (_player == null) return;

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[yellow bold]{Markup.Escape(_player.Name)}'s Inventory[/]").Centered());
        AnsiConsole.WriteLine();

        var equipped = _player.Equipment?.Items.ToList() ?? new();
        var bag = _player.Inventory?.Items.ToList() ?? new();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Where[/]")
            .AddColumn("[yellow]Name[/]")
            .AddColumn("[yellow]Type[/]")
            .AddColumn("[yellow]Weight[/]");

        foreach (var item in equipped)
            table.AddRow("[green]equipped[/]", Markup.Escape(item.Name), item.ItemType, item.Weight.ToString());

        foreach (var item in bag)
            table.AddRow("[dim]bag[/]", Markup.Escape(item.Name), item.ItemType, item.Weight.ToString());

        if (!equipped.Any() && !bag.Any())
            AnsiConsole.MarkupLine("[dim]You are carrying nothing.[/]");
        else
            AnsiConsole.Write(table);

        var bagMax = _player.Inventory?.MaxWeight ?? 0;
        AnsiConsole.MarkupLine($"\n[dim]Carrying {_player.GetCurrentWeight()}/{bagMax} lbs[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    // ================================================================
    // CHEST INTERACTION
    // ================================================================
    // Uses the W13 chest mechanics (Player.OpenChest / TryUnlock / LootChest)
    // combined with the W15 Chest.LocationRoomId so chests in the current
    // room appear as a menu option.
    private void HandleChest()
    {
        if (_player?.CurrentRoom == null) return;

        var chestsHere = _chests.Where(c => c.LocationRoomId == _player.CurrentRoom.Id).ToList();
        if (!chestsHere.Any())
        {
            _explorationUI.ShowMessage("There are no chests here.", ConsoleColor.Yellow);
            return;
        }

        Chest chest;
        if (chestsHere.Count == 1)
        {
            chest = chestsHere[0];
        }
        else
        {
            var labels = chestsHere.Select(c =>
            {
                var status = c.IsLocked ? "(locked)" : c.Items.Any() ? "(open)" : "(empty)";
                return $"{c.Description} {status}";
            }).ToList();
            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Which chest?[/]")
                    .AddChoices(labels.Append(CancelLabel)));
            if (pick == CancelLabel) return;
            chest = chestsHere[labels.IndexOf(pick)];
        }

        // If locked, offer to unlock before opening
        if (chest.IsLocked)
        {
            if (!PromptUnlockChest(chest)) return;
        }

        // Try to open - handles trap firing
        var openResult = _player.OpenChest(chest);
        _context.SaveChanges();

        switch (openResult)
        {
            case Player.OpenResult.Trapped:
                _explorationUI.ShowMessage(
                    $"A trap springs! You take {chest.TrapDamage} damage. The chest creaks open.",
                    ConsoleColor.Red);
                break;
            case Player.OpenResult.Opened:
            case Player.OpenResult.AlreadyOpen:
                // fall through to loot prompt
                break;
            case Player.OpenResult.Locked:
                // Shouldn't happen - we unlocked above
                _explorationUI.ShowMessage($"The {chest.Description} is still locked.", ConsoleColor.Red);
                return;
        }

        // Now the chest is open - offer to take items
        if (!chest.Items.Any())
        {
            _explorationUI.ShowMessage("The chest is empty.", ConsoleColor.Yellow);
            return;
        }

        var itemList = string.Join(", ", chest.Items.Select(i => i.Name));
        if (AnsiConsole.Confirm($"Chest contains: {itemList}. Take all?"))
        {
            _player.LootChest(chest);
            _context.SaveChanges();
            _explorationUI.ShowMessage("You loot the chest.", ConsoleColor.Green);
        }
    }

    /// <summary>
    /// Prompts the player to try a key from inventory on a locked chest.
    /// Same pattern as PromptUnlockDoor - both call Player.TryUnlock via
    /// the ILockable interface from W13.
    /// </summary>
    private bool PromptUnlockChest(Chest chest)
    {
        if (_player?.Inventory == null) return false;

        var keys = _player.Inventory.Items.OfType<KeyItem>().ToList();
        if (!keys.Any())
        {
            _explorationUI.ShowMessage($"The {chest.Description} is locked and you have no keys.", ConsoleColor.Red);
            return false;
        }

        var labels = keys
            .Select(k => k.KeyId == null
                ? $"{k.Name} (lockpick)"
                : $"{k.Name} (key: {k.KeyId})")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]The chest is locked. Try which key?[/]")
                .AddChoices(labels.Append(CancelLabel)));

        if (pick == CancelLabel) return false;

        var key = keys[labels.IndexOf(pick)];
        var success = _player.TryUnlock(chest, key);
        _context.SaveChanges();

        if (success)
        {
            _explorationUI.ShowMessage($"You unlock the {chest.Description}.", ConsoleColor.Green);
        }
        else
        {
            _explorationUI.ShowMessage($"The {key.Name} doesn't fit.", ConsoleColor.Red);
        }
        return success;
    }

    // ================================================================
    // ADMIN MODE
    // ================================================================

    private TurnResult AdminTurn()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow bold]Admin Mode[/]").Centered());
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Select an admin action[/]")
                .PageSize(15)
                .AddChoices(
                    // Base tier
                    "Add Character",
                    "Display All Characters",
                    "Search Character by Name",

                    // B tier
                    "Display Room Details",
                    "List All Rooms with Monsters",

                    // A tier
                    "Add Ability to Character",
                    "Display Character Abilities",
                    "Find Item Location (LINQ)",
                    "Monster Census (LINQ GroupBy)",

                    // Stretch goal - self-contained, no dependency on game state
                    "Parser Demo (Stretch Goal)",

                    // Navigation
                    "Return to Exploration Mode",
                    "Quit"));

        ServiceResult? result = choice switch
        {
            "Add Character" => _adminService.AddCharacter(),
            "Display All Characters" => _adminService.DisplayAllCharacters(),
            "Search Character by Name" => _adminService.SearchCharacterByName(),
            "Display Room Details" => _adminService.DisplayRoomDetails(),
            "List All Rooms with Monsters" => _adminService.ListAllRoomsWithMonsters(),
            "Add Ability to Character" => _adminService.AddAbilityToCharacter(),
            "Display Character Abilities" => _adminService.DisplayCharacterAbilities(),
            "Find Item Location (LINQ)" => _adminService.FindItemLocation(),
            "Monster Census (LINQ GroupBy)" => _adminService.MonsterCensus(),
            "Parser Demo (Stretch Goal)" => RunParserDemo(),
            _ => null
        };

        if (result != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(result.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
            return TurnResult.Continue;
        }

        if (choice == "Return to Exploration Mode")
        {
            _mode = GameMode.Exploration;
            return TurnResult.Continue;
        }

        // Quit
        return TurnResult.Quit;
    }

    /// <summary>
    /// Launches the self-contained parser demo (see ParserDemo/ParserDemo.cs).
    /// Returns a ServiceResult so it slots into the admin dispatch table the
    /// same way every other admin action does. The demo has its own REPL and
    /// its own mock world - nothing it does affects the real game state.
    /// </summary>
    private ServiceResult RunParserDemo()
    {
        new ParserDemo.ParserDemo().Run();
        return ServiceResult.Ok("Parser demo finished.");
    }

    private enum TurnResult { Continue, Quit }
}

public enum GameMode
{
    Exploration,
    Admin
}
