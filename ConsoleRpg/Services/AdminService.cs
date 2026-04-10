using ConsoleRpg.Models;
using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace ConsoleRpg.Services;

/// <summary>
/// AdminService - The admin/developer mode CRUD layer.
///
/// Each public method maps to a menu option in the admin mode. The methods
/// are grouped by rubric tier (Base / B / A) so students can see where each
/// feature fits. READ the rubric section of the README to see which are
/// pre-built reference implementations and which you are asked to extend.
///
/// =========================================================
/// PEDAGOGY NOTE
/// =========================================================
/// These methods are intentionally simple wrappers around LINQ queries and
/// basic EF Core operations. They're REFERENCE implementations. The idea is:
///
///   1. Run the game and use each feature to understand what it does
///   2. Open this file and read how it's implemented - it's just LINQ
///   3. Use these as templates for your own features in the A-level tasks
///
/// You do NOT need to rewrite any of these. Your A-level work is ADDING
/// new methods that use similar patterns to answer new questions about
/// your world.
/// </summary>
public class AdminService
{
    private readonly GameContext _context;

    public AdminService(GameContext context)
    {
        _context = context;
    }

    // ==================================================================
    // BASE TIER (from earlier weeks, polished up here)
    // ==================================================================

    /// <summary>
    /// Prompt-driven "add new character" flow. Students were asked to
    /// build something like this in earlier weeks; this version is the
    /// reference implementation.
    /// </summary>
    public ServiceResult AddCharacter()
    {
        var name = AnsiConsole.Ask<string>("Character [yellow]name[/]:");
        var level = AnsiConsole.Ask<int>("Starting [yellow]level[/]:", 1);

        // New players get their own Inventory and Equipment containers.
        var inventory = new Inventory { MaxWeight = 100 };
        var equipment = new Equipment();
        _context.Containers.Add(inventory);
        _context.Containers.Add(equipment);
        _context.SaveChanges();

        var player = new Player
        {
            Name = name,
            Level = level,
            Health = 100,
            InventoryId = inventory.Id,
            EquipmentId = equipment.Id
        };
        _context.Players.Add(player);
        _context.SaveChanges();

        return ServiceResult.Ok($"Created character '{name}'.");
    }

    /// <summary>
    /// Lists every player in the database with basic stats.
    /// Demonstrates a simple LINQ query with OrderBy.
    /// </summary>
    public ServiceResult DisplayAllCharacters()
    {
        // Eager-load the Room so we can display each character's location.
        var players = _context.Players
            .Include(p => p.CurrentRoom)
            .OrderBy(p => p.Name)
            .ToList();

        if (!players.Any())
            return ServiceResult.Ok("No characters found.");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Name[/]")
            .AddColumn("[yellow]Level[/]")
            .AddColumn("[yellow]HP[/]")
            .AddColumn("[yellow]Location[/]");

        foreach (var p in players)
        {
            table.AddRow(
                Markup.Escape(p.Name),
                p.Level.ToString(),
                p.Health.ToString(),
                Markup.Escape(p.CurrentRoom?.Name ?? "(nowhere)"));
        }

        AnsiConsole.Write(table);
        return ServiceResult.Ok($"Displayed {players.Count} character(s).");
    }

    /// <summary>
    /// Prompts for a search term and uses LINQ Contains() to find players.
    /// Demonstrates case-insensitive string matching.
    /// </summary>
    public ServiceResult SearchCharacterByName()
    {
        var term = AnsiConsole.Ask<string>("Search [yellow]name[/] (partial OK):");

        var matches = _context.Players
            .Where(p => EF.Functions.Like(p.Name, $"%{term}%"))
            .OrderBy(p => p.Name)
            .ToList();

        if (!matches.Any())
            return ServiceResult.Ok($"No characters matching '{term}'.");

        foreach (var p in matches)
        {
            AnsiConsole.MarkupLine($"  [yellow]{Markup.Escape(p.Name)}[/] (Lvl {p.Level}, HP {p.Health})");
        }
        return ServiceResult.Ok($"Found {matches.Count} match(es).");
    }

    // ==================================================================
    // B TIER
    // ==================================================================

    /// <summary>
    /// Displays details for a specific room including the items on the
    /// floor. Uses .Include() for eager loading of the room's Items collection.
    /// </summary>
    public ServiceResult DisplayRoomDetails()
    {
        var rooms = _context.Containers
            .OfType<Room>()
            .Include(r => r.Items)
            .OrderBy(r => r.Name)
            .ToList();

        if (!rooms.Any())
            return ServiceResult.Ok("No rooms found.");

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Which room?[/]")
                .AddChoices(rooms.Select(r => r.Name)));

        var room = rooms.First(r => r.Name == pick);

        AnsiConsole.MarkupLine($"\n[yellow bold]{Markup.Escape(room.Name)}[/]");
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(room.Description)}[/]\n");

        var monsters = _context.Monsters.Where(m => m.CurrentRoomId == room.Id).ToList();
        AnsiConsole.MarkupLine($"[red]Monsters:[/] {(monsters.Any() ? string.Join(", ", monsters.Select(m => Markup.Escape(m.Name))) : "[dim]none[/]")}");
        AnsiConsole.MarkupLine($"[yellow]Items on the floor:[/] {(room.Items.Any() ? string.Join(", ", room.Items.Select(i => Markup.Escape(i.Name))) : "[dim]none[/]")}");

        return ServiceResult.Ok($"Displayed details for {room.Name}.");
    }

    /// <summary>
    /// Lists every room and the count of monsters in each.
    /// Demonstrates LINQ GroupJoin via subquery on a TPH hierarchy.
    /// </summary>
    public ServiceResult ListAllRoomsWithMonsters()
    {
        var rooms = _context.Containers
            .OfType<Room>()
            .OrderBy(r => r.Name)
            .ToList();

        // Second query for monsters - simpler than a join for teaching.
        var monsterCounts = _context.Monsters
            .Where(m => m.CurrentRoomId.HasValue)
            .GroupBy(m => m.CurrentRoomId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Room[/]")
            .AddColumn("[yellow]Monsters[/]")
            .AddColumn("[yellow]Items[/]");

        foreach (var r in rooms)
        {
            var mc = monsterCounts.TryGetValue(r.Id, out var c) ? c : 0;
            table.AddRow(Markup.Escape(r.Name), mc.ToString(), r.Items.Count.ToString());
        }

        AnsiConsole.Write(table);
        return ServiceResult.Ok($"Listed {rooms.Count} rooms.");
    }

    // ==================================================================
    // A TIER
    // ==================================================================

    /// <summary>
    /// Demonstrates a LINQ query across THREE tables (Items -> Container ->
    /// either Room or a subclass) to answer: "where in the world is this
    /// specific item right now?"
    /// </summary>
    public ServiceResult FindItemLocation()
    {
        var term = AnsiConsole.Ask<string>("Search [yellow]item name[/] (partial OK):");

        // One query with Include - LINQ walks from Item to its Container
        // (which might be an Inventory, Chest, Room, etc. thanks to TPH).
        var items = _context.Items
            .Include(i => i.Container)
            .Where(i => EF.Functions.Like(i.Name, $"%{term}%"))
            .ToList();

        if (!items.Any())
            return ServiceResult.Ok($"No items matching '{term}'.");

        foreach (var item in items)
        {
            string location = item.Container?.ContainerType switch
            {
                "Room" => $"on the floor of {((Room)item.Container).Name}",
                "Chest" => $"inside chest \"{((Chest)item.Container).Description}\"",
                "Inventory" => "in a player's backpack",
                "Equipment" => "equipped by a player",
                "MonsterLoot" => "on a monster's corpse",
                _ => "(unknown)"
            };
            AnsiConsole.MarkupLine($"  [yellow]{Markup.Escape(item.Name)}[/] [dim]->[/] {Markup.Escape(location)}");
        }

        return ServiceResult.Ok($"Found {items.Count} match(es).");
    }

    /// <summary>
    /// Adds an existing ability to a character. Demonstrates many-to-many
    /// insertion by adding to the navigation collection.
    /// </summary>
    public ServiceResult AddAbilityToCharacter()
    {
        var players = _context.Players.Include(p => p.Abilities).OrderBy(p => p.Name).ToList();
        if (!players.Any())
            return ServiceResult.Fail("No characters to modify.");

        var playerPick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Which character?[/]")
                .AddChoices(players.Select(p => p.Name)));
        var player = players.First(p => p.Name == playerPick);

        var abilities = _context.Abilities.OrderBy(a => a.Name).ToList();
        if (!abilities.Any())
            return ServiceResult.Fail("No abilities exist in the database.");

        var abilityPick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]Which ability to teach {Markup.Escape(player.Name)}?[/]")
                .AddChoices(abilities.Select(a => a.Name)));
        var ability = abilities.First(a => a.Name == abilityPick);

        if (player.Abilities.Any(a => a.Id == ability.Id))
            return ServiceResult.Fail($"{player.Name} already knows {ability.Name}.");

        player.Abilities.Add(ability);
        _context.SaveChanges();

        return ServiceResult.Ok($"{player.Name} learned {ability.Name}.");
    }

    /// <summary>
    /// Displays a character's abilities. Demonstrates eager loading of a
    /// many-to-many navigation collection.
    /// </summary>
    public ServiceResult DisplayCharacterAbilities()
    {
        var players = _context.Players.Include(p => p.Abilities).OrderBy(p => p.Name).ToList();
        if (!players.Any())
            return ServiceResult.Fail("No characters found.");

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Which character?[/]")
                .AddChoices(players.Select(p => p.Name)));
        var player = players.First(p => p.Name == pick);

        if (!player.Abilities.Any())
            return ServiceResult.Ok($"{player.Name} knows no abilities.");

        AnsiConsole.MarkupLine($"\n[yellow bold]{Markup.Escape(player.Name)}[/]'s abilities:");
        foreach (var a in player.Abilities)
            AnsiConsole.MarkupLine($"  - [cyan]{Markup.Escape(a.Name)}[/] ([dim]{Markup.Escape(a.Description)}[/])");

        return ServiceResult.Ok($"{player.Abilities.Count} abilities displayed.");
    }

    /// <summary>
    /// Lists monsters grouped by type, showing count and total HP per type.
    /// Demonstrates LINQ GroupBy with aggregates.
    /// </summary>
    public ServiceResult MonsterCensus()
    {
        var groups = _context.Monsters
            .GroupBy(m => m.MonsterType)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count(),
                TotalHp = g.Sum(m => m.Health),
                Alive = g.Count(m => m.Health > 0)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (!groups.Any())
            return ServiceResult.Ok("No monsters in the database.");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Type[/]")
            .AddColumn("[yellow]Total[/]")
            .AddColumn("[yellow]Alive[/]")
            .AddColumn("[yellow]Total HP[/]");

        foreach (var g in groups)
        {
            table.AddRow(Markup.Escape(g.Type), g.Count.ToString(), g.Alive.ToString(), g.TotalHp.ToString());
        }

        AnsiConsole.Write(table);
        return ServiceResult.Ok($"{groups.Count} monster type(s) found.");
    }
}
