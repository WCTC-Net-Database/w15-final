# Week 15: Final Project — ConsoleRPG World Builder

> **Template Purpose:** This is the capstone template. Everything you've built from Week 12 through Week 14 is here, extended with a polished dual-mode UI, a richer seeded world, and reference implementations you can study and extend.

---

## Overview

Over the course of this semester you've built an RPG data model piece by piece:

| Week | What you added |
|------|----------------|
| **W9** | EF Core + migrations (first contact with the database) |
| **W10** | TPH inheritance for Monsters and Abilities |
| **W11** | Equipment system (legacy W11-era design, superseded in W12) |
| **W12** | `Item` and `Container` TPH hierarchies — the foundation |
| **W13** | `Chest` + `MonsterLoot` + the `ILockable` interface |
| **W14** | `Room` as a Container + `Door` using `ILockable` |
| **W15** | **→ You are here** — dual-mode UI, richer world, final presentation |

Week 15 is **not** about rewriting architecture. The model layer from W14 is already complete. Your job is to:

1. **Understand and explore** the finished framework (the hardest part)
2. **Use advanced LINQ** to answer new questions about the world
3. **Extend the world** with your own rooms, items, monsters, or small architecture additions
4. **Make it your own** — creative freedom is strongly encouraged for A+ work

## Learning Objectives

By the end of this project you will have demonstrated:
- [ ] Proficiency with EF Core, migrations, and LINQ across a non-trivial domain model
- [ ] Ability to read and extend a service-layered application
- [ ] Understanding of TPH inheritance for multiple entity hierarchies
- [ ] Application of all five SOLID principles in a connected example
- [ ] Creative world-building on top of a working framework

## Prerequisites

- [ ] Completed Week 12-14 assignments (or caught up using this template)
- [ ] SQL Server LocalDB installed and working
- [ ] EF Core migrations experience
- [ ] Basic familiarity with Spectre.Console (introduced lightly in W14)

---

## The World You Inherit

The seed data builds a complete starter world with:

- **10 rooms** forming a town, a maze, and a dungeon
- **3 monsters** (Goblin, Wolf, Skeleton) placed in specific rooms
- **3 doors** with different states (locked, trapped, secret)
- **6 chests** placed throughout the world (some locked, one trapped, one openable in town)
- **A starter kit** for the player (sword, armor, potions, lockpick)

### World Layout

```
         [Ancient Library]      [Training Grounds]
               |                         |
          [archway]                 [open path]
               |                         |
         [Northern Gate] === Northern Gate (trapped) ===
                       \                                 \
                        [Town Square]   ← YOU START HERE
                              |
                         [open path]
                              |
                       [Forest Edge]  ← Gray Shadow (Wolf)
                              |
                         [overgrown]
                              |
                    [Twisting Wilds]  ← Grubnak (Goblin)
                       /   |   \
                  [Grove  ...  Thicket]  ← a maze of twisty little
                   ↕ ↔          ↔ ↕         passages, all alike
                              |
                      [Ironbound Door LOCKED]
                  (requires Grubnak's Dungeon Key)
                              |
                     [Trapped Vault]  ← Rattlebones (Skeleton)
                              |              + dusty chest (trapped)
                              |
                     [Marble Panel SECRET]   ← Inspect Room to reveal
                              |
                     [Hidden Shrine]   ← ornate chest (same Dungeon Key)
```

The dungeon has a deliberate **one key, two locks** progression: the Dungeon Key Grubnak drops opens both the Ironbound Door AND the Ornate Chest in the Hidden Shrine. The Marble Panel between the Trapped Vault and the Hidden Shrine is a **secret door** — the room beyond it is hidden from your map until you find it via Inspect Room.

### The Progression Loop

A typical first playthrough looks something like:

1. Start in **Town Square**, open the merchant lockbox sitting on a stool, take what's inside
2. Move south through the **Forest Edge** — encounter the Wolf, fight, loot
3. Enter the **Twisting Wilds** — navigate the maze (hint: one path loops!)
4. Fight **Grubnak** to get the **Dungeon Key**
5. Use the key on the **Ironbound Door** to reach the **Trapped Vault**
6. Defeat **Rattlebones**, then carefully open the **dusty chest** (it's trapped — bring HP)
7. **Inspect Room** to find the secret **Marble Panel**
8. South into the **Hidden Shrine** — use the Dungeon Key on the **Ornate Chest** for the final reward

---

## Project Structure

This template preserves the two-project architecture you've been using since Week 7:

```
ConsoleRpgFinal.sln
│
├── ConsoleRpg/                            # UI, services, game loop
│   ├── Program.cs
│   ├── Startup.cs                         # DI configuration
│   ├── appsettings.json
│   ├── Models/
│   │   └── ServiceResult.cs               # NEW: service return type
│   ├── Services/
│   │   ├── GameEngine.cs                  # Tiny dispatcher (SRP)
│   │   ├── PlayerService.cs               # Exploration actions
│   │   └── AdminService.cs                # Admin-mode CRUD + LINQ
│   ├── Helpers/
│   │   ├── MapManager.cs                  # NEW: Spectre.Console ASCII map
│   │   ├── ExplorationUI.cs               # NEW: split-panel layout
│   │   ├── MenuManager.cs                 # Legacy menu helper
│   │   └── OutputManager.cs               # Legacy output buffer
│   └── ParserDemo/
│       └── ParserDemo.cs                  # NEW: Zork-style parser stretch goal
│
└── ConsoleRpgEntities/                    # Data + models (unchanged from W14)
    ├── Data/
    │   ├── GameContext.cs                 # All DbSets + TPH config
    │   └── GameContextFactory.cs
    ├── Models/
    │   ├── Characters/
    │   │   ├── Player.cs                  # Same Player from W14
    │   │   └── Monsters/
    │   │       ├── Monster.cs
    │   │       ├── Goblin.cs              # from W10
    │   │       ├── Wolf.cs                # NEW for W15
    │   │       └── Skeleton.cs            # NEW for W15
    │   ├── Containers/                    # W12 hierarchy + W13 + W14
    │   │   ├── IItemContainer.cs
    │   │   ├── ILockable.cs
    │   │   ├── Container.cs
    │   │   ├── Inventory.cs
    │   │   ├── Equipment.cs
    │   │   ├── Chest.cs
    │   │   ├── MonsterLoot.cs
    │   │   ├── Room.cs
    │   │   ├── Item.cs
    │   │   ├── Weapon.cs / Armor.cs / Consumable.cs / KeyItem.cs
    │   ├── World/
    │   │   └── Door.cs                    # W14 lockable door
    │   └── Abilities/
    │       └── PlayerAbilities/ ...       # TPH from W10
    ├── Helpers/
    │   ├── ConfigurationHelper.cs
    │   └── MigrationHelper.cs
    └── Migrations/
        ├── InitialCreate (W12)
        ├── SeedInitialData (W12)
        ├── AddChestsAndMonsterLoot (W13)
        ├── SeedWorldContent (W13)
        ├── AddRoomsAndDoors (W14)
        ├── AddMonsterTypes (W15 - Wolf/Skeleton columns)
        ├── AddChestLocation (W15 - Chest.LocationRoomId for placement)
        ├── SeedFinalWorld (W15)
        └── Scripts/
            ├── SeedInitialData.sql        # W12
            ├── SeedWorldContent.sql       # W13
            └── SeedFinalWorld.sql         # NEW for W15
```

---

## The Two Game Modes

When you run the game you're greeted by a Figlet title and dropped into **Exploration Mode**. Switching between modes is a menu option in each.

### Exploration Mode

A split-panel Spectre.Console layout:

- **Left panel:** the world map, drawn with ASCII art. Your current room is marked with `[@]`, rooms with monsters are `[M]`, empty rooms are `[■]`, and connections are drawn as horizontal/vertical lines. Rooms hidden behind undiscovered secret doors are NOT shown — find the door with Inspect Room to reveal them.
- **Right panel (top):** current room description, visible exits, monsters present, chests in the room (with status: locked / open / empty), and items on the floor.
- **Right panel (bottom):** combined character panel — name, level, HP, XP, attack/defense, what's equipped, and a one-line bag summary (`Bag: 14 items (5 wpn, 4 arm, 4 csm, 1 key) · 31/100 lbs`). The full per-item list is one click away via the **View Inventory** menu action.
- **Bottom:** a context-sensitive selection menu with only the actions that make sense right now — "Attack Monster" only shows if there's a live monster here, "Open Chest" only if a chest is in the room, "Equip Item" only if there's something equippable in your bag, and so on.

The full action list is: **Go N/S/E/W**, **Attack Monster**, **Pick Up Item**, **Open Chest**, **Equip Item**, **Unequip Item**, **Drop Item**, **Use Consumable**, **View Inventory**, **Inspect Room**, **Switch to Admin Mode**, **Quit**. Locked doors prompt you to pick a key from your bag before you walk through them — same with locked chests.

### Admin Mode

A straightforward arrow-key menu grouped by rubric tier:

**Base tier:**
- Add Character
- Display All Characters
- Search Character by Name

**B tier:**
- Display Room Details
- List All Rooms with Monsters

**A tier:**
- Add Ability to Character
- Display Character Abilities
- Find Item Location (LINQ)
- Monster Census (LINQ GroupBy)

**Stretch goal:**
- **Parser Demo** — a self-contained Zork-style text parser running on a tiny mock world (the original "West of House" mailbox/leaflet from 1980). Type `help` to see verbs, try `open mailbox`, `read leaflet`, `take leaflet`, `inventory`. See [Parser Demo](#parser-demo-stretch-goal) below for what it teaches and how to extend it.

---

## Getting Started

### 1. Apply the migrations

From the solution directory:

```bash
dotnet ef database update --project ConsoleRpgEntities --startup-project ConsoleRpg
```

This applies eight migrations in order, ending with `SeedFinalWorld` which populates your world. If you ever want a fresh start, run:

```bash
dotnet ef database update 0 --project ConsoleRpgEntities --startup-project ConsoleRpg
dotnet ef database update   --project ConsoleRpgEntities --startup-project ConsoleRpg
```

### 2. Build and run

```bash
dotnet build
dotnet run --project ConsoleRpg
```

### 3. Explore

Start by wandering around Town Square and surrounding rooms. Check the admin menu features. Fight the wolf. See how the map updates as you move. Try to figure out the maze.

---

## Grading Rubric

The rubric is intentionally tiered so students at different levels can succeed. Everything in the **Base** tier should feel familiar from previous weeks — it's "can you run the world and read the code you've been building all semester?"

### Base Tier (up to 75 points)

This tier is **comprehension + exploration**. You should be able to:

- [ ] Apply the migrations successfully
- [ ] Explore the entire world (find every room, including the secret shrine)
- [ ] Fight every monster and collect their loot
- [ ] Open every chest in the world (including the trapped one)
- [ ] Use both the Exploration and Admin menus
- [ ] Read and explain in your own words what each of these files does:
  - `Services/GameEngine.cs` (the dispatcher)
  - `Services/PlayerService.cs` (exploration actions)
  - `Services/AdminService.cs` (admin CRUD and LINQ)
  - `Helpers/MapManager.cs` (Spectre map rendering)
  - `Models/Containers/Container.cs` (the TPH base that makes everything tick)

**Everything at this tier is already working in the template.** You do not need to write code — you need to understand it, use it, and be able to discuss it during your final presentation.

### B Tier (up to 85 points)

Add **LINQ queries that answer new questions** about the world. Pick at least TWO of the following and implement them as new methods on `AdminService`:

- [ ] **"Inventory Audit"**: list every item in the game grouped by what kind of container it's in (Inventory, Chest, MonsterLoot, Room floor). Use `GroupBy` and join through `Item.Container`.
- [ ] **"Most Dangerous Room"**: find the room with the highest total monster HP. Use `GroupBy` on `Monster.CurrentRoomId` and `Sum` on Health.
- [ ] **"Locked Treasures"**: list every locked chest OR locked door that the player cannot currently open (i.e. the player doesn't have the required key). Use `Where` with a subquery.
- [ ] **"Floor Sweep"**: find the total gold value of all items lying on the floors of all rooms. Use `OfType<Room>()` + nested `Sum`.
- [ ] **Your own query**: any meaningful LINQ query that answers a question you came up with. Document what it does in a comment.

Wire each new method into the Admin menu so it's callable at runtime.

### A Tier (up to 95 points)

All of the B-tier work, PLUS a **small architecture extension** that requires a new class and a migration. Pick ONE:

- [ ] **Add a new Monster type** (e.g. `Orc`, `Troll`, `Dragon`) with at least one subclass-specific property and a distinctive `Attack()` behavior. Register it in `GameContext`, generate a migration, and add at least one instance to the seed world (by editing `SeedFinalWorld.sql` or a new seed migration).
- [ ] **Add a new Container type** (e.g. `Shop`, `Bookshelf`, `AltarOffering`) as a new TPH subclass. The new container should serve a world-building purpose: a shop exchanges items for gold, a bookshelf holds KeyItems called "tomes" that reveal lore, etc.
- [ ] **Add a new Item type** (e.g. `Scroll`, `Ring`, `Rune`) with subclass-specific behavior. Wire it into `Player.UseItem` or a new method.
- [ ] **Add a new ILockable entity** (e.g. a `LockedJournal`, a `MagicPortal`) that reuses `Player.TryUnlock` without modification. This is the Liskov Substitution payoff: your unlock code should work on it with zero changes.

### A+ Tier (up to 100 points) — Be Creative

Do all of A tier, PLUS show real creativity. Some ideas (not an exhaustive list):

- [ ] **Expand the world** with 5+ new rooms that form a coherent new area (a sewer, a swamp, a noble's manor, a pirate ship, whatever)
- [ ] **Add a shop with buying and selling** using a new Container type and a Gold field on Player
- [ ] **Add a quest system** — a table of active quests with a goal state and a reward
- [ ] **Add a combat abilities upgrade** — buff the existing ShoveAbility with new levels, or add a second ability like `Fireball` or `Heal`
- [ ] **Add save/load slots** so the player can roll back to a previous state
- [ ] **Improve the map rendering** — color-code rooms by biome, add a true "fog of war" where rooms you haven't visited yet are hidden (the template already hides rooms behind undiscovered secret doors — extend that to all unvisited rooms)
- [ ] **Port the Parser Demo to drive the real game.** Replace the SelectionPrompt menu with a Zork-style command line backed by `GameContext`. See [Parser Demo](#parser-demo-stretch-goal) below — the demo gives you a complete reference to copy from.
- [ ] **Take it somewhere we haven't seen** — a previous student built a WPF frontend for their ConsoleRPG. The data model is yours to play with.

The A+ tier is worth showing off in your final presentation, so pick something you'll enjoy demonstrating.

---

## Parser Demo (Stretch Goal)

Open the Admin menu and pick **"Parser Demo (Stretch Goal)"**. You'll be dropped into a self-contained mini-game: one room (West of House from the 1980 original Zork), one mailbox, one leaflet, and a Zork-style text parser. Type `help` to see the verbs.

### What it is

A complete reference implementation of a text parser, living in one file ([ConsoleRpg/ParserDemo/ParserDemo.cs](ConsoleRpg/ParserDemo/ParserDemo.cs)) and sharing **zero** code with the rest of the game. You can delete the entire `ParserDemo/` folder and the game still builds. It's there as something to study, not something the rubric requires you to use.

### What it teaches

Two ideas, both directly applicable to the real game:

1. **The Command pattern as an OCP demonstration.** Every verb is a class implementing `IParserCommand`. The parser holds a `Dictionary<string, IParserCommand>` and dispatches by name. Adding a new verb is one new class plus one new line in the registry — the parser code itself never changes. This is the same Open/Closed payoff you saw with the TPH discriminators in `GameContext`, applied to *behavior* instead of *data*.

2. **A minimal text parser pipeline.** Tokenize → canonicalize the verb (synonym lookup) → dispatch → resolve nouns against visible objects. Real parsers add prepositions and indirect objects ("put leaflet **in** mailbox"); this one stops at `verb [noun]` so you can read the whole pipeline in 15 lines.

### Why it's separate from the main game

The W15 rubric uses Spectre.Console SelectionPrompt menus because menus are accessible — you can play the game without first solving the parsing problem. The parser demo exists for students who want to learn how text input works and have a complete, readable reference to study.

### How to use it for the A+ tier

The hardest extension idea in the A+ list is **"port the parser to drive the real game."** Concretely, that means:

- Replace `MockWorld` with `GameContext` + `Player.CurrentRoom`
- Replace `MockItem` with the existing `Item` TPH (`Weapon`, `Armor`, `Consumable`, `KeyItem`)
- Each command class delegates to `PlayerService` instead of mutating local state
- Run the REPL inside `GameEngine.ExplorationTurn` instead of calling `RenderAndPrompt`

When you're done, you've replaced the entire menu UI with a Zork-style command line, using none of the existing UI code. That's a serious project — plan on 4-8 hours — but you start from a working reference and the path is fully documented inside `ParserDemo.cs`.

The doc-comment block at the top of [ParserDemo.cs](ConsoleRpg/ParserDemo/ParserDemo.cs) lists smaller extensions too (new verbs, synonyms, prepositions, disambiguation prompts) ranked easy → hard, so you can dip in at whatever level matches your interest.

---

## Final Presentation

Everyone presents their world in the last class. Prepare:

1. **A 5-minute demo** — walk the class through your world, showing the changes you made
2. **A short explanation** — what rubric tier did you target, what did you add, what was hardest
3. **A code walk-through** — pick ONE file you changed and explain what it does

---

## Tips

- **The admin menu is your friend.** Before writing any new LINQ, use the existing queries to understand the data shape.
- **Check SQL Server Object Explorer** while the game is running. Watch the `Containers`, `Items`, and `Doors` tables update in real time. That's the model layer in action.
- **Read `PlayerService.cs` before modifying `Player.cs`.** Most actions are already wired — you might not need to touch the entity at all.
- **The map rendering uses `Room.X` and `Room.Y`**, so if you add new rooms, give them sensible coordinates and they'll appear on the map automatically.
- **If you break the seed data, reset.** Run `dotnet ef database update 0` to wipe everything, then `dotnet ef database update` to reseed.
- **Ask questions.** W15 is deliberately "here's the framework, go build something." If you're stuck on where to start, ask — I'll be teaching more during office hours than I did any other week.

---

## Reference: LINQ Patterns You'll Use

```csharp
// Find all rooms with monsters
var dangerous = _context.Containers
    .OfType<Room>()
    .Where(r => _context.Monsters.Any(m => m.CurrentRoomId == r.Id && m.Health > 0))
    .ToList();

// Group items by container type
var itemsByLocation = _context.Items
    .Include(i => i.Container)
    .GroupBy(i => i.Container!.ContainerType)
    .Select(g => new { Location = g.Key, Count = g.Count() })
    .ToList();

// Sum the value of everything on every floor
var floorValue = _context.Containers
    .OfType<Room>()
    .SelectMany(r => r.Items)
    .Sum(i => i.Value);

// Find locked containers the player cannot open
var playerKeys = player.Inventory!.Items
    .OfType<KeyItem>()
    .Select(k => k.KeyId)
    .Where(k => k != null)
    .ToHashSet();

var unopenable = _context.Containers
    .OfType<Chest>()
    .Where(c => c.IsLocked && c.RequiredKeyId != null && !playerKeys.Contains(c.RequiredKeyId))
    .ToList();
```

---

## Submission

1. Commit your changes with meaningful messages throughout the project
2. Push to your GitHub Classroom repository
3. Submit the repository URL in Canvas
4. Be ready to present on the final day

---

## Resources

- [Spectre.Console documentation](https://spectreconsole.net/)
- [EF Core TPH Inheritance](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance)
- [EF Core Eager Loading](https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager)
- [LINQ Query Syntax vs Method Syntax](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)
- The **README files for Weeks 12-14** — go back and re-read the discussion sections. They explain the patterns you'll be extending.

---

## Need Help?

- Office hours are expanded for the final week
- Canvas discussion board
- In-class review sessions
- The in-class repository has examples of everything discussed above

Good luck! Build something you'd want to show off.
