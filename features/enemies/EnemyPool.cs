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
            chaser.Visible = false;
            _chaserPool.Enqueue(chaser);
            AddChild(chaser);
        }

        GD.Print($"EnemyPool initialized with {POOL_SIZE} chaser enemies");
    }

    public CocoChaser GetChaser()
    {
        CocoChaser chaser;
        
        if (_chaserPool.Count > 0)
        {
            chaser = _chaserPool.Dequeue();
            chaser.Show();
            chaser.Visible = true;
            chaser.SetProcess(true);
            chaser.SetPhysicsProcess(true);
            
            // Reset velocity to prevent crazy values when retrieved from pool
            chaser.Velocity = Vector3.Zero;
            
            // Setup collision layers - enemies on layer 3, collide with ground and other enemies
            // This allows players to walk through enemies while enemies can still pathfind
            chaser.SetCollisionLayerValue(1, false);  // Not on ground layer
            chaser.SetCollisionLayerValue(2, false);  // Not on player layer
            chaser.SetCollisionLayerValue(3, true);   // On enemy layer
            chaser.SetCollisionMaskValue(1, true);    // Collide with ground/environment
            chaser.SetCollisionMaskValue(2, false);   // Don't physically interact with players
            chaser.SetCollisionMaskValue(3, true);    // DO collide with other enemies to prevent stacking
            
            // Add back to enemies group for targeting (Reset() will also do this, but make sure)
            chaser.AddToGroup("enemies");
            
            GD.Print($"Retrieved chaser from pool. Pool count: {_chaserPool.Count}");
        }
        else
        {
            // Pool is empty, create new instance
            chaser = _chaserScene.Instantiate<CocoChaser>();
            AddChild(chaser);
            GD.Print("Pool empty, created new chaser instance");
        }

        _activeEnemyCount++;
        
        // Make sure the enemy is properly initialized
        chaser.CallDeferred("add_to_group", "enemies");
        
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
        
        // Reset velocity before calling Reset() to prevent crazy values
        chaser.Velocity = Vector3.Zero;
        chaser.Reset(); // Reset chaser state
        
        GD.Print($"Enemy disabled and moved to storage location: {chaser.GlobalPosition}");
        
        // Only return to pool if we have space
        if (_chaserPool.Count < POOL_SIZE)
        {
            _chaserPool.Enqueue(chaser);
            GD.Print($"Returned chaser to pool. Pool count: {_chaserPool.Count}");
        }
        else
        {
            // Pool is full, actually destroy the enemy
            chaser.QueueFree();
            GD.Print("Pool full, destroyed excess chaser");
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
        GD.Print("Enemy pool cleared");
    }

    public override void _ExitTree()
    {
        _instance = null;
    }
}
