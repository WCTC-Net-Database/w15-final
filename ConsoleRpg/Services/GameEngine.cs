using ConsoleRpg.Helpers;
using ConsoleRpg.Models;
using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
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

    private GameMode _mode = GameMode.Exploration;

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

        // Main loop - runs until the player quits.
        while (true)
        {
            // Reload cached data between turns so we see DB changes.
            ReloadGameState();

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

        var choice = _explorationUI.RenderAndPrompt(_player, _player.CurrentRoom, _rooms, _monsters);

        switch (choice)
        {
            case "Go North": HandleMove(_player.CurrentRoom?.NorthRoomId, "North"); break;
            case "Go South": HandleMove(_player.CurrentRoom?.SouthRoomId, "South"); break;
            case "Go East":  HandleMove(_player.CurrentRoom?.EastRoomId,  "East"); break;
            case "Go West":  HandleMove(_player.CurrentRoom?.WestRoomId,  "West"); break;

            case "Attack Monster": HandleAttack(); break;
            case "Pick Up Item":   HandlePickUp(); break;
            case "Drop Item":      HandleDrop(); break;
            case "Use Consumable": HandleUseConsumable(); break;
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
        var result = _playerService.Move(_player, _player.CurrentRoom, targetId, direction);
        _explorationUI.ShowMessage(result.DetailedOutput, result.Success ? ConsoleColor.Green : ConsoleColor.Red);
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

        var target = monstersHere.Count == 1
            ? monstersHere[0]
            : monstersHere.First(m => m.Name == AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Attack which monster?[/]")
                    .AddChoices(monstersHere.Select(x => x.Name))));

        var result = _playerService.AttackMonster(_player, target);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Red);

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
                .AddChoices(floorItems.Select(i => i.Name)));
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
                .AddChoices(bagItems.Select(i => i.Name)));
        var item = bagItems.First(i => i.Name == pick);

        var result = _playerService.Drop(_player, item);
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

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Use which consumable?[/]")
                .AddChoices(consumables.Select(c => $"{c.Name} ({c.EffectType} {c.EffectAmount})")));
        var chosen = consumables.First(c => $"{c.Name} ({c.EffectType} {c.EffectAmount})" == pick);

        var result = _playerService.UseConsumable(_player, chosen);
        _explorationUI.ShowMessage(result.DetailedOutput, ConsoleColor.Green);
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

    private enum TurnResult { Continue, Quit }
}

public enum GameMode
{
    Exploration,
    Admin
}
