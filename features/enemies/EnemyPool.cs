using Godot;
using System.Collections.Generic;

public partial class EnemyPool : Node
{
    private static EnemyPool _instance;
    public static EnemyPool Instance => _instance;

    private Queue<CocoChaser> _chaserPool = new();
    private PackedScene _chaserScene;
    private const int POOL_SIZE = 25;
    private int _activeEnemyCount = 0;

    public int ActiveEnemyCount => _activeEnemyCount;
    public int PooledEnemyCount => _chaserPool.Count;

    public override void _Ready()
    {
        _instance = this;
        _chaserScene = GD.Load<PackedScene>("res://features/enemies/chaser_enemy/CocoChaser.tscn");

        // Pre-populate pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var chaser = _chaserScene.Instantiate<CocoChaser>();
            chaser.SetProcess(false);
            chaser.SetPhysicsProcess(false);
            chaser.Hide();
            chaser.RemoveFromGroup("enemies");
            chaser.Visible = false;
            _chaserPool.Enqueue(chaser);
            AddChild(chaser);
        }
    }

    public CocoChaser GetChaser()
    {
        CocoChaser chaser;
        
        if (_chaserPool.Count > 0)
        {
            chaser = _chaserPool.Dequeue();
            chaser.Reset(); // Reset state for reuse
            chaser.Show();
            chaser.Visible = true;
            chaser.SetProcess(true);
            chaser.SetPhysicsProcess(true);
        }
        else
        {
            // Pool is empty, create new instance
            chaser = _chaserScene.Instantiate<CocoChaser>();
            AddChild(chaser);
        }

        _activeEnemyCount++;
        
        
        // Make sure the enemy is properly initialized
        return chaser;
    }

    public void ReturnChaser(CocoChaser chaser)
    {
        if (chaser == null || chaser.IsQueuedForDeletion())
            return;

        // Completely disable the enemy while in pool
        chaser.Hide();
        chaser.Visible = false;
        chaser.SetProcess(false);
        chaser.SetPhysicsProcess(false);
        
        // Disable all collision while in pool
        chaser.SetCollisionLayerValue(1, false);
        chaser.SetCollisionLayerValue(2, false);
        chaser.SetCollisionLayerValue(3, false);
        chaser.SetCollisionMaskValue(1, false);
        chaser.SetCollisionMaskValue(2, false);
        chaser.SetCollisionMaskValue(3, false);
        
        // Remove from enemies group so it doesn't interfere with targeting
        chaser.RemoveFromGroup("enemies");
        
        // Move to a far-away location to prevent any interference
        chaser.GlobalPosition = new Vector3(10000, -1000, 10000);
        chaser.ClearTarget();
        
        // Only return to pool if we have space
        if (_chaserPool.Count < POOL_SIZE)
        {
            _chaserPool.Enqueue(chaser);
        }
        else
        {
            // Pool is full, actually destroy the enemy
            chaser.QueueFree();
        }

        _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
    }

    public void ClearPool()
    {
        while (_chaserPool.Count > 0)
        {
            var chaser = _chaserPool.Dequeue();
            chaser?.QueueFree();
        }
        _activeEnemyCount = 0;
    }

    public override void _ExitTree()
    {
        _instance = null;
    }
}
