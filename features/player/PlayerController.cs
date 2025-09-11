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
        Godot.Vector3 velocity = Velocity;
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }
        _playerInputs.Handler();
        UpdateMovement(delta);
    }

    private void UpdateMovement(double delta)
    {
        Vector3 velocity = Velocity;    
        if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
        
        if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector3 direction = _playerInputs.CalculatedDirection;
		if (_playerInputs.IsMoving)
		{
			velocity.X = direction.X * moveSpeed;
			velocity.Z = direction.Z * moveSpeed;

            Vector3 lookTarget = GlobalPosition - _playerInputs.CalculatedDirection*3;
            _playerModel.LookAt(lookTarget);
			_animationTree.Set("parameters/conditions/Run", true);
			_animationTree.Set("parameters/conditions/Idle", false);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, moveSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, moveSpeed);
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
		GlobalPosition = newPosition;
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

        var spawnPosition = GlobalPosition + (-GlobalTransform.Basis.Z * 2f);
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
            Rpc(nameof(SpawnSingleSpellRpc), spell.SpellType, spell.SpawnPosition, spell.Direction, spell.Damage, spell.Speed);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnSingleSpellRpc(string spellType, Vector3 spawnPosition, Vector3 direction, float damage, float speed)
    {
        if (spellType == "Fireball")
        {
            var fireball = _fireballScene.Instantiate<Fireball>();
            GetTree().CurrentScene.AddChild(fireball);
            fireball.GlobalPosition = spawnPosition;
            fireball.Initialize(damage, speed, direction, spawnPosition);
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
            var distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

}
