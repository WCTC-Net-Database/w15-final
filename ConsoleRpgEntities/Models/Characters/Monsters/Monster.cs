using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Containers;

namespace ConsoleRpgEntities.Models.Characters.Monsters
{
    public abstract class Monster : IMonster, ITargetable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Health { get; set; }
        public int AggressionLevel { get; set; }
        public string MonsterType { get; set; } = string.Empty;

        // ============================================================
        // LOOT (new in Week 13)
        // ============================================================
        // Each monster has a MonsterLoot container holding the items it
        // drops on defeat. The FK lives here on Monster so the relationship
        // is owned by the monster entity (same pattern as Player.InventoryId).
        public int? LootId { get; set; }
        public virtual MonsterLoot? Loot { get; set; }

        /// <summary>
        /// True once the player has looted this monster's corpse.
        /// </summary>
        public bool IsLooted { get; set; }

        // ============================================================
        // LOCATION (new in Week 14)
        // ============================================================
        // Monsters exist inside rooms, just like players. An optional
        // CurrentRoomId lets dungeon designers place monsters in specific
        // rooms so players encounter them while exploring.
        public int? CurrentRoomId { get; set; }
        public virtual Room? CurrentRoom { get; set; }

        protected Monster()
        {

        }

        public abstract void Attack(ITargetable target);

    }
}
