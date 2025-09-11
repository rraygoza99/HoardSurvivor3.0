using Godot;
using System.Collections.Generic;
using HoardSurvivor3._0.Features.Spells;

public partial class SpellProjectilePool : Node
{
    private static SpellProjectilePool _instance;
    public static SpellProjectilePool Instance => _instance;

    private Queue<Fireball> _fireballPool = new();
    private PackedScene _fireballScene;
    private const int POOL_SIZE = 20;

    public override void _Ready()
    {
        _instance = this;
        _fireballScene = GD.Load<PackedScene>("res://features/spells/types/Fireball.tscn");

        // Pre-populate pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var fireball = _fireballScene.Instantiate<Fireball>();
            fireball.SetProcess(false);
            fireball.Hide();
            _fireballPool.Enqueue(fireball);
            AddChild(fireball);
        }
    }
    public Fireball GetFireball()
    {
        if (_fireballPool.Count > 0)
        {
            var fireball = _fireballPool.Dequeue();
            fireball.Show();
            fireball.SetProcess(true);
            return fireball;
        }
        return _fireballScene.Instantiate<Fireball>();
    }
    public void ReturnFireball(Fireball fireball)
    {
        fireball.Hide();
        fireball.SetProcess(false);
        fireball.Reset(); // Reset fireball state
        _fireballPool.Enqueue(fireball);
    }
}