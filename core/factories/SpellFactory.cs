using System;
using HoardSurvivor3._0.Core.Enums;
using HoardSurvivor3._0.Features.Spells.Base;

namespace Core.Factories
{
    public static class SpellFactory
    {
        public static ISpell CreateSpell(SpellType spellType)
        {
            switch (spellType)
            {
                case SpellType.Fireball:
                    return new FireballSpell();
                default:
                    throw new ArgumentException("Invalid spell type");
            }
        }
    }
}