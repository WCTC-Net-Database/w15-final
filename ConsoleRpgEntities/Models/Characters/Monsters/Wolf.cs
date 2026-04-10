using ConsoleRpgEntities.Models.Attributes;

namespace ConsoleRpgEntities.Models.Characters.Monsters
{
    /// <summary>
    /// Wolf - A wilderness monster new in Week 15.
    ///
    /// Demonstrates extending the Monster TPH hierarchy: one new class,
    /// one new discriminator value in GameContext, one seed row, zero
    /// changes to existing Monster or Goblin code.
    /// </summary>
    public class Wolf : Monster
    {
        /// <summary>
        /// Wolves hunt in packs. The PackSize field tracks how many others
        /// hunt with this wolf, which boosts its attack damage below.
        /// </summary>
        public int PackSize { get; set; }

        public override void Attack(ITargetable target)
        {
            // Pack damage: base 4 + 2 per pack member (capped at 12)
            int damage = Math.Min(4 + (PackSize * 2), 12);
            target.Health -= damage;
            Console.WriteLine(
                PackSize > 0
                    ? $"{Name} howls and lunges with {PackSize} packmates for {damage} damage!"
                    : $"{Name} lunges at {target.Name} for {damage} damage!");
        }
    }
}
