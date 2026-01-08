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
            // Only the (designated) authority instance should process hits.
            // Also enforce per-enemy hit cooldown.
            if (!_isAuthority || !body.IsInGroup("enemies") || _hitCooldowns.ContainsKey(body))
            {
                return;
            }

            if (!body.Visible)
            {
                return;
            }

            GD.Print($"Orbital hit an enemy: {body.Name}");

            // Fireball uses body.Rpc(nameof(CocoChaser.RpcTakeDamage), damage) so that damage
            // is always applied on the enemy's authoritative peer (usually the host) and then
            // replication (death, etc.) flows correctly. Mirror that behavior here instead of
            // body.Call("RpcTakeDamage") which only executes locally and can desync health.
            if (body.HasMethod("RpcTakeDamage"))
            {
                body.Rpc("RpcTakeDamage", _damage);
            }
            else if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", _damage);
            }

            // Start individual cooldown to avoid rapid multi-hits.
            _hitCooldowns[body] = HIT_COOLDOWN;
        }
    }
}