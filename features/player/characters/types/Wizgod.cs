using System.Collections.Generic;
using Godot;
using HoardSurvivor3._0.features.characters.@base;
using HoardSurvivor3._0.Features.Spells;
using HoardSurvivor3._0.Features.Spells.Base;

namespace HoardSurvivor3._0.Features.Player.Characters.Types
{
    public class Wizgod : Base.Character
    {
        public Wizgod() : base(
            "Wizgod", 
            new CharacterStats(
                150f,    // MaxHealth
                4f,      // MoveSpeed
                1.2f,    // SpellPower
                0.1f,    // CooldownReduction
                0.05f,   // CritChanceBonus
                0.2f,    // CritDamageBonus
                0.15f,
                .5f    // AreaOfEffectBonus
            )
        )
        {
        }
        protected override void InitializeStartingSpells()
        {
            Spells.Add(new FireballSpell());
        }
    }
}