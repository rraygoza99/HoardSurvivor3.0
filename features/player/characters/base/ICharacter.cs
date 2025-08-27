namespace HoardSurvivor3._0.Features.Player.Characters.Base
{
    public interface ICharacter
    {
        CharacterStats Stats { get; }
        void CastSpell(string spellName);
        void TakeDamage(int amount);
        void Heal(int amount);
        string GetCharacterInfo();
    }
}