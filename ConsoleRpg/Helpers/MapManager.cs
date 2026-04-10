using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace ConsoleRpg.Helpers;

/// <summary>
/// MapManager - Renders the game world as a Spectre.Console panel.
///
/// Uses each Room's X/Y coordinates to draw a grid-based ASCII map. The
/// current room is highlighted, rooms with monsters are red, empty rooms
/// are blue. Connections between rooms are drawn as horizontal (═) or
/// vertical (║) lines.
///
/// ===== HOW THE MAP RENDERING WORKS =====
/// 1. Find the bounding box (min/max X and Y) of all rooms
/// 2. For each Y row from top to bottom:
///    a. For each X column left to right, find the Room at (x, y)
///    b. Print a symbol for the room (or spaces if no room)
///    c. If the room has an east connection, print "═" between cells
/// 3. Between rows, print a row of "║" for any room with a south connection
///
/// This is intentionally a straightforward nested-loop algorithm. A more
/// sophisticated version would handle diagonal connections or non-grid
/// layouts, but for a 2D dungeon with cardinal exits, this is all you need.
/// </summary>
public class MapManager
{
    /// <summary>
    /// Builds a Spectre panel containing the rendered map. Pass the current
    /// room and the list of monsters so the renderer can highlight both
    /// "where you are" and "where the monsters are".
    /// </summary>
    public Panel BuildMapPanel(IEnumerable<Room> rooms, Room? currentRoom, IEnumerable<Monster>? monsters = null)
    {
        var roomList = rooms.ToList();
        if (!roomList.Any())
        {
            return Panel(new Markup("[red]No rooms available.[/]"), "World Map");
        }

        var monstersByRoom = (monsters ?? Array.Empty<Monster>())
            .Where(m => m.CurrentRoomId.HasValue && m.Health > 0)
            .GroupBy(m => m.CurrentRoomId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var content = BuildMapMarkup(roomList, currentRoom, monstersByRoom);
        return Panel(content, "World Map");
    }

    /// <summary>
    /// Builds a compact "current room details" panel: description, exits,
    /// monsters in the room, and items on the floor. Used alongside the
    /// map panel in the split-screen exploration view.
    /// </summary>
    public Panel BuildRoomDetailsPanel(Room? room, IEnumerable<Monster>? monsters = null)
    {
        if (room == null)
        {
            return Panel(new Markup("[red]No room information.[/]"), "Current Location");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[bold]{EscapeMarkup(room.Description)}[/]");
        sb.AppendLine();

        // Exits
        var exits = new List<string>();
        if (room.NorthRoomId.HasValue) exits.Add("[cyan]N[/]orth");
        if (room.SouthRoomId.HasValue) exits.Add("[cyan]S[/]outh");
        if (room.EastRoomId.HasValue)  exits.Add("[cyan]E[/]ast");
        if (room.WestRoomId.HasValue)  exits.Add("[cyan]W[/]est");
        sb.AppendLine(exits.Any()
            ? $"[green]Exits:[/] {string.Join(", ", exits)}"
            : "[dim]No visible exits.[/]");

        // Monsters in this room
        var monstersHere = (monsters ?? Array.Empty<Monster>())
            .Where(m => m.CurrentRoomId == room.Id && m.Health > 0)
            .ToList();
        if (monstersHere.Any())
        {
            sb.AppendLine($"[red]Monsters:[/] {string.Join(", ", monstersHere.Select(m => $"{EscapeMarkup(m.Name)} (HP:{m.Health})"))}");
        }

        // Items on the floor
        if (room.Items.Any())
        {
            sb.AppendLine($"[yellow]On the floor:[/] {string.Join(", ", room.Items.Select(i => EscapeMarkup(i.Name)))}");
        }

        return Panel(new Markup(sb.ToString().TrimEnd()), room.Name);
    }

    /// <summary>
    /// Builds a small "player stats" panel showing name, HP, and a summary
    /// of inventory/equipment.
    /// </summary>
    public Panel BuildPlayerPanel(ConsoleRpgEntities.Models.Characters.Player? player)
    {
        if (player == null)
        {
            return Panel(new Markup("[red]No player.[/]"), "Character");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[bold]{EscapeMarkup(player.Name)}[/] (Lvl {player.Level})");
        sb.AppendLine($"[green]HP:[/] {player.Health}  [yellow]XP:[/] {player.Experience}");
        sb.AppendLine($"[yellow]Atk:[/] {player.GetTotalAttack()}  [blue]Def:[/] {player.GetTotalDefense()}");

        var bagWeight = player.GetCurrentWeight();
        var bagMax = player.Inventory?.MaxWeight ?? 0;
        sb.AppendLine($"[dim]Bag:[/] {bagWeight}/{bagMax} lbs");

        return Panel(new Markup(sb.ToString().TrimEnd()), "Character");
    }

    // ====================================================================
    // INTERNAL HELPERS
    // ====================================================================

    private static Panel Panel(IRenderable content, string header) =>
        new(content)
        {
            Header = new PanelHeader($"[yellow]{header}[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };

    /// <summary>
    /// Core map rendering: walks the grid from max Y down to min Y and
    /// from min X up to max X, printing a symbol for each room and
    /// drawing connections between them.
    /// </summary>
    private static Markup BuildMapMarkup(List<Room> rooms, Room? currentRoom, Dictionary<int, int> monstersByRoom)
    {
        int minX = rooms.Min(r => r.X);
        int maxX = rooms.Max(r => r.X);
        int minY = rooms.Min(r => r.Y);
        int maxY = rooms.Max(r => r.Y);

        var sb = new StringBuilder();

        for (int y = maxY; y >= minY; y--)
        {
            // Row of room symbols with east connectors between them
            for (int x = minX; x <= maxX; x++)
            {
                var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                if (room != null)
                {
                    sb.Append(SymbolFor(room, currentRoom, monstersByRoom));

                    if (x < maxX)
                    {
                        sb.Append(room.EastRoomId.HasValue ? "[dim]═[/]" : " ");
                    }
                }
                else
                {
                    sb.Append("   ");
                    if (x < maxX) sb.Append(' ');
                }
            }
            sb.AppendLine();

            // Row of south connectors (only between real rows)
            if (y > minY)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);
                    sb.Append(room != null && room.SouthRoomId.HasValue ? "[dim] ║ [/]" : "   ");
                    if (x < maxX) sb.Append(' ');
                }
                sb.AppendLine();
            }
        }

        sb.Append("[dim][[@]]=You [[M]]=Monster [[■]]=Empty[/]");

        return new Markup(sb.ToString());
    }

    private static string SymbolFor(Room room, Room? currentRoom, Dictionary<int, int> monstersByRoom)
    {
        if (currentRoom != null && room.Id == currentRoom.Id)
            return "[green on white][[@]][/]";

        if (monstersByRoom.ContainsKey(room.Id))
            return "[red][[M]][/]";

        return "[blue][[■]][/]";
    }

    /// <summary>
    /// Spectre.Console's markup parser uses [brackets] as control characters,
    /// so user-provided strings (like room descriptions or monster names)
    /// need to have any literal brackets escaped with doubles: [[ and ]].
    /// </summary>
    private static string EscapeMarkup(string input)
        => input.Replace("[", "[[").Replace("]", "]]");
}
