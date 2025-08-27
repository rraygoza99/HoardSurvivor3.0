using System.Collections.Generic;
using SteamMultiplayer.features.characters.@base;

namespace HoardSurvivor3._0.Features.Player.Characters.Base
{
    public abstract class Character : ICharacter
    {
        public string Name { get; protected set; }
        public CharacterStats Stats { get; protected set; }
        public List<ISpell> Spells { get; protected set; }

        protected Character(string name, Charactertats stats)
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
        public virtual void UpdateSpellCooldowns(float deltaTime){
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
    }
}