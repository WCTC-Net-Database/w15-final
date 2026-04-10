using ConsoleRpgEntities.Models.Attributes;

namespace ConsoleRpgEntities.Models.Characters.Monsters
{
    /// <summary>
    /// Skeleton - An undead dungeon monster new in Week 15.
    ///
    /// Second example of extending the Monster TPH hierarchy. Skeletons
    /// have a "rattle" aggression check and deal modest damage that is
    /// boosted by their HasArmor flag.
    /// </summary>
    public class Skeleton : Monster
    {
        /// <summary>
        /// Whether this skeleton is wearing scavenged armor.
        /// Armored skeletons deal more damage and are harder to hurt.
        /// </summary>
        public bool HasArmor { get; set; }

        public override void Attack(ITargetable target)
        {
            int damage = HasArmor ? 8 : 5;
            target.Health -= damage;
            Console.WriteLine($"{Name} rattles forward and strikes for {damage} damage!");
        }
    }
}
