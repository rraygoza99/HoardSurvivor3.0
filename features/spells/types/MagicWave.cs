using Godot;
using HoardSurvivor3._0.Features.Spells.Base;
using System;
using System.Linq;
using HoardSurvivor3._0.Core.Enums;
namespace HoardSurvivor3._0.Features.Spells
{
    public partial class MagicWaveSpell : ISpell
    {
        public SpellType SpellType => SpellType.MagicWave;
        public string Name => "Magic Wave";
        public string Description => "Emits a wave of magical energy that damages all enemies in its path.";
        public float Damage { get; private set; }
        public float Cooldown { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float CurrentCooldown { get; private set; }
        public float WaveSpeed { get; private set; }
        public float WaveWidth { get; private set; }
        public float Size { get; private set; }

        public float ProjectileSpeed { get;  private set; }


        public MagicWaveSpell()
        {
            Damage = 30f;
            Cooldown = 5f;
            CritChance = 0.15f;
            CritDamage = 2.0f;
            CurrentCooldown = 0f;
            WaveSpeed = 3f;
            WaveWidth = 3f;
            Size = 1f;
            ProjectileSpeed = 15f;
        }

        public void Cast()
        {
            // Implementation for casting magic wave
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
    public partial class MagicWave : Area3D
    {
        private Vector3 _direction;
        private float _damage = 25f;
        private float _lifetime = 5f; // To prevent it from flying forever
        private float _speed = 5f; // Default speed
        private bool _isActive = false;
        private int _enemiesHit = 0;
        private const int MAX_ENEMIES_HIT = 3;
        private System.Collections.Generic.HashSet<Node> _hitEnemies = new();
        private int _ownerPeerId = 0;
        [Export] private float _forwardRotationOffsetDegrees = 0f; // Use 180 if your mesh faces +Z instead of -Z

        public void Initialize(float damage, float speed, Vector3 direction, Vector3 startPosition, int ownerPeerId = 0)
        {
            _damage = damage;
            _speed = speed;
            _direction = direction;
            GlobalPosition = startPosition;
            _ownerPeerId = ownerPeerId;
            _isActive = true;
            _enemiesHit = 0;
            _hitEnemies.Clear();
            SetProcess(true);
            Show(); // Make sure the wave is visible when active
            AlignToDirection();
        }

        public void Reset()
        {
            _isActive = false;
            _lifetime = 5f;
            _direction = Vector3.Zero;
            _enemiesHit = 0;
            _hitEnemies.Clear();
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
            // Keep visuals aligned with travel direction
            AlignToDirection();
            
            // Return to pool if lifetime expired or hit maximum enemies
            if (_lifetime <= 0 || _enemiesHit >= MAX_ENEMIES_HIT)
            {
                ReturnToPool();
            }
        }

        private void AlignToDirection()
        {
            var dir = _direction;
            if (dir.Length() < 0.0001f) return;
            // Lock to horizontal plane to avoid tilting
            dir.Y = 0;
            if (dir.Length() < 0.0001f) return;
            LookAt(GlobalPosition + dir, Vector3.Up);
            if (Mathf.Abs(_forwardRotationOffsetDegrees) > 0.001f)
            {
                RotateY(Mathf.DegToRad(_forwardRotationOffsetDegrees));
            }
        }

        private void ReturnToPool()
        {
            _isActive = false;
            SpellProjectilePool.Instance?.ReturnMagicWave(this);
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
            // Only the projectile owner applies damage to avoid duplicate hits
            if (!IsMultiplayerAuthority()) return;
    
            if (body.IsInGroup("enemies"))
            {
                // Check if we've already hit this enemy (prevent double-hitting)
                if (_hitEnemies.Contains(body))
                {
                    return;
                }
                
                // Add enemy to hit list and increment counter
                _hitEnemies.Add(body);
                _enemiesHit++;
                
                GD.Print($"💥 Magic wave hit enemy {_enemiesHit}/{MAX_ENEMIES_HIT}: {body.Name} (Damage: {_damage})");
                if (body.HasMethod(nameof(CocoChaser.RpcTakeDamage)))
                {
                    body.Rpc(nameof(CocoChaser.RpcTakeDamage), _damage);
                }
                else
                {
                    body.Call("TakeDamage", _damage);
                }
                
                // Don't return to pool immediately - let it continue to hit more enemies
                // The _Process method will handle returning to pool when max enemies are hit
            }
        }
    }
}

