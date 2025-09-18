using Godot;
using System.Collections.Generic;

namespace HoardSurvivor3._0.Features.Spells.Types
{
    public partial class OrbitalProjectile : Area3D
    {
        private float _damage;
        private bool _isAuthority;
        private readonly Dictionary<Node, float> _hitCooldowns = new();
        private const float HIT_COOLDOWN = 1.0f; // 1 second cooldown per enemy

        public void Initialize(float damage, bool isAuthority)
        {
            _damage = damage;
            _isAuthority = isAuthority;
        }

        public override void _Ready()
        {
            // It's generally more reliable to connect this signal via the Godot Editor's Node panel.
            // Ensure `BodyEntered(Node3D body)` is connected to `OnBodyEntered`.
            GD.Print("Orbital Projectile Ready.");

            // Set projectile on layer 4 and detect enemies on layer 3
            SetCollisionLayerValue(4, true);   // This projectile IS on layer 4
            SetCollisionMaskValue(3, true);    // This projectile LOOKS FOR things on layer 3 (enemies)
        }

        public override void _Process(double delta)
        {
            // This only runs on the authoritative client
            if (!_isAuthority) return;

            // Cooldown logic for enemies that have been hit
            var keys = new List<Node>(_hitCooldowns.Keys);
            foreach (var enemy in keys)
            {
                _hitCooldowns[enemy] -= (float)delta;
                if (_hitCooldowns[enemy] <= 0)
                {
                    _hitCooldowns.Remove(enemy);
                }
            }
        }

        private void OnBodyEntered(Node3D body)
        {
            // Only the authority should process hits
            // Don't hit the same enemy if it's on cooldown
            if (!_isAuthority || !body.IsInGroup("enemies") || _hitCooldowns.ContainsKey(body))
            {
                return;
            }

            // The body is already confirmed to be a Node3D, so we just check visibility
            if (body.Visible)
            {
                GD.Print($"Orbital hit an enemy: {body.Name}");
                
                // Apply damage via RPC if available, otherwise call method directly
                if (body.HasMethod("TakeDamage"))
                {
                    body.Call("TakeDamage", _damage);
                }

                // Put the enemy on cooldown
                _hitCooldowns[body] = HIT_COOLDOWN;
            }
        }
    }
}