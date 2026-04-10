using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Characters;

namespace ConsoleRpgEntities.Models.Abilities.PlayerAbilities
{
    public class ShoveAbility : Ability
    {
        public int Damage { get; set; }
        public int Distance { get; set; }

        public override void Activate(IPlayer user, ITargetable target)
        {
            // Shove: push the target back and deal the shove's damage.
            // This is the concrete implementation of the abstract Activate
            // method declared on Ability. Each subclass decides its own effect.
            target.Health -= Damage;
            Console.WriteLine($"{user.Name} shoves {target.Name} back {Distance} feet, dealing {Damage} damage!");
        }
    }
}
