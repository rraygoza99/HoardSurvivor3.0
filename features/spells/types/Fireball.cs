using HoardSurvivor3._0.Features.Spells.Base;

public class FireballSpell : ISpell
    {
        public string Name => "Fireball";
        public string Description => "Launches a ball of fire that explodes on impact.";
        public float Damage { get; private set; }
        public float Cooldown { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float Size { get; private set; }
        public float CurrentCooldown { get; private set; }

        public FireballSpell()
        {
            Damage = 25f;
            Cooldown = 3f;
            CritChance = 0.1f;
            CritDamage = 1.5f;
            Size = 2f;
            CurrentCooldown = 0f;
        }

        public void Cast()
        {
            // Implementation for casting fireball
            CurrentCooldown = Cooldown;
        }

        public bool CanCast()
        {
            return CurrentCooldown <= 0;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown -= deltaTime;
                if (CurrentCooldown < 0) CurrentCooldown = 0;
            }
        }
    }