using System.Collections.Generic;

namespace HoardSurvivor3.0.Features.Player.Characters.Types
{
    public class Wizgod : Base.Character
    {
        public Wizgod() : base(
            "Wizgod", 
            new CharacterStats(
                maxHealth: 150f,
                moveSpeed: 5f,
                spellPower: 1.2f,
                cooldownReduction: 0.1f,
                critChanceBonus: 0.05f,
                critDamageBonus: 0.2f,
                areaOfEffectBonus: 0.15f
            ))
        {
        }

        protected override void InitializeStartingSpells()
        {
            // Add starting spells for Wizgod
            AddSpell(new FireballSpell());
        }
    }
}