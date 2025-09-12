using Godot;

public partial class SharedXPManager : Node
{
    private static SharedXPManager _instance;
    public static SharedXPManager Instance => _instance;
    
    private Node _gameData;
    
    // Signals that match the GDScript signals
    [Signal] public delegate void SharedXpGainedEventHandler(int amount, int totalXp);
    [Signal] public delegate void SharedLevelUpEventHandler(int newLevel);
    [Signal] public delegate void SharedXpChangedEventHandler(int currentXp, int xpToNext, int level);
    
    public override void _Ready()
    {
        _instance = this;
        
        GD.Print("SharedXPManager starting up...");
        
        // Get reference to GameData singleton
        _gameData = GetNode("/root/GameData");
        
        if (_gameData != null)
        {
            // Connect to GameData signals
            _gameData.Connect("shared_xp_gained", new Callable(this, nameof(OnSharedXpGained)));
            _gameData.Connect("shared_level_up", new Callable(this, nameof(OnSharedLevelUp)));
            _gameData.Connect("shared_xp_changed", new Callable(this, nameof(OnSharedXpChanged)));
            
            GD.Print("SharedXPManager connected to GameData signals");
        }
        else
        {
            GD.PrintErr("Could not find GameData singleton!");
        }
    }
    
    public void GainSharedXp(int amount)
    {
        if (_gameData != null)
        {
            GD.Print($"SharedXPManager: Gaining {amount} shared XP");
            _gameData.Call("gain_shared_xp", amount);
            
            // Also broadcast via RPC to sync with other players
            Rpc(nameof(SyncSharedXpGain), amount);
        }
        else
        {
            GD.PrintErr("SharedXPManager: Cannot gain XP - GameData not available");
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SyncSharedXpGain(int amount)
    {
        if (_gameData != null)
        {
            _gameData.Call("gain_shared_xp", amount);
        }
    }
    
    public Godot.Collections.Dictionary GetSharedXpProgress()
    {
        if (_gameData != null)
        {
            return _gameData.Call("get_shared_xp_progress").AsGodotDictionary();
        }
        
        // Fallback if GameData is not available
        var fallback = new Godot.Collections.Dictionary();
        fallback["current_xp"] = 0;
        fallback["xp_to_next_level"] = 100;
        fallback["current_level"] = 1;
        fallback["xp_percentage"] = 0.0f;
        return fallback;
    }
    
    public void ResetSharedProgression()
    {
        if (_gameData != null)
        {
            _gameData.Call("reset_shared_progression");
        }
    }
    
    // Signal relay methods
    private void OnSharedXpGained(int amount, int totalXp)
    {
        EmitSignal(SignalName.SharedXpGained, amount, totalXp);
    }
    
    private void OnSharedLevelUp(int newLevel)
    {
        EmitSignal(SignalName.SharedLevelUp, newLevel);
    }
    
    private void OnSharedXpChanged(int currentXp, int xpToNext, int level)
    {
        EmitSignal(SignalName.SharedXpChanged, currentXp, xpToNext, level);
    }
}