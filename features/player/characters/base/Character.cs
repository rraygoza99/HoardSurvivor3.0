using System.Collections.Generic;
using HoardSurvivor3._0.features.characters.@base;
using HoardSurvivor3._0.Features.Spells.Base;

namespace HoardSurvivor3._0.Features.Player.Characters.Base
{
    public abstract class Character : ICharacter
    {
        public string Name { get; protected set; }
        public CharacterStats Stats { get; protected set; }
        public List<ISpell> Spells { get; protected set; }

        protected Character(string name, CharacterStats stats)
        {
            Name = name;
            Stats = stats;
            Spells = new List<ISpell>();
            InitializeStartingSpells();
        }

        protected abstract void InitializeStartingSpells();

        public void AddSpell(ISpell spell)
        {
            Spells.Add(spell);
        }

        public void RemoveSpell(ISpell spell)
        {
            Spells.Remove(spell);
        }
        public virtual void UpdateSpellCooldowns(float deltaTime)
        {
            foreach (var spell in Spells)
            {
                spell.UpdateCooldown(deltaTime);
            }
        }
        public void CastSpell(int index)
        {
            if (index >= 0 && index < Spells.Count)
            {
                var spell = Spells[index];
                if (spell.CanCast())
                {
                    spell.Cast();
                }
            }
        }
        public void TakeDamage(int amount)
        {
            Stats.CurrentHealth -= amount;
            if (Stats.CurrentHealth < 0)
            {
                Stats.CurrentHealth = 0;
            }
        }
        public void Heal(int amount)
        {
            Stats.CurrentHealth += amount;
            if (Stats.CurrentHealth > Stats.MaxHealth)
            {
                Stats.CurrentHealth = Stats.MaxHealth;
            }
        }
        public string GetCharacterInfo()
        {
            return $"Name: {Name}, Health: {Stats.CurrentHealth}/{Stats.MaxHealth}, Move Speed: {Stats.MoveSpeed}";
        }
    }
}