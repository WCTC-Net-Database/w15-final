using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Containers;
using ConsoleRpgEntities.Models.World;
using Microsoft.EntityFrameworkCore;

namespace ConsoleRpgEntities.Data;

/// <summary>
/// GameContext - EF Core database context for the ConsoleRPG game.
///
/// Week 12: two TPH hierarchies
///   - Container (Inventory, Equipment)
///   - Item      (Weapon, Armor, Consumable, KeyItem)
///
/// Week 13: extends Container with Chest + MonsterLoot (no modifications)
///
/// Week 14: extends the world again
///   - Container adds Room (a place items live on the floor)
///   - New Door entity (not a Container) that connects rooms and can be
///     locked/trapped/secret using the same ILockable interface as chests
///
/// Notice the pattern: every week adds new types WITHOUT modifying existing
/// ones. That's the Open/Closed Principle paying dividends. By W14 we have
/// FIVE Container subclasses sharing one table and one set of item operations.
/// </summary>
public class GameContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Monster> Monsters { get; set; }
    public DbSet<Ability> Abilities { get; set; }

    // New in Week 12
    public DbSet<Container> Containers { get; set; }
    public DbSet<Item> Items { get; set; }

    // New in Week 14
    public DbSet<Door> Doors { get; set; }

    public GameContext(DbContextOptions<GameContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================================
        // TPH: Monster hierarchy (extended in Week 15)
        // ============================================
        // W10 introduced Goblin. W15 adds Wolf (wilderness) and Skeleton
        // (dungeon). Adding a new monster type is exactly one line in this
        // discriminator map, one new class file, and one seed row - zero
        // changes to existing Goblin code. That's the Open/Closed Principle
        // still paying dividends.
        modelBuilder.Entity<Monster>()
            .HasDiscriminator<string>(m => m.MonsterType)
            .HasValue<Goblin>("Goblin")
            .HasValue<Wolf>("Wolf")
            .HasValue<Skeleton>("Skeleton");

        // ============================================
        // TPH: Ability hierarchy (from Week 10)
        // ============================================
        modelBuilder.Entity<Ability>()
            .HasDiscriminator<string>(a => a.AbilityType)
            .HasValue<ShoveAbility>("ShoveAbility");

        // Many-to-many: Player <-> Ability
        modelBuilder.Entity<Player>()
            .HasMany(p => p.Abilities)
            .WithMany(a => a.Players)
            .UsingEntity(j => j.ToTable("PlayerAbilities"));

        // ============================================
        // TPH: Container hierarchy (extended in Week 14)
        // ============================================
        // All containers share ONE "Containers" table. W14 adds Room.
        modelBuilder.Entity<Container>()
            .HasDiscriminator<string>(c => c.ContainerType)
            .HasValue<Inventory>("Inventory")
            .HasValue<Equipment>("Equipment")
            .HasValue<Chest>("Chest")
            .HasValue<MonsterLoot>("MonsterLoot")
            .HasValue<Room>("Room");

        // ============================================
        // TPH: Item hierarchy (NEW in Week 12)
        // ============================================
        // All items (Weapons, Armor, Consumables, KeyItems) live in ONE "Items"
        // table with an ItemType discriminator.
        modelBuilder.Entity<Item>()
            .HasDiscriminator<string>(i => i.ItemType)
            .HasValue<Weapon>("Weapon")
            .HasValue<Armor>("Armor")
            .HasValue<Consumable>("Consumable")
            .HasValue<KeyItem>("KeyItem");

        // ============================================
        // Container <-> Item relationship (one-to-many)
        // ============================================
        // Each item has exactly ONE current container. Each container has many items.
        // This is NOT many-to-many - an item can't be in two containers at once.
        // See the README for a discussion of why items are instances, not types.
        modelBuilder.Entity<Item>()
            .HasOne(i => i.Container)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.ContainerId)
            .OnDelete(DeleteBehavior.SetNull);

        // ============================================
        // Player -> Inventory / Equipment (one-way)
        // ============================================
        // Player holds the FKs; Inventory and Equipment don't back-reference the Player.
        // This avoids a duplicate PlayerId column in the TPH Containers table.
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Inventory)
            .WithMany()
            .HasForeignKey(p => p.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Player>()
            .HasOne(p => p.Equipment)
            .WithMany()
            .HasForeignKey(p => p.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================
        // Monster -> MonsterLoot (NEW in Week 13)
        // ============================================
        modelBuilder.Entity<Monster>()
            .HasOne(m => m.Loot)
            .WithMany()
            .HasForeignKey(m => m.LootId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================
        // Room self-referencing navigation (NEW in Week 14)
        // ============================================
        // Each room points to at most one other Room in each cardinal direction.
        // DeleteBehavior.Restrict prevents cascading deletes from tangling the
        // graph if a room is removed.
        modelBuilder.Entity<Room>()
            .HasOne(r => r.NorthRoom)
            .WithMany()
            .HasForeignKey(r => r.NorthRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.SouthRoom)
            .WithMany()
            .HasForeignKey(r => r.SouthRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.EastRoom)
            .WithMany()
            .HasForeignKey(r => r.EastRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.WestRoom)
            .WithMany()
            .HasForeignKey(r => r.WestRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================
        // Player/Monster -> current Room (NEW in Week 14)
        // ============================================
        modelBuilder.Entity<Player>()
            .HasOne(p => p.CurrentRoom)
            .WithMany()
            .HasForeignKey(p => p.CurrentRoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Monster>()
            .HasOne(m => m.CurrentRoom)
            .WithMany()
            .HasForeignKey(m => m.CurrentRoomId)
            .OnDelete(DeleteBehavior.SetNull);

        // ============================================
        // Door entity (NEW in Week 14)
        // ============================================
        // Doors are NOT Containers (they don't hold items). They're a separate
        // entity with foreign keys to both connected rooms. Each Door is ONE
        // row, regardless of which side the player approaches from.
        modelBuilder.Entity<Door>()
            .HasOne(d => d.RoomA)
            .WithMany()
            .HasForeignKey(d => d.RoomAId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Door>()
            .HasOne(d => d.RoomB)
            .WithMany()
            .HasForeignKey(d => d.RoomBId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
