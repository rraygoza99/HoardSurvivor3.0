using System;
using Features.Player.Characters.Base;
using Core.Enums;

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