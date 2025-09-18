using System;
using System.Collections.Generic;
using HoardSurvivor3._0.Core.Enums;
using HoardSurvivor3._0.Features.Spells;
using HoardSurvivor3._0.Features.Spells.Base;
using HoardSurvivor3._0.Features.Spells.Types;

namespace Core.Factories
{
    public static class SpellFactory
    {
        private static readonly Dictionary<SpellType, Func<ISpell>> SpellConstructors = new()
        {
            { SpellType.Fireball, () => new FireballSpell() },
            { SpellType.MagicWave, () => new MagicWaveSpell() },
            { SpellType.Orbitals, () => new OrbitalsSpell() }
        };

        public static ISpell CreateSpell(SpellType spellType)
        {
            if (SpellConstructors.TryGetValue(spellType, out var constructor))
            {
                return constructor();
            }
            throw new ArgumentException($"Invalid spell type: {spellType}");
        }

        public static List<ISpell> GetAllAvailableSpells()
        {
            var allSpells = new List<ISpell>();
            foreach (var constructor in SpellConstructors.Values)
            {
                allSpells.Add(constructor());
            }
            return allSpells;
        }
    }
}