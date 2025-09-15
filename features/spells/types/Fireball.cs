using Godot;
using System.Linq;
using HoardSurvivor3._0.Features.Spells.Base;

namespace HoardSurvivor3._0.Features.Spells
{
    public class FireballSpell : ISpell
    {
        public string Name => "Fireball";
        public string Description => "Launches a ball of fire that explodes on impact.";
        public float Damage { get; private set; }
        public float Cooldown { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float Size { get; private set; }
        public float CurrentCooldown { get; private set; }
        public float ProjectileSpeed { get; private set; }

        public FireballSpell()
        {
            Damage = 25f;
            Cooldown = 3f;
            CritChance = 0.1f;
            CritDamage = 1.5f;
            Size = 2f;
            CurrentCooldown = 3f;
            ProjectileSpeed = 5f;
        }

        public void Cast()
        {
            // Implementation for casting fireball
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

    public partial class Fireball : Area3D
    {
        private Vector3 _direction;
        private float _damage = 25f;
        private float _lifetime = 5f; // To prevent it from flying forever
        private float _speed = 5f; // Default speed
        private bool _isActive = false;
        private int _ownerPeerId = 0;

        public void Initialize(float damage, float speed, Vector3 direction, Vector3 startPosition, int ownerPeerId = 0)
        {
            _damage = damage;
            _speed = speed;
            _direction = direction;
            GlobalPosition = startPosition;
            _ownerPeerId = ownerPeerId;
            _isActive = true;
            SetProcess(true);
            
            if (direction != Vector3.Zero)
            {
                LookAt(GlobalPosition + direction);
                RotateY(Mathf.Pi); // Rotate 180 degrees on the Y axis
            }
        }

        public void Reset()
        {
            _isActive = false;
            _lifetime = 5f;
            _direction = Vector3.Zero;
            _ownerPeerId = 0;
            Hide();
            SetProcess(false);
            
        }

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            
            // Set projectile on layer 4 and detect enemies on layer 3
            SetCollisionLayerValue(4, true);   // Projectiles on layer 4
            SetCollisionMaskValue(1, true);    // Detect environment/boundaries
            SetCollisionMaskValue(3, true);    // Detect enemies
            SetCollisionMaskValue(2, false);   // Don't detect players
            
            //SetDirectionToNearestEnemy();
        }

        public override void _Process(double delta)
        {
            if (!_isActive) return;
            Position += _direction * _speed * (float)delta;
            _lifetime -= (float)delta;
            if (_lifetime <= 0)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            _isActive = false;
            SpellProjectilePool.Instance?.ReturnFireball(this);
        }

        private void SetDirectionToNearestEnemy()
        {
            var enemies = GetTree().GetNodesInGroup("enemies").Cast<Node3D>().ToList();
            Node3D nearestEnemy = null;
            var minDistance = float.MaxValue;

            if (enemies.Count == 0)
            {
                _direction = -GlobalTransform.Basis.Z; // Go forward if no enemies
                return;
            }

            foreach (var enemy in enemies)
            {
                var distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            if (nearestEnemy != null)
            {
                _direction = (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();
            }
            else
            {
                _direction = -GlobalTransform.Basis.Z; // Go forward if no enemies in range
            }
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
                        body.Rpc(nameof(CocoChaser.RpcTakeDamage), _damage);
                    }
                    else
                    {
                        body.Call("TakeDamage", _damage);
                    }
                    ReturnToPool(); // Return to pool instead of QueueFree()
                }
            }
        }
    }
}