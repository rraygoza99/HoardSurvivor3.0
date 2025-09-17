using Godot;
using System.Collections.Generic;

public partial class OrbitalProjectile : Area3D
{
    private float _damage;
    private int _ownerPeerId;
    private Dictionary<Node, float> _hitCooldowns = new();
    private const float HIT_COOLDOWN = 1.0f; // 1 second cooldown per enemy

    public void Initialize(float damage, int ownerPeerId)
    {
        _damage = damage;
        _ownerPeerId = ownerPeerId;
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        // Update cooldowns
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

    private void OnBodyEntered(Node body)
    {
        if (!GetParent().GetParent<Node3D>().IsMultiplayerAuthority() || !body.IsInGroup("enemies") || _hitCooldowns.ContainsKey(body))
        {
            return;
        }

        if (body is Node3D node3D && node3D.Visible)
        {
            GD.Print($"Orbital hit an enemy: {body.Name}");
            if (body.HasMethod("RpcTakeDamage"))
            {
                body.Rpc("RpcTakeDamage", _damage);
            }
            else if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", _damage);
            }
            _hitCooldowns[body] = HIT_COOLDOWN;
        }
    }
}