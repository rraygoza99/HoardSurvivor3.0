using System;
using System.Collections.Generic;

namespace HoardSurvivor3.0.Features.Spells
{
    public class SpellManager
    {
        private Dictionary<string, List<ISpell>> characterSpells;

        public SpellManager()
        {
            characterSpells = new Dictionary<string, List<ISpell>>();
            InitializeSpells();
        }

        private void InitializeSpells()
        {
            // Example spells for each character type
            characterSpells["Wizgod"] = new List<ISpell>
            {
                new FireballSpell(),
            };
        }

        public List<ISpell> GetSpellsForCharacter(string characterType)
        {
            if (characterSpells.ContainsKey(characterType))
            {
                return characterSpells[characterType];
            }
            return new List<ISpell>();
        }

        public void AddSpellToCharacter(string characterType, ISpell spell)
        {
            if (!characterSpells.ContainsKey(characterType))
            {
                characterSpells[characterType] = new List<ISpell>();
            }
            characterSpells[characterType].Add(spell);
        }
    }
}