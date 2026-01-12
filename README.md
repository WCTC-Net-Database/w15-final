# Week 15: Final Exam - RPG World Builder

> **Template Purpose:** This is the final exam template. It provides a world-building RPG framework where you'll create an immersive game experience with navigable rooms, character interactions, and visual map displays.

---

## Overview

The final exam brings together everything you've learned this semester: EF Core, LINQ, SOLID principles, and game architecture. You'll extend an existing RPG framework to create a complete world-building experience with exploration, combat, and character management.

## Learning Objectives

By completing this exam, you will demonstrate:
- [ ] Proficiency with Entity Framework Core and LINQ
- [ ] Understanding of TPH inheritance patterns
- [ ] Ability to implement CRUD operations
- [ ] Skill in creating navigable game worlds
- [ ] Application of SOLID principles in practice

## Prerequisites

Before starting, ensure you have:
- [ ] Completed all weekly assignments (or reviewed the templates)
- [ ] Working SQL Server LocalDB installation
- [ ] Understanding of EF Core migrations
- [ ] Familiarity with Spectre.Console (used in this template)

## What's Being Tested

| Concept | Where You Learned It |
|---------|---------------------|
| EF Core CRUD | Weeks 9-12 |
| TPH Inheritance | Week 10 |
| LINQ Queries | Weeks 3, 7, 12 |
| Navigation Properties | Weeks 10-12 |
| Abstract Classes | Week 6 |
| SOLID Principles | Weeks 3-6 |

---

## Project Structure

The solution consists of two projects:

### ConsoleRpg (Console Application)
- **Program.cs** - Entry point with dependency injection
- **Startup.cs** - Service configuration
- **Services/GameEngine.cs** - Core game loop (Exploration + Admin modes)
- **Helpers/MapManager.cs** - Visual map using Spectre.Console
- **Helpers/MenuManager.cs** - Admin menu for CRUD operations
- **Helpers/OutputManager.cs** - Console output buffering

### ConsoleRpgEntities (Entity Framework Library)
- **Data/GameContext.cs** - DbContext with all DbSets
- **Models/Characters/** - Player and Monster entities
- **Models/Abilities/** - Ability hierarchy (TPH)
- **Models/Equipments/** - Items and Equipment
- **Models/Rooms/Room.cs** - Rooms with directional navigation

---

## Dual-Mode Design

The game operates in two modes:

### Exploration Mode (Default)
- Visual map showing all rooms
- Current room highlighted
- Available exits (N, S, E, W)
- Monsters and players in room
- Context-sensitive actions

### Admin/Developer Mode (Press 'X')
- CRUD operations for characters
- Room creation and configuration
- Database management
- Advanced queries

---

## Exam Requirements

### "C" Level (405/500 points)

**Required: All basic CRUD + these features:**

1. **Add Abilities to a Character** (Admin Menu)
   - Display list of characters
   - Display available abilities
   - Associate ability with character (many-to-many)
   - Save and confirm

2. **Display Character Abilities** (Admin Menu)
   - Select a character
   - Use `.Include(p => p.Abilities)`
   - Display character info and abilities

3. **Execute Ability During Attack** (Exploration Mode)
   - Implement `AttackMonster()` in GameEngine
   - Implement `UseAbilityOnMonster()`
   - Apply damage and update database

### "B" Level (445/500 points)

**Required: All "C" level + these features:**

1. **Add New Room** (Admin Menu)
   - Prompt for name and description
   - Set directional connections
   - Save to database

2. **Display Room Details** (Admin Menu)
   - List all rooms
   - Use `.Include(r => r.Players).Include(r => r.Monsters)`
   - Show exits and inhabitants

3. **Navigate Rooms** (Already implemented!)
   - N, S, E, W navigation
   - Updates player's RoomId

### "A" Level (475/500 points)

**Required: All "B" level + these features:**

1. **List Characters in Room by Attribute** (Admin Menu)
   - Select a room
   - Filter by attribute (Health, Experience)
   - Display matching characters

2. **List All Rooms with Characters** (Admin Menu)
   - Query rooms with `.Include(r => r.Players)`
   - Display grouped by room

3. **Find Equipment Location** (Admin Menu)
   - Search for item by name
   - Find which character has it
   - Display character and room location

---

## Key Models

### Player
```csharp
public int Id { get; set; }
public string Name { get; set; }
public int Health { get; set; }
public int Experience { get; set; }
public int? EquipmentId { get; set; }
public int? RoomId { get; set; }
public virtual Equipment Equipment { get; set; }
public virtual Room Room { get; set; }
public virtual ICollection<Ability> Abilities { get; set; }
```

### Room (Key for World Building)
```csharp
public int Id { get; set; }
public string Name { get; set; }
public string Description { get; set; }
public int? NorthRoomId { get; set; }
public int? SouthRoomId { get; set; }
public int? EastRoomId { get; set; }
public int? WestRoomId { get; set; }
public virtual Room NorthRoom { get; set; }
public virtual Room SouthRoom { get; set; }
public virtual Room EastRoom { get; set; }
public virtual Room WestRoom { get; set; }
public virtual ICollection<Player> Players { get; set; }
public virtual ICollection<Monster> Monsters { get; set; }
```

### Monster (TPH)
```csharp
public int Id { get; set; }
public string Name { get; set; }
public int Health { get; set; }
public int AggressionLevel { get; set; }
public string MonsterType { get; set; }  // Discriminator
public int? RoomId { get; set; }
public virtual Room Room { get; set; }
```

---

## LINQ Examples

```csharp
// Filter characters in a room by health
var healthyPlayers = _context.Players
    .Where(p => p.RoomId == roomId && p.Health > 50)
    .ToList();

// Rooms with players
var roomsWithPlayers = _context.Rooms
    .Include(r => r.Players)
    .Where(r => r.Players.Any())
    .ToList();

// Find equipment location
var playerWithSword = _context.Players
    .Include(p => p.Equipment)
        .ThenInclude(e => e.Weapon)
    .Include(p => p.Room)
    .FirstOrDefault(p => p.Equipment.Weapon.Name.Contains("Sword"));
```

---

## Getting Started

### 1. Setup Database
```bash
cd /path/to/project

# Apply migrations (creates tables and seed data)
dotnet ef database update --project ConsoleRpgEntities --startup-project ConsoleRpg
```

### 2. Build and Run
```bash
dotnet build
dotnet run --project ConsoleRpg
```

### 3. First Run
1. Start in Exploration Mode (Town Square)
2. Navigate using N, S, E, W
3. Press **X** for Admin Menu
4. Press **0** to return to Exploration

---

## Grading Rubric

| Level | Points | Requirements |
|-------|--------|--------------|
| Basic CRUD | 325 | Add/Edit/Display/Search Characters |
| "C" Level | 405 | + Abilities management, ability execution |
| "B" Level | 445 | + Room creation, room details, navigation |
| "A" Level | 475 | + Advanced queries, equipment location |
| **Excellence** | 500 | + World design, polish, creativity |

---

## Evaluation Criteria

1. **Functionality** - Do all features work correctly?
2. **World Design** - Is the world interesting and well-connected?
3. **Code Quality** - Clean, organized, properly commented?
4. **Error Handling** - Graceful handling of edge cases?
5. **User Experience** - Is the game enjoyable to play?
6. **Creativity** - Did you go beyond minimum requirements?

---

## World-Building Philosophy

This is NOT just a CRUD application. Students should:

- **Expand the world** - Add more rooms beyond the 3x3 grid
- **Create atmosphere** - Write compelling room descriptions
- **Design encounters** - Place monsters strategically
- **Build connections** - Create interesting navigation paths
- **Add depth** - Implement meaningful ability interactions
- **Polish presentation** - Use Spectre.Console for rich UI

---

## Tips

- Start with the basics - make CRUD work first
- Test incrementally - don't write all features at once
- Use the debugger - step through code to understand flow
- Read the logs - they help track what's happening
- Be creative - the template is a starting point
- Ask questions - if requirements are unclear, ask

---

## Submission Checklist

- [ ] All basic CRUD operations work
- [ ] Character abilities can be added and displayed
- [ ] Combat system with abilities implemented
- [ ] Rooms can be created and navigated
- [ ] Advanced queries return correct results
- [ ] World design is coherent and interesting
- [ ] Code is clean and well-commented
- [ ] Error handling implemented
- [ ] Logging used throughout

---

## Resources

- [Spectre.Console Documentation](https://spectreconsole.net/)
- [EF Core Include/ThenInclude](https://learn.microsoft.com/en-us/ef/core/querying/related-data/)
- [LINQ Query Syntax](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)

---

## Need Help?

- Raise your hand during the exam
- Review inline comments in the code
- Check the examples in this README
- Use the logging output to debug issues

Good luck creating your RPG world!
