using System.Collections.Generic;
using Godot;
using HoardSurvivor3._0.features.characters.@base;

namespace HoardSurvivor3._0.Features.Player.Characters.Types
{
    public class Wizgod : Base.Character
    {
        public Wizgod() : base(
            "Wizgod", 
            new CharacterStats(
                150f,    // MaxHealth
                150f,    // CurrentHealth
                5f,      // MoveSpeed
                1.2f,    // SpellPower
                0.1f,    // CooldownReduction
                0.05f,   // CritChanceBonus
                0.2f,    // CritDamageBonus
                0.15f    // AreaOfEffectBonus
            )
        )
        {
        }
        protected override void InitializeStartingSpells()
        {
            // Add starting spells for Wizgod
            
        }
    }
}