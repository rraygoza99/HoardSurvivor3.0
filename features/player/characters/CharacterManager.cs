using System;
using System.Collections.Generic;
using HoardSurvivor3;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Features.Player.Characters.Types;

namespace HoardSurvivor3.Features.Player.Characters
{
    public class CharacterManager
    {
        private Dictionary<string, System.Type> _characterTypes;

        public CharacterManager()
        {
            InitializeCharacterTypes();
        }

        private void InitializeCharacterTypes()
        {
            _characterTypes = new Dictionary<string, System.Type>
            {
                { "Wizgod", typeof(Wizgod) },
            };
        }

        public List<string> GetAvailableCharacters()
        {
            return new List<string>(_characterTypes.Keys);
        }

        public Character CreateCharacter(string characterType)
        {
            if (_characterTypes.TryGetValue(characterType, out System.Type type))
            {
                return System.Activator.CreateInstance(type) as Character;
            }

            return null;
        }
    }
}