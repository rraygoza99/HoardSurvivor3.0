namespace HoardSurvivor3._0.Features.Spells.Base
{
    public abstract class Spell
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Cooldown {get; protected set;}
        public float CritChance { get; protected set; }
        public float CritDamage { get; protected set; }
        public float Size { get; protected set; }
        public float CurrentCooldown { get; protected set; }

        protected Spell(string name, string description, float cooldown, float critChance, float critDamage, float size)
        {
            Name = name;
            Description = description;
            Cooldown = cooldown;
            CritChance = critChance;
            CritDamage = critDamage;
            Size = size;
            CurrentCooldown = 0;
        }

        public abstract void Cast();

        public virtual bool CanCast()
        {
            return CurrentCooldown <= 0;
        }
        public virtual void UpdateCooldown(float deltaTime)
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown -= deltaTime;
                if (CurrentCooldown < 0)
                {
                    CurrentCooldown = 0;
                }
            }
        }
    }
}