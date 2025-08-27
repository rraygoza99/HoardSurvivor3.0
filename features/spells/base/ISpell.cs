namespace HoardSurvivor3._0.Features.Spells.Base
{
    public interface ISpell
    {
        string Name { get; }
        void Cast();
    }
}