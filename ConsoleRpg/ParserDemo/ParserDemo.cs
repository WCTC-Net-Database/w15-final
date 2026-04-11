using Spectre.Console;

namespace ConsoleRpg.ParserDemo;

// =====================================================================
// PARSER DEMO - A STRETCH GOAL FOR THE FINAL PROJECT
// =====================================================================
//
// What this is:
//   A self-contained mini-game that demonstrates how a Zork-style text
//   parser works. It runs from the admin menu, has its own tiny world
//   (one room, one mailbox, one leaflet - a deliberate throwback to the
//   1980 original), and shares NO code with the rest of ConsoleRPG.
//
// Why it's separate from the main game:
//   The W15 rubric uses Spectre.Console SelectionPrompt menus because
//   menus are accessible: students can play the game without first
//   solving the parsing problem. This demo exists so students who WANT
//   to learn how text input works have a complete, readable reference
//   they can study and then port into the real game on their own.
//
//   The "stretch goal" wording is intentional: nothing in the rubric
//   asks for this, and nothing in the main game depends on it. You can
//   delete this entire folder and the game still builds and runs.
//
// =====================================================================
// THE TWO IDEAS THIS DEMO SHOWS
// =====================================================================
//
// 1. THE COMMAND PATTERN (the Open/Closed Principle, applied to verbs)
//    Every verb the player can type is a class that implements
//    IParserCommand. The TextParser holds a Dictionary<string, IParserCommand>
//    and dispatches by verb name. Adding a new verb is ONE new class +
//    ONE new line in the registry - the parser code itself never changes.
//    That's the same OCP payoff you saw with the TPH discriminators in
//    GameContext: extend by adding, never by editing.
//
// 2. A MINIMAL TEXT PARSER (tokenize -> verb -> noun phrase -> resolve)
//    The parser does four things, in order:
//      a. tokenize:     split the input on whitespace
//      b. canonicalize: map synonyms to a canonical verb ("get" -> "take",
//                       "l" -> "look", "i" -> "inventory")
//      c. dispatch:     look the verb up in the registry
//      d. resolve:      let the command find the noun in the visible
//                       objects (room contents + inventory)
//    This is the SAME shape as a real parser, just with the grammar
//    pinned to "verb [noun]". Real parsers add prepositions and indirect
//    objects ("put leaflet IN mailbox") - that's a great extension to
//    try once you understand the basic loop.
//
// =====================================================================
// WHERE TO GO FROM HERE (extension ideas, ranked easy -> hard)
// =====================================================================
//
//   EASY:
//   - Add more synonyms (s -> south, x -> examine, g -> again)
//   - Add a new verb: write a class implementing IParserCommand and
//     add one line to TextParser.BuildRegistry. Try "shake", "smell",
//     or "wave" for flavor.
//   - Make the leaflet's text a multi-line block of your own writing.
//
//   MEDIUM:
//   - Add prepositions: parse "put leaflet in mailbox" as
//     verb=put, direct=leaflet, prep=in, indirect=mailbox.
//   - Add a second room and a "go <direction>" verb. The mock World
//     class already has room for this - add an Exits dictionary.
//   - Disambiguation prompts: if the player types "take key" and there
//     are two keys in the room, ask "which one - the brass key or the
//     iron key?"
//
//   HARD (this is the actual stretch goal):
//   - Port the parser to the REAL game. Replace the mock World with
//     GameContext + Player.CurrentRoom. Replace MockItem with the
//     existing Item TPH (Weapon/Armor/Consumable/KeyItem). Each command
//     class delegates to PlayerService instead of mutating local state.
//     When you finish, you've replaced the entire SelectionPrompt menu
//     with a Zork-style command line - using NONE of the existing
//     UI code.
//
// =====================================================================

/// <summary>
/// Entry point for the parser demo. Called from GameEngine.AdminTurn
/// when the player picks "Parser Demo (Stretch Goal)".
/// </summary>
public class ParserDemo
{
    public void Run()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow bold]Parser Demo - West of House[/]").Centered());
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]This is a self-contained text-parser demo. Type 'help' for verbs, 'quit' to leave.[/]");
        AnsiConsole.MarkupLine("[dim]Nothing you do here affects the main game.[/]");
        AnsiConsole.WriteLine();

        // Build the world and the parser. Both are throwaway local state -
        // when this method returns, all of it is garbage-collected and the
        // real game resumes exactly where it left off.
        var world = MockWorld.BuildClassic();
        var parser = new TextParser();

        // Print the room description on entry, just like Zork did.
        new LookCommand().Execute(world, Array.Empty<string>());

        // Classic Zork REPL loop. Read a line, parse it, repeat until quit.
        while (!world.QuitRequested)
        {
            AnsiConsole.Markup("[green]>[/] ");
            var input = Console.ReadLine();
            if (input == null) break;          // Ctrl+Z / EOF
            if (string.IsNullOrWhiteSpace(input)) continue;

            parser.Parse(input, world);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Returning to admin menu. Press any key...[/]");
        Console.ReadKey(true);
    }
}

// =====================================================================
// THE WORLD - mock POCOs, no database, no DI, no entities
// =====================================================================
//
// Everything below is intentionally tiny. The whole point of the demo
// is that you can read it top-to-bottom in one sitting. If you find
// yourself adding fields here, ask whether the new feature really
// belongs in the demo or whether it should live in the real game.

/// <summary>
/// A mock item. Has a canonical name, the synonyms the parser will
/// match against (Zork called these "synonyms" and "adjectives"), an
/// optional read-text for things like leaflets, and a set of flags
/// that mirror the original Zork object flags.
/// </summary>
public class MockItem
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ReadText { get; set; }
    public List<string> Synonyms { get; set; } = new();

    // Container behavior
    public bool IsContainer { get; set; }
    public bool IsOpen { get; set; }
    public List<MockItem> Contents { get; set; } = new();

    // Flags
    public bool IsTakeable { get; set; }
    public bool IsReadable { get; set; }

    /// <summary>
    /// Returns true if this item answers to the given typed word.
    /// Match is case-insensitive and checks the canonical name AND
    /// every synonym - so "mailbox", "MAILBOX", and "box" all hit
    /// the same item.
    /// </summary>
    public bool Matches(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        if (Name.Equals(word, StringComparison.OrdinalIgnoreCase)) return true;
        if (Name.Split(' ').Any(part => part.Equals(word, StringComparison.OrdinalIgnoreCase))) return true;
        return Synonyms.Any(s => s.Equals(word, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// The mock world: one room with a description, an item list on the
/// floor, and a player inventory. No EF Core, no migrations, no DI -
/// just a plain object the parser commands mutate directly.
///
/// "Visible objects" means "things the parser is allowed to find when
/// resolving a noun" - currently that's room contents + open container
/// contents + inventory. If you add darkness or hidden items, that's
/// where the rule lives.
/// </summary>
public class MockWorld
{
    public string RoomName { get; set; } = "";
    public string RoomDescription { get; set; } = "";
    public List<MockItem> RoomItems { get; set; } = new();
    public List<MockItem> Inventory { get; set; } = new();
    public bool QuitRequested { get; set; }

    /// <summary>
    /// Walks every object the player can currently see and returns
    /// the flat list. Open containers reveal their contents; closed
    /// ones don't. This is what the noun-resolution step searches.
    /// </summary>
    public IEnumerable<MockItem> VisibleObjects()
    {
        foreach (var item in RoomItems)
        {
            yield return item;
            if (item.IsContainer && item.IsOpen)
                foreach (var inner in item.Contents)
                    yield return inner;
        }
        foreach (var item in Inventory)
            yield return item;
    }

    /// <summary>
    /// Builds the canonical "West of House" opening from the original
    /// 1980 Zork. The text is verbatim from the game data so the demo
    /// feels authentic. Adding more rooms is left as an exercise.
    /// </summary>
    public static MockWorld BuildClassic()
    {
        var leaflet = new MockItem
        {
            Name = "leaflet",
            Description = "A small leaflet.",
            Synonyms = new() { "leaflet", "pamphlet", "mail", "paper" },
            IsTakeable = true,
            IsReadable = true,
            ReadText =
                "\"WELCOME TO ZORK!\n\n" +
                "ZORK is a game of adventure, danger, and low cunning. In it you will\n" +
                "explore some of the most amazing territory ever seen by mortals. No\n" +
                "computer should be without one!\""
        };

        var mailbox = new MockItem
        {
            Name = "small mailbox",
            Description = "A small mailbox.",
            Synonyms = new() { "mailbox", "box" },
            IsContainer = true,
            IsOpen = false,
            Contents = new() { leaflet }
        };

        return new MockWorld
        {
            RoomName = "West of House",
            RoomDescription =
                "You are standing in an open field west of a white house, " +
                "with a boarded front door.\nThere is a small mailbox here.",
            RoomItems = new() { mailbox }
        };
    }
}

// =====================================================================
// THE PARSER
// =====================================================================

/// <summary>
/// Every verb the player can type is one of these. The Execute method
/// gets the world (so it can mutate it) and the post-verb tokens (so
/// it can resolve a noun).
///
/// Why an interface and not a delegate? Because you'll want stateful
/// commands eventually (a Take command might want a "take all" mode,
/// a Go command might track the last direction). Classes give you
/// somewhere to put that state without changing the registry shape.
/// </summary>
public interface IParserCommand
{
    /// <summary>One-line help text shown by the Help verb.</summary>
    string HelpText { get; }

    /// <summary>Run the verb. args is everything after the verb token.</summary>
    void Execute(MockWorld world, string[] args);
}

/// <summary>
/// The parser itself. Owns the synonym map and the verb registry,
/// and exposes one method - Parse - that does the full pipeline:
/// tokenize, canonicalize, dispatch.
///
/// Notice that Parse() is ~15 lines and has no knowledge of any
/// individual verb. That's the OCP payoff: adding a verb requires
/// editing exactly one method (BuildRegistry), and the parser logic
/// stays untouched.
/// </summary>
public class TextParser
{
    private readonly Dictionary<string, IParserCommand> _verbs;
    private readonly Dictionary<string, string> _synonyms;

    public TextParser()
    {
        _verbs = BuildRegistry();
        _synonyms = BuildSynonyms();
    }

    /// <summary>
    /// The full parse pipeline. Tokenize, canonicalize the verb,
    /// look it up, and dispatch. Unknown verbs print a Zork-style
    /// "I don't know the word" message rather than crashing.
    /// </summary>
    public void Parse(string input, MockWorld world)
    {
        // STEP 1: tokenize on whitespace. Real parsers strip punctuation
        // and normalize quotes here too.
        var tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return;

        // STEP 2: canonicalize the verb. "get" becomes "take", "l"
        // becomes "look", etc. The synonym map is the only place that
        // knows about player-facing aliases - the command classes only
        // ever see canonical verb names.
        var verb = tokens[0].ToLowerInvariant();
        if (_synonyms.TryGetValue(verb, out var canonical))
            verb = canonical;

        // STEP 3: dispatch. If the verb isn't in the registry, complain
        // in a way that's recognizably Zork.
        if (!_verbs.TryGetValue(verb, out var command))
        {
            AnsiConsole.MarkupLine($"[red]I don't know the word \"{Markup.Escape(tokens[0])}\".[/]");
            return;
        }

        // STEP 4: hand the rest of the tokens to the command and let
        // it do its own noun resolution. The parser deliberately does
        // NOT try to find the object - that responsibility lives with
        // the command, because different verbs care about different
        // subsets of the world ("read" only cares about readable items,
        // "take" only cares about takeable ones, etc.).
        var args = tokens.Skip(1).ToArray();
        command.Execute(world, args);
    }

    /// <summary>
    /// The verb registry. To add a new verb to the demo, write a class
    /// implementing IParserCommand and add one line here. The parser
    /// itself does not need to change.
    /// </summary>
    private static Dictionary<string, IParserCommand> BuildRegistry() => new()
    {
        ["look"]      = new LookCommand(),
        ["examine"]   = new ExamineCommand(),
        ["take"]      = new TakeCommand(),
        ["drop"]      = new DropCommand(),
        ["open"]      = new OpenCommand(),
        ["close"]     = new CloseCommand(),
        ["read"]      = new ReadCommand(),
        ["inventory"] = new InventoryCommand(),
        ["help"]      = new HelpCommand(),
        ["quit"]      = new QuitCommand(),
    };

    /// <summary>
    /// Player-facing aliases. The keys are what the player types; the
    /// values are the canonical verb name in the registry above. This
    /// is where you add "g" -> "again", "x" -> "examine", etc.
    /// </summary>
    private static Dictionary<string, string> BuildSynonyms() => new()
    {
        ["l"]    = "look",
        ["x"]    = "examine",
        ["get"]  = "take",
        ["grab"] = "take",
        ["i"]    = "inventory",
        ["inv"]  = "inventory",
        ["?"]    = "help",
        ["q"]    = "quit",
        ["exit"] = "quit",
    };

    /// <summary>
    /// Helper for the Help command - exposes the registry so it can
    /// list every verb. Kept on the parser rather than the command so
    /// the registry stays private.
    /// </summary>
    public IEnumerable<KeyValuePair<string, IParserCommand>> AllVerbs() => _verbs;
}

// =====================================================================
// THE COMMANDS
// =====================================================================
//
// One class per verb. Each is small enough to read in one screen.
// Note how every command does its own noun resolution against
// world.VisibleObjects() - this is the seam where you would later
// swap in real game state.

/// <summary>
/// "look" - print the room description and what's lying around. The
/// most common verb in any text adventure, so it's first.
/// </summary>
public class LookCommand : IParserCommand
{
    public string HelpText => "look - describe your surroundings";

    public void Execute(MockWorld world, string[] args)
    {
        AnsiConsole.MarkupLine($"[yellow bold]{Markup.Escape(world.RoomName)}[/]");
        AnsiConsole.MarkupLine(Markup.Escape(world.RoomDescription));
    }
}

/// <summary>
/// "examine X" / "x X" - print one object's description. Falls back
/// to a not-found message if no visible object matches the noun.
/// </summary>
public class ExamineCommand : IParserCommand
{
    public string HelpText => "examine <thing> - look at something closely";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Examine what?[/]");
            return;
        }

        var target = ResolveNoun(world, args);
        if (target == null)
        {
            AnsiConsole.MarkupLine("[red]You don't see that here.[/]");
            return;
        }

        AnsiConsole.MarkupLine(Markup.Escape(target.Description));
        if (target.IsContainer)
        {
            if (!target.IsOpen)
                AnsiConsole.MarkupLine("[dim]It is closed.[/]");
            else if (target.Contents.Any())
                AnsiConsole.MarkupLine($"[dim]It contains: {string.Join(", ", target.Contents.Select(c => c.Name))}.[/]");
            else
                AnsiConsole.MarkupLine("[dim]It is empty.[/]");
        }
    }

    /// <summary>
    /// Tiny noun-resolver. Walks every visible object and returns the
    /// first one that matches any of the typed words. Real parsers
    /// rank matches by adjective overlap - this one takes the first hit.
    /// </summary>
    public static MockItem? ResolveNoun(MockWorld world, string[] args)
    {
        foreach (var item in world.VisibleObjects())
            foreach (var word in args)
                if (item.Matches(word))
                    return item;
        return null;
    }
}

/// <summary>
/// "take X" / "get X" - move an item from the room (or an open
/// container in the room) into the player's inventory.
/// </summary>
public class TakeCommand : IParserCommand
{
    public string HelpText => "take <thing> - pick something up";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Take what?[/]");
            return;
        }

        var target = ExamineCommand.ResolveNoun(world, args);
        if (target == null)
        {
            AnsiConsole.MarkupLine("[red]You don't see that here.[/]");
            return;
        }

        if (world.Inventory.Contains(target))
        {
            AnsiConsole.MarkupLine("[yellow]You already have that.[/]");
            return;
        }

        if (!target.IsTakeable)
        {
            AnsiConsole.MarkupLine("[red]You can't take that.[/]");
            return;
        }

        // Remove from wherever it is - either the room floor or an
        // open container's contents - then add to inventory.
        if (world.RoomItems.Remove(target))
        {
            world.Inventory.Add(target);
            AnsiConsole.MarkupLine("[green]Taken.[/]");
            return;
        }
        foreach (var container in world.RoomItems.Where(i => i.IsContainer && i.IsOpen))
        {
            if (container.Contents.Remove(target))
            {
                world.Inventory.Add(target);
                AnsiConsole.MarkupLine("[green]Taken.[/]");
                return;
            }
        }
    }
}

/// <summary>
/// "drop X" - move an item from inventory back to the room floor.
/// </summary>
public class DropCommand : IParserCommand
{
    public string HelpText => "drop <thing> - drop something from your inventory";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Drop what?[/]");
            return;
        }

        var target = world.Inventory.FirstOrDefault(i => args.Any(w => i.Matches(w)));
        if (target == null)
        {
            AnsiConsole.MarkupLine("[red]You aren't carrying that.[/]");
            return;
        }

        world.Inventory.Remove(target);
        world.RoomItems.Add(target);
        AnsiConsole.MarkupLine("[green]Dropped.[/]");
    }
}

/// <summary>
/// "open X" - open a container so its contents become visible to
/// the noun resolver. The mailbox is the canonical example.
/// </summary>
public class OpenCommand : IParserCommand
{
    public string HelpText => "open <thing> - open a container";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Open what?[/]");
            return;
        }

        var target = ExamineCommand.ResolveNoun(world, args);
        if (target == null)
        {
            AnsiConsole.MarkupLine("[red]You don't see that here.[/]");
            return;
        }

        if (!target.IsContainer)
        {
            AnsiConsole.MarkupLine("[red]That's not something you can open.[/]");
            return;
        }

        if (target.IsOpen)
        {
            AnsiConsole.MarkupLine("[yellow]It's already open.[/]");
            return;
        }

        target.IsOpen = true;
        var contents = target.Contents.Any()
            ? $", revealing: {string.Join(", ", target.Contents.Select(c => c.Name))}"
            : ", but it's empty";
        AnsiConsole.MarkupLine($"[green]Opening the {Markup.Escape(target.Name)}{contents}.[/]");
    }
}

/// <summary>
/// "close X" - the inverse of open. Closing a container hides its
/// contents from the noun resolver again.
/// </summary>
public class CloseCommand : IParserCommand
{
    public string HelpText => "close <thing> - close a container";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Close what?[/]");
            return;
        }

        var target = ExamineCommand.ResolveNoun(world, args);
        if (target == null || !target.IsContainer)
        {
            AnsiConsole.MarkupLine("[red]You can't close that.[/]");
            return;
        }

        if (!target.IsOpen)
        {
            AnsiConsole.MarkupLine("[yellow]It's already closed.[/]");
            return;
        }

        target.IsOpen = false;
        AnsiConsole.MarkupLine($"[green]Closed.[/]");
    }
}

/// <summary>
/// "read X" - print the readable text of an item. Only works on
/// items flagged Readable, which is how Zork distinguished a leaflet
/// from a sword.
/// </summary>
public class ReadCommand : IParserCommand
{
    public string HelpText => "read <thing> - read something with writing on it";

    public void Execute(MockWorld world, string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Read what?[/]");
            return;
        }

        var target = ExamineCommand.ResolveNoun(world, args);
        if (target == null)
        {
            AnsiConsole.MarkupLine("[red]You don't see that here.[/]");
            return;
        }

        if (!target.IsReadable || target.ReadText == null)
        {
            AnsiConsole.MarkupLine("[red]There's nothing to read on that.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine(target.ReadText);
        AnsiConsole.WriteLine();
    }
}

/// <summary>
/// "inventory" / "i" - list what the player is carrying. Always
/// works, never fails - it just prints "empty-handed" if the bag
/// is empty.
/// </summary>
public class InventoryCommand : IParserCommand
{
    public string HelpText => "inventory - list what you're carrying";

    public void Execute(MockWorld world, string[] args)
    {
        if (!world.Inventory.Any())
        {
            AnsiConsole.MarkupLine("[dim]You are empty-handed.[/]");
            return;
        }
        AnsiConsole.MarkupLine("[yellow]You are carrying:[/]");
        foreach (var item in world.Inventory)
            AnsiConsole.MarkupLine($"  [dim]-[/] {Markup.Escape(item.Name)}");
    }
}

/// <summary>
/// "help" - list every verb the parser knows. Reaches into the
/// parser's registry through a single AllVerbs() accessor.
/// </summary>
public class HelpCommand : IParserCommand
{
    public string HelpText => "help - list known verbs";

    public void Execute(MockWorld world, string[] args)
    {
        // Build a fresh parser just to enumerate verbs - in a real
        // game you'd inject the parser here. This is the demo's one
        // shortcut for the sake of fitting in a single file.
        var parser = new TextParser();
        AnsiConsole.MarkupLine("[yellow]Known verbs:[/]");
        foreach (var kv in parser.AllVerbs())
            AnsiConsole.MarkupLine($"  [cyan]{kv.Key}[/] - {Markup.Escape(kv.Value.HelpText)}");
        AnsiConsole.MarkupLine("[dim]Aliases: l=look, x=examine, get=take, i=inventory, q=quit[/]");
    }
}

/// <summary>
/// "quit" - flips the QuitRequested flag, which the REPL loop checks
/// each iteration. Doesn't call Environment.Exit because we want the
/// admin menu to resume cleanly when this returns.
/// </summary>
public class QuitCommand : IParserCommand
{
    public string HelpText => "quit - leave the parser demo and return to admin mode";

    public void Execute(MockWorld world, string[] args)
    {
        world.QuitRequested = true;
        AnsiConsole.MarkupLine("[yellow]Leaving the parser demo.[/]");
    }
}
