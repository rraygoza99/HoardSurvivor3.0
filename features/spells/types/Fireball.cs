public class FireballSpell : Spell
{
    public FireballSpell() : base(
        name: "Fireball",
        description: "A fiery projectile that explodes on impact.",
        cooldown: 5f,
        critChance: 0.1f,
        critDamage: 1.5f,
        size: 1f
    )
    {
    }

    public override void Cast()
    {
        // Logic for casting the fireball spell
        Console.WriteLine($"{Name} has been cast, dealing {Damage} damage!");
        CurrentCooldown = Cooldown;
    }
}