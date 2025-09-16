using Godot;

public partial class SharedXPManager : Node
{
    private static SharedXPManager _instance;
    public static SharedXPManager Instance => _instance;
    
    private Node _gameData;
    
    // Upgrade selection tracking
    private Godot.Collections.Array<long> _playersWaitingForUpgrade = new();
    private bool _gameIsPaused = false;
    
    // Signals that match the GDScript signals
    [Signal] public delegate void SharedXpGainedEventHandler(int amount, int totalXp);
    [Signal] public delegate void SharedLevelUpEventHandler(int newLevel);
    [Signal] public delegate void SharedXpChangedEventHandler(int currentXp, int xpToNext, int level);
    [Signal] public delegate void ShowLevelUpScreenEventHandler(int newLevel); // New signal for level up screen
    [Signal] public delegate void ShowSpellSelectionScreenEventHandler(int newLevel); // New signal for spell selection
    [Signal] public delegate void AllPlayersSelectedUpgradesEventHandler(); // New signal when all players are ready
    
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
        
        // Start tracking upgrade selections for all connected players
        StartUpgradeSelectionPhase();
        
        if (newLevel == 5 || newLevel == 15 || newLevel == 30)
        {
            EmitSignal(SignalName.ShowSpellSelectionScreen, newLevel);
        }
        else
        {
            EmitSignal(SignalName.ShowLevelUpScreen, newLevel); // Trigger level up screen for all players
        }
    }
    
    private void StartUpgradeSelectionPhase()
    {
        _playersWaitingForUpgrade.Clear();
        
        // Get all connected players
        var connectedPeers = Multiplayer.GetPeers();
        
        // Add the local player (host)
        _playersWaitingForUpgrade.Add(Multiplayer.GetUniqueId());
        
        // Add all connected peers
        foreach (int peerId in connectedPeers)
        {
            _playersWaitingForUpgrade.Add(peerId);
        }
        
        GD.Print($"Starting upgrade selection phase for {_playersWaitingForUpgrade.Count} players: [{string.Join(", ", _playersWaitingForUpgrade)}]");
        
        // Pause the game
        PauseGame();
    }
    
    public void OnPlayerSelectedUpgrade(long playerId)
    {
        if (_playersWaitingForUpgrade.Contains(playerId))
        {
            _playersWaitingForUpgrade.Remove(playerId);
            GD.Print($"Player {playerId} selected upgrade. {_playersWaitingForUpgrade.Count} players remaining.");
            
            // Notify other players that this player selected an upgrade
            Rpc(nameof(SyncPlayerUpgradeSelection), playerId);
            
            // Check if all players have selected
            if (_playersWaitingForUpgrade.Count == 0)
            {
                AllPlayersReady();
            }
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SyncPlayerUpgradeSelection(long playerId)
    {
        if (_playersWaitingForUpgrade.Contains(playerId))
        {
            _playersWaitingForUpgrade.Remove(playerId);
            GD.Print($"Synced: Player {playerId} selected upgrade. {_playersWaitingForUpgrade.Count} players remaining.");
            
            // Check if all players have selected
            if (_playersWaitingForUpgrade.Count == 0)
            {
                AllPlayersReady();
            }
        }
    }
    
    private void AllPlayersReady()
    {
        GD.Print("All players have selected their upgrades! Resuming game...");
        ResumeGame();
        EmitSignal(SignalName.AllPlayersSelectedUpgrades);
    }
    
    private void PauseGame()
    {
        if (!_gameIsPaused)
        {
            GetTree().Paused = true;
            _gameIsPaused = true;
            GD.Print("Game paused for upgrade selection");
        }
    }
    
    private void ResumeGame()
    {
        if (_gameIsPaused)
        {
            GetTree().Paused = false;
            _gameIsPaused = false;
            GD.Print("Game resumed after upgrade selection");
        }
    }
    
    private void OnSharedXpChanged(int currentXp, int xpToNext, int level)
    {
        EmitSignal(SignalName.SharedXpChanged, currentXp, xpToNext, level);
    }
}