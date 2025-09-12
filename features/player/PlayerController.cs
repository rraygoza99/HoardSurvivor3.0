using Godot;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Features.Player.Characters.Types;
using HoardSurvivor3._0.Features.Spells.Base;
using HoardSurvivor3._0.Features.Spells;
using System.Collections.Generic;
using System.Linq;
using SteamMultiplayer.features.player;
using HoardSurvivor3._0.Features.Spells.Data;

public partial class PlayerController : CharacterBody3D
{
    [Export] private MultiplayerSynchronizer _synchonizer;
    [Export] private PlayerCamera _camera;
    [Export] private int _multiplayerAuthority;
    private Character character;
    private float moveSpeed;
    private float currentHealth;
    private AnimationTree _animationTree;
    private PlayerInputs _playerInputs;
    public Vector3 StartPosition { get; set; }
    [Export] private Node3D _playerModel;

    // Spell casting related fields
    private PackedScene _fireballScene;
    private List<ISpell> _spells;
    private Queue<SpellCastData> _pendingSpells = new();
    private float _rpcBatchTimer = 0f;
    private const float RPC_BATCH_INTERVAL = 0.1f;

    private Godot.Vector3 direction = Godot.Vector3.Zero;

    private bool canCast = true;
    public Area3D pickupArea;
    [ExportGroup("Player Stats")]
    // Individual XP properties are no longer used - keeping for compatibility
    [Export] public int CurrentXp { get; private set; } = 0;
    [Export] float XpGainMultiplier { get; set; } = 1.0f;
    [Export] public int XpToNextLevel { get; private set; } = 100;
    [Export] public int CurrentLevel { get; private set; } = 1;

    public int MultiplayerAuthority
    {
        get => _multiplayerAuthority;
        set
        {
            _multiplayerAuthority = value;
            SetMultiplayerAuthority(value);
        }
    }

    public void Initialize(Character selectedCharacter)
    {
        character = selectedCharacter;
        moveSpeed = character.Stats.MoveSpeed;
        currentHealth = character.Stats.MaxHealth;
    }
    public override void _Ready()
    {
        var isMultiplayerAuthority = IsMultiplayerAuthority();

        // Initialize velocity to zero to prevent crazy initial values
        Velocity = Vector3.Zero;

        // Initialize character based on scene name
        string nodeName = Name.ToString();
        string characterName = nodeName.Contains("_") ? nodeName.Split("_")[1] : "Wizgod";

        // Initialize the correct character based on the scene name
        switch (characterName.ToLower())
        {
            case "dave":
                Initialize(new Dave());
                break;
            case "alice":
                // Add Alice character class when implemented
                Initialize(new Wizgod()); // Fallback for now
                break;
            case "sam":
                // Add Sam character class when implemented
                Initialize(new Wizgod()); // Fallback for now
                break;
            case "carl":
                // Add Carl character class when implemented
                Initialize(new Wizgod()); // Fallback for now
                break;
            case "bern":
                // Add Bern character class when implemented
                Initialize(new Wizgod()); // Fallback for now
                break;
            case "wizgod":
            default:
                Initialize(new Wizgod());
                break;
        }

        SetProcess(isMultiplayerAuthority);
        SetPhysicsProcess(isMultiplayerAuthority);

        if (!isMultiplayerAuthority)
        {
            return;
        }
        _playerInputs = new PlayerInputs(this);
        _animationTree = GetNode<AnimationTree>("AnimationTree");
        _animationTree.Active = true;

        // Initialize spell casting
        _spells = character.Spells;
        _fireballScene = GD.Load<PackedScene>("res://features/spells/types/Fireball.tscn");

        var main = GetTree().Root.GetNode<Node>("Main");
        main.Connect("player_teleport", new Callable(this, MethodName.OnPlayerTeleport));
        
        // Connect to shared XP system
        if (SharedXPManager.Instance != null)
        {
            SharedXPManager.Instance.SharedXpChanged += OnSharedXpChanged;
            SharedXPManager.Instance.SharedLevelUp += OnSharedLevelUp;
            
            // Sync current values from shared system
            SyncWithSharedXP();
        }
        else
        {
            GD.Print("SharedXPManager not yet available, will try later");
            // Try again after a short delay
            CallDeferred(nameof(TryConnectToSharedXP));
        }
    }
    
    private void TryConnectToSharedXP()
    {
        if (SharedXPManager.Instance != null)
        {
            SharedXPManager.Instance.SharedXpChanged += OnSharedXpChanged;
            SharedXPManager.Instance.SharedLevelUp += OnSharedLevelUp;
            SyncWithSharedXP();
            GD.Print("Successfully connected to SharedXPManager");
        }
    }
    
    private void SyncWithSharedXP()
    {
        if (SharedXPManager.Instance != null)
        {
            var progress = SharedXPManager.Instance.GetSharedXpProgress();
            CurrentXp = progress["current_xp"].AsInt32();
            XpToNextLevel = progress["xp_to_next_level"].AsInt32();
            CurrentLevel = progress["current_level"].AsInt32();
            
            GD.Print($"Synced with shared XP: Level {CurrentLevel}, XP {CurrentXp}/{XpToNextLevel}");
        }
    }
    
    // Signal handlers for shared XP system
    private void OnSharedXpChanged(int currentXp, int xpToNext, int level)
    {
        CurrentXp = currentXp;
        XpToNextLevel = xpToNext;
        CurrentLevel = level;
        
        GD.Print($"Shared XP updated: Level {level}, XP {currentXp}/{xpToNext}");
        // TODO: Update UI elements here if needed
    }
    
    private void OnSharedLevelUp(int newLevel)
    {
        GD.Print($"Shared level up! New level: {newLevel}");
        // TODO: Trigger level up effects, sounds, etc.
    }
    public override void _Process(double delta)
    {
        _playerInputs.Handler();

        foreach (var spell in _spells)
        {
            spell.UpdateCooldown((float)delta);
        }

        CastSpells();

        // ADD THIS MISSING BATCH TIMER LOGIC:
        _rpcBatchTimer += (float)delta;
        if (_rpcBatchTimer >= RPC_BATCH_INTERVAL && _pendingSpells.Count > 0)
        {
            SendBatchedSpells();
            _rpcBatchTimer = 0f;
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        _playerInputs.Handler();
        UpdateMovement(delta);
    }

    private void UpdateMovement(double delta)
    {
        Vector3 velocity = Velocity;

        // Clamp velocity to prevent insane values
        if (velocity.Length() > 100.0f)
        {
            velocity = Vector3.Zero;
        }

        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        // Get the input direction and handle the movement/deceleration.
        Vector3 direction = _playerInputs.CalculatedDirection;
        if (_playerInputs.IsMoving)
        {
            velocity.X = direction.X * moveSpeed;
            velocity.Z = direction.Z * moveSpeed;

            Vector3 lookTarget = GlobalPosition - _playerInputs.CalculatedDirection * 3;
            _playerModel.LookAt(lookTarget);
            _animationTree.Set("parameters/conditions/Run", true);
            _animationTree.Set("parameters/conditions/Idle", false);
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, moveSpeed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, moveSpeed);
            _animationTree.Set("parameters/conditions/Run", false);
            _animationTree.Set("parameters/conditions/Idle", true);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
    private void OnPlayerTeleport(Vector3 newPosition)
    {
        GD.Print("Teleporting.. - ", Name);
        GD.Print("New pos: ", newPosition);

        // Reset velocity BEFORE moving to prevent physics conflicts
        Velocity = Vector3.Zero;

        // Disable physics temporarily to prevent conflicts
        SetPhysicsProcess(false);

        // Ensure player spawns above ground level
        var space_state = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            newPosition + Vector3.Up * 10,  // Start further above
            newPosition + Vector3.Down * 10  // Ray down to find ground
        );
        query.CollisionMask = 1;  // Ground layer

        var result = space_state.IntersectRay(query);
        if (result.ContainsKey("position"))
        {
            var groundPosition = result["position"].AsVector3();
            // Spawn 2 units above ground to be safe
            GlobalPosition = groundPosition + Vector3.Up * 2.0f;
            GD.Print("Adjusted spawn position to: ", GlobalPosition);
        }
        else
        {
            // Fallback: use the original position but add more height
            GlobalPosition = newPosition + Vector3.Up * 3.0f;
            GD.Print("No ground found, using elevated position: ", GlobalPosition);
        }

        // Force reset velocity again after position change
        Velocity = Vector3.Zero;

        // Re-enable physics and force a reset
        CallDeferred(nameof(ResetPhysicsAfterTeleport));
    }

    private void ResetPhysicsAfterTeleport()
    {
        Velocity = Vector3.Zero;
        SetPhysicsProcess(IsMultiplayerAuthority());
    }

    private void CastSpells()
    {
        foreach (var spell in _spells)
        {
            if (spell.CanCast())
            {
                if (spell.Name == "Fireball")
                {
                    CastFireball(spell);
                }
            }
        }
    }

    private void CastFireball(ISpell spell)
    {
        var fireballSpell = _spells.FirstOrDefault(s => s.Name == "Fireball") as FireballSpell;
        if (fireballSpell == null || !fireballSpell.CanCast()) return;

        var nearestEnemy = FindNearestEnemy(50f);
        if (nearestEnemy == null) return;

        var spawnPosition = GlobalPosition;
        var targetPosition = nearestEnemy.GlobalPosition;
        targetPosition.Y = spawnPosition.Y; // Keep same Y level
        var direction = (targetPosition - spawnPosition).Normalized();

        fireballSpell.Cast();
        _pendingSpells.Enqueue(new SpellCastData("Fireball", spawnPosition, direction, fireballSpell.Damage, fireballSpell.ProjectileSpeed));

    }
    private void SendBatchedSpells()
    {
        if (_pendingSpells.Count == 0) return;

        var spellArray = new SpellCastData[_pendingSpells.Count];
        for (int i = 0; i < spellArray.Length; i++)
        {
            spellArray[i] = _pendingSpells.Dequeue();
        }

        // Send each spell individually for now to avoid array conversion issues
        foreach (var spell in spellArray)
        {
            GD.Print($"Sending RPC for spell: {spell.SpellType} at position: {spell.SpawnPosition}");
            Rpc(nameof(SpawnSingleSpellRpc), spell.SpellType, spell.SpawnPosition, spell.Direction, spell.Damage, spell.Speed);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnSingleSpellRpc(string spellType, Vector3 spawnPosition, Vector3 direction, float damage, float speed)
    {
        GD.Print($"SpawnSingleSpellRpc called: {spellType} from peer {Multiplayer.GetRemoteSenderId()}");
        if (spellType == "Fireball")
        {
            var fireball = SpellProjectilePool.Instance?.GetFireball();
            if (fireball == null)
            {
                fireball = _fireballScene.Instantiate<Fireball>();
                GetTree().CurrentScene.AddChild(fireball);
            }
            fireball.GlobalPosition = spawnPosition;
            fireball.Initialize(damage, speed, direction, spawnPosition);
            fireball.Show();

        }
        // Add other spell types here as you implement them
    }
    private Node3D FindNearestEnemy(float range)
    {
        var enemies = GetTree().GetNodesInGroup("enemies").Cast<Node3D>().ToList();
        Node3D nearestEnemy = null;
        var minDistance = range;

        foreach (var enemy in enemies)
        {
            if (enemy is CocoChaser chaser && (!chaser.Visible || chaser.GlobalPosition.X > 5000))
                continue;
            var distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    public void GainXp(int amount)
    {
        // Only the multiplayer authority should process XP gains to avoid duplicates
        if (!IsMultiplayerAuthority()) return;
        
        int modifiedAmount = Mathf.RoundToInt(amount * XpGainMultiplier);
        
        // Use the shared XP system instead of individual progression
        if (SharedXPManager.Instance != null)
        {
            SharedXPManager.Instance.GainSharedXp(modifiedAmount);
            GD.Print($"Player gained {modifiedAmount} shared XP (base: {amount}, multiplier: {XpGainMultiplier:F2}x)");
        }
        else
        {
            GD.PrintErr("SharedXPManager not available - XP gain ignored");
        }
    }
    private void _on_pickup_area_area_entered(Area3D area){
		if (area is XpOrb orb)
		{
			orb.StartSeeking(this);
		}
	}
	private void _on_collection_area_area_entered(Area3D area)
	{
		if(area is XpOrb orb){
			GainXp(orb.XpAmount);
			orb.QueueFree();
		}
	}

}
