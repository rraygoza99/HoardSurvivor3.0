using Godot;
using System.Linq;
using HoardSurvivor3._0.Features.Spells.Base;
using HoardSurvivor3._0.Core.Enums;
using System.Security.Cryptography.X509Certificates;

namespace HoardSurvivor3._0.Features.Spells
{
    public class OrbitalsSpell : ISpell
    {
        public SpellType SpellType => SpellType.Orbitals;
        public string Name => "Orbitals";
        public string Description => "Summons orbiting projectiles that damage enemies on contact.";
        public float Damage { get; private set; }
        public float Cooldown { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float CurrentCooldown { get; private set; }
        public float Size { get; private set; }
        public float ProjectileSpeed { get; private set; }
        public int NumberOfOrbitals { get; private set; }
        public float OrbitRadius { get; private set; }
        public float OrbitSpeed { get; private set; }

        public OrbitalsSpell()
        {
            Damage = 20f;
            Cooldown = 8f;
            CritChance = 0.1f;
            CritDamage = 1.5f;
            CurrentCooldown = 0f;
            Size = 0.5f;
            ProjectileSpeed = 10f;
            NumberOfOrbitals = 3;
            OrbitRadius = 2f;
            OrbitSpeed = 1f;
        }

        public void Cast()
        {
            // Implementation for casting orbitals
            CurrentCooldown = Cooldown;
        }

        public bool CanCast()
        {
            return CurrentCooldown <= 0;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown -= deltaTime;
                if (CurrentCooldown < 0) CurrentCooldown = 0;
            }
        }

    }
    
    public partial class Orbitals : Node3D
    {
        private float _angle;
        private float _orbitRadius = 2f;
        private float _orbitSpeed = 1f;
        private int _numberOfOrbitals = 3;
        private float _lifetime = 10f; // To prevent them from orbiting forever
        private float _elapsedTime = 0f;
        private bool _isActive = false;
        private int _ownerPeerId = 0;

        public override void _Process(double delta)
        {
            if (_isActive)
            {
                _elapsedTime += (float)delta;
                if (_elapsedTime >= _lifetime)
                {
                    QueueFree();
                    return;
                }

                _angle += _orbitSpeed * (float)delta;
                for (int i = 0; i < GetChildCount(); i++)
                {
                    var orbital = GetChild(i) as MeshInstance3D;
                    if (orbital != null)
                    {
                        float angleOffset = (Mathf.Tau / _numberOfOrbitals) * i;
                        float x = Mathf.Cos(_angle + angleOffset) * _orbitRadius;
                        float z = Mathf.Sin(_angle + angleOffset) * _orbitRadius;
                        orbital.Position = new Vector3(x, 0, z);
                    }
                }
            }
        }

        public void Activate(int ownerPeerId, float orbitRadius, float orbitSpeed, int numberOfOrbitals, float damage)
        {
            _ownerPeerId = ownerPeerId;
            _orbitRadius = orbitRadius;
            _orbitSpeed = orbitSpeed;
            _numberOfOrbitals = numberOfOrbitals;

            for (int i = 0; i < _numberOfOrbitals; i++)
            {
                var orbital = new OrbitalProjectile();
                orbital.Initialize(damage, ownerPeerId);
                var mesh = new MeshInstance3D();
                mesh.Mesh = new SphereMesh() { Radius = 0.2f };
                orbital.AddChild(mesh);
                AddChild(orbital);
            }

            _isActive = true;
        }

        private void OnBodyEntered(Node body)
        {
            if (!_isActive) return;
            // Only the projectile owner applies damage to avoid duplicate hits across peers
            if (!IsMultiplayerAuthority()) return;
    
            if (body.IsInGroup("enemies"))
            {
                if (body is Node3D node3D && node3D.Visible)
                {
                    GD.Print($"Fireball hit an enemy: {body.Name}");
                    // Prefer authoritative RPC if available
                    if (body.HasMethod(nameof(CocoChaser.RpcTakeDamage)))
                    {
                        //body.Rpc(nameof(CocoChaser.RpcTakeDamage), _damage);
                    }
                    else
                    {
                        //body.Call("TakeDamage", _damage);
                    }
                    //ReturnToPool(); // Return to pool instead of QueueFree()
                }
            }
        }
    }
}