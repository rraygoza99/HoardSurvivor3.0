using Godot;

namespace SteamMultiplayer.features.characters.@base
{
    public class CharacterStats
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public float MoveSpeed { get; set; }
        public float SpellPower { get; set; } // Multiplier for spell damage
        public float CooldownReduction { get; set; } // 0-1 value
        public float CritChanceBonus { get; set; } // Added to spell's crit chance
        public float CritDamageBonus { get; set; } // Added to spell's crit damage multiplier
        public float AreaOfEffectBonus { get; set; } // Multiplier for spell size

        public CharacterStats(float maxHealth, float moveSpeed, float spellPower, 
                              float cooldownReduction, float critChanceBonus, 
                              float critDamageBonus, float areaOfEffectBonus)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            MoveSpeed = moveSpeed;
            SpellPower = spellPower;
            CooldownReduction = cooldownReduction;
            CritChanceBonus = critChanceBonus;
            CritDamageBonus = critDamageBonus;
            AreaOfEffectBonus = areaOfEffectBonus;
        }
    }
}