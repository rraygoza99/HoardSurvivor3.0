using Godot;

namespace HoardSurvivor3._0.Features.Spells.Data
{
    public struct SpellCastData
    {
        public Vector3 SpawnPosition;
        public Vector3 Direction;
        public float Damage;
        public float Speed;
        public string SpellType;
        public float Size; // For area of effect spells
        public float CritChance;
        public float CritDamage;

        public SpellCastData(string spellType, Vector3 spawnPosition, Vector3 direction, 
                           float damage, float speed, float size = 1f, 
                           float critChance = 0f, float critDamage = 1f)
        {
            SpellType = spellType;
            SpawnPosition = spawnPosition;
            Direction = direction;
            Damage = damage;
            Speed = speed;
            Size = size;
            CritChance = critChance;
            CritDamage = critDamage;
        }
    }
}