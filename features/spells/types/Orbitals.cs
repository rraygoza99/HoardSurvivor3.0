using Godot;
using HoardSurvivor3._0.Core.Enums;
using HoardSurvivor3._0.Features.Spells.Base;

namespace HoardSurvivor3._0.Features.Spells.Types
{
    // This class defines the data for the Orbitals spell.
    // It's used for the spell selection screen and to store the spell's properties.
    public class OrbitalsSpell : ISpell
    {
        public SpellType SpellType => SpellType.Orbitals;
        public string Name => "Orbitals";
        public string Description => "Passive: Summons orbiting projectiles that damage enemies on contact.";
        public float Damage { get; private set; }
        public float Cooldown { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float CurrentCooldown { get; private set; }
        public float Size { get; private set; }
        
        // Custom properties for the Orbitals spell
        public float ProjectileSpeed { get; private set; }
        public int ProjectileAmount { get; private set; }
        public float ProjectileRange { get; private set; }

        public OrbitalsSpell()
        {
            Damage = 15f;
            Cooldown = 9999f; // A large number to signify it's a passive, one-time activation
            CritChance = 0f;
            CritDamage = 1.5f;
            CurrentCooldown = 0f;
            Size = 0.3f;
            ProjectileAmount = 3;
            ProjectileSpeed = 1.5f; // This will be the orbit speed
            ProjectileRange = 2.5f; // This will be the orbit radius
        }

        public void Cast()
        {
            // This is called once to activate the passive effect
            CurrentCooldown = Cooldown;
        }

        public bool CanCast()
        {
            // Can only "cast" once to activate
            return CurrentCooldown <= 0;
        }

        public void UpdateCooldown(float deltaTime)
        {
            // No need to update cooldown for a passive spell in the traditional sense
        }
    }

    // This class is the Node3D that manages the orbiting projectiles.
    // It's instantiated by the PlayerController when the spell is learned.
    public partial class Orbitals : Node3D
    {
        private float _angle;
        private OrbitalsSpell _spell;
        private PackedScene _projectileScene;

        public void Initialize(OrbitalsSpell spell)
        {
            _spell = spell;
            _projectileScene = GD.Load<PackedScene>("res://features/spells/types/OrbitalProjectile.tscn");
            
            // Create the specified number of projectiles
            for (int i = 0; i < _spell.ProjectileAmount; i++)
            {
                var orbitalProjectile = _projectileScene.Instantiate<OrbitalProjectile>();
                orbitalProjectile.Initialize(_spell.Damage, IsMultiplayerAuthority());
                AddChild(orbitalProjectile);
            }
        }

        public override void _Process(double delta)
        {
            // Don't do anything if the spell data hasn't been set
            if (_spell == null) return;

            // Rotate the angle based on the projectile speed
            _angle += _spell.ProjectileSpeed * (float)delta;

            // Update the position of each orbital projectile
            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is Node3D orbital)
                {
                    float angleOffset = (Mathf.Tau / _spell.ProjectileAmount) * i;
                    float x = Mathf.Cos(_angle + angleOffset) * _spell.ProjectileRange;
                    float z = Mathf.Sin(_angle + angleOffset) * _spell.ProjectileRange;
                    orbital.Position = new Vector3(x, 0, z);
                }
            }
        }
    }
}