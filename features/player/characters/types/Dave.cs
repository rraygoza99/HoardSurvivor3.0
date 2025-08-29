using System;
using HoardSurvivor3._0.features.characters.@base;
namespace HoardSurvivor3._0.Features.Player.Characters.Types
{
    public class Dave : Base.Character
    {
        public Dave() : base(
            "Dave",
            new CharacterStats(
                200f,    // MaxHealth
                3.5f,    // MoveSpeed
                1.0f,    // SpellPower
                0.15f,   // CooldownReduction
                0.1f,    // CritChanceBonus
                0.25f,   // CritDamageBonus
                0.1f,
                0.3f     // AreaOfEffectBonus
            )
        )
        {
        }
        protected override void InitializeStartingSpells()
        {
            // Add starting spells for Dave
            
        }
    }
}
