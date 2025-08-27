using System;
using System.Collections.Generic;

namespace HoardSurvivor3.0.Features.Player.Characters
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