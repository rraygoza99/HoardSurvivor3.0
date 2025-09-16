using HoardSurvivor3._0.Core.Enums;

namespace HoardSurvivor3._0.Features.Spells.Base
{
    public interface ISpell
    {
        SpellType SpellType { get; }
        string Name { get; }
        string Description { get; }
        float Damage { get; }
        float Cooldown { get; }
        float CritChance { get; }
        float CritDamage { get; }
        float Size { get; }
        float CurrentCooldown { get; }
        float ProjectileSpeed { get; }
        void Cast();
        bool CanCast();
        void UpdateCooldown(float deltaTime);
    }
}