using System;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Core.Enums;
using HoardSurvivor3._0.Features.Player.Characters.Types;

namespace Core.Factories
{
    public static class CharacterFactory
    {
        public static ICharacter CreateCharacter(CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Wizgod:
                    return new Wizgod();
                default:
                    throw new ArgumentException("Invalid character type");
            }
        }
    }
}