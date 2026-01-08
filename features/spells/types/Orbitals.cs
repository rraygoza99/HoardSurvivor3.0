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
        private OrbitalsSpell _spell; // kept for authority side logic / future upgrades
        private PackedScene _projectileScene;

        // Cached parameter values for non-authority clients (so they don't need OrbitalsSpell instance)
        private float _damage;
        private int _projectileAmount;
        private float _projectileSpeed;
        private float _projectileRange;
        private bool _initialized;

        public void Initialize(OrbitalsSpell spell)
        {
            // Store the spell reference (authority side)
            _spell = spell;
            _damage = spell.Damage;
            _projectileAmount = spell.ProjectileAmount;
            _projectileSpeed = spell.ProjectileSpeed;
            _projectileRange = spell.ProjectileRange;
            CreateProjectiles();
        }

        // Used by RPC on remote peers to recreate visuals without needing OrbitalsSpell
        public void InitializeFromData(float damage, int projectileAmount, float projectileSpeed, float projectileRange, bool isAuthority)
        {
            _damage = damage;
            _projectileAmount = projectileAmount;
            _projectileSpeed = projectileSpeed;
            _projectileRange = projectileRange;
            CreateProjectiles(isAuthority);
        }

        private void CreateProjectiles(bool isAuthorityOverride = false)
        {
            if (_initialized) return;
            _projectileScene ??= GD.Load<PackedScene>("res://features/spells/types/OrbitalProjectile.tscn");
            if (_projectileScene == null)
            {
                GD.PrintErr("[Orbitals] Failed to load OrbitalProjectile scene.");
                return;
            }
            for (int i = 0; i < _projectileAmount; i++)
            {
                var orbitalProjectile = _projectileScene.Instantiate<OrbitalProjectile>();
                // Only the authority should process damage logic
                var isAuth = isAuthorityOverride || IsMultiplayerAuthority();
                orbitalProjectile.Initialize(_damage, isAuth);
                AddChild(orbitalProjectile);
            }
            _initialized = true;
            // Ensure processing runs on every peer so orbit animation updates locally
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            if (!_initialized) return;

            // Determine the parameters (authority may have a spell, others rely on cached values)
            var speed = _spell != null ? _spell.ProjectileSpeed : _projectileSpeed;
            var amount = _spell != null ? _spell.ProjectileAmount : _projectileAmount;
            var range = _spell != null ? _spell.ProjectileRange : _projectileRange;

            _angle += speed * (float)delta;

            for (int i = 0; i < GetChildCount(); i++)
            {
                if (GetChild(i) is Node3D orbital)
                {
                    float angleOffset = (Mathf.Tau / amount) * i;
                    float x = Mathf.Cos(_angle + angleOffset) * range;
                    float z = Mathf.Sin(_angle + angleOffset) * range;
                    orbital.Position = new Vector3(x, 0, z);
                }
            }
        }
    }
}