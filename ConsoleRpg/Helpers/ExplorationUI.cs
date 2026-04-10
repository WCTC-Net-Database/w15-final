using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using Spectre.Console;

namespace ConsoleRpg.Helpers;

/// <summary>
/// ExplorationUI - Draws the exploration-mode layout: map on the left,
/// current-room details and character stats stacked on the right, and
/// an action prompt at the bottom.
///
/// Uses Spectre.Console's Grid widget for the 2-column layout. Each cell
/// is a Panel built by the MapManager helper.
///
/// This is a thin presentation layer - it knows NOTHING about the database
/// or game state. The GameEngine hands it the data to render and asks it
/// "what did the player choose?" The separation keeps the game loop clean.
/// </summary>
public class ExplorationUI
{
    private readonly MapManager _mapManager;

    public ExplorationUI(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// Renders the full exploration view (map + room details + character)
    /// and prompts the player to pick an action. Returns the selected action
    /// label for the GameEngine to dispatch.
    /// </summary>
    public string RenderAndPrompt(
        Player player,
        Room? currentRoom,
        IEnumerable<Room> allRooms,
        IEnumerable<Monster> allMonsters)
    {
        AnsiConsole.Clear();

        // Top-level rule banner
        AnsiConsole.Write(new Rule("[yellow bold]ConsoleRPG - Exploration Mode[/]").Centered());
        AnsiConsole.WriteLine();

        // Two-column grid: map on the left, details + stats on the right
        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        var mapPanel = _mapManager.BuildMapPanel(allRooms, currentRoom, allMonsters);
        var roomPanel = _mapManager.BuildRoomDetailsPanel(currentRoom, allMonsters);
        var playerPanel = _mapManager.BuildPlayerPanel(player);

        // Stack the room details and player panels in the right column.
        var rightColumn = new Rows(roomPanel, playerPanel);

        grid.AddRow(mapPanel, rightColumn);
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        // Build the list of available actions based on current context.
        var actions = BuildActionList(currentRoom, allMonsters);

        // Use Spectre's built-in SelectionPrompt for a nice arrow-key menu.
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What do you do?[/]")
                .PageSize(12)
                .AddChoices(actions));

        return choice;
    }

    /// <summary>
    /// Shows a message to the player and waits for them to press a key.
    /// Used by the GameEngine after each action to surface the outcome.
    /// </summary>
    public void ShowMessage(string message, ConsoleColor color = ConsoleColor.White)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{SpectreColor(color)}]{Markup.Escape(message)}[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Build the context-sensitive action list shown in the SelectionPrompt.
    /// </summary>
    private static List<string> BuildActionList(Room? currentRoom, IEnumerable<Monster> allMonsters)
    {
        var actions = new List<string>();

        // Navigation
        if (currentRoom?.NorthRoomId.HasValue == true) actions.Add("Go North");
        if (currentRoom?.SouthRoomId.HasValue == true) actions.Add("Go South");
        if (currentRoom?.EastRoomId.HasValue == true)  actions.Add("Go East");
        if (currentRoom?.WestRoomId.HasValue == true)  actions.Add("Go West");

        // Context actions
        if (currentRoom != null)
        {
            var monstersHere = allMonsters.Any(m => m.CurrentRoomId == currentRoom.Id && m.Health > 0);
            if (monstersHere) actions.Add("Attack Monster");
            if (currentRoom.Items.Any()) actions.Add("Pick Up Item");
        }

        // Inventory / Drop
        actions.Add("Drop Item");
        actions.Add("Use Consumable");

        // System actions
        actions.Add("Inspect Room");
        actions.Add("Switch to Admin Mode");
        actions.Add("Quit");

        return actions;
    }

    private static string SpectreColor(ConsoleColor color) => color switch
    {
        ConsoleColor.Red => "red",
        ConsoleColor.Green => "green",
        ConsoleColor.Yellow => "yellow",
        ConsoleColor.Cyan => "cyan",
        ConsoleColor.DarkGray => "dim",
        _ => "white"
    };
}
