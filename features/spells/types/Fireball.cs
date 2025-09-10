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
            CurrentCooldown = 0f;
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

        public void Initialize(float damage, float speed, Vector3 direction)
        {
            _damage = damage;
            _speed = speed;
            _direction = direction;

        }

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            //SetDirectionToNearestEnemy();
        }

        public override void _Process(double delta)
        {
            Position += _direction * _speed * (float)delta;
            _lifetime -= (float)delta;
            if (_lifetime <= 0)
            {
                QueueFree();
            }
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
            if (body.IsInGroup("enemies"))
            {
                // Assuming the enemy has a method to take damage
                // body.Call("TakeDamage", _damage);
                GD.Print($"Fireball hit an enemy: {body.Name}");
                QueueFree(); // Destroy the fireball on impact
            }
        }
    }
}