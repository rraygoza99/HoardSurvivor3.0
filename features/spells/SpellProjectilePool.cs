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
    public Queue<MagicWave> _magicWavePool = new();
    private PackedScene _magicWaveScene;

    public override void _Ready()
    {
        _instance = this;
        _fireballScene = GD.Load<PackedScene>("res://features/spells/types/Fireball.tscn");
        _magicWaveScene = GD.Load<PackedScene>("res://features/spells/types/MagicWave.tscn");

        // Pre-populate pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var fireball = _fireballScene.Instantiate<Fireball>();
            fireball.SetProcess(false);
            fireball.Hide();
            _fireballPool.Enqueue(fireball);
            AddChild(fireball);
        }
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var magicWave = _magicWaveScene.Instantiate<MagicWave>();
            magicWave.SetProcess(false);
            magicWave.Hide();
            _magicWavePool.Enqueue(magicWave);
            AddChild(magicWave);
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
    public MagicWave GetMagicWave()
    {
        if (_magicWavePool.Count > 0)
        {
            var magicWave = _magicWavePool.Dequeue();
            magicWave.Show();
            magicWave.SetProcess(true);
            return magicWave;
        }
        return _magicWaveScene.Instantiate<MagicWave>();
    }
    public void ReturnMagicWave(MagicWave magicWave)
    {
        magicWave.Hide();
        magicWave.SetProcess(false);
        magicWave.Reset(); // Reset magic wave state
        _magicWavePool.Enqueue(magicWave);
    }
    

}