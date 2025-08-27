namespace HoardSurvivor3._0.Features.Spells.Base
{
    public interface ISpell
    {
        string Name { get; }
        string Description { get; }
        float Damage { get; }
        float Cooldown { get; }
        float CritChance { get; }
        float CritDamage { get; }
        float Size { get; }
        float CurrentCooldown { get; }
        void Cast();
        bool CanCast();
        void UpdateCooldown(float deltaTime);
    }
}