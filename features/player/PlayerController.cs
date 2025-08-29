using Godot;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Features.Player.Characters.Types;
using SteamMultiplayer.features.player;

public partial class PlayerController : CharacterBody3D
{
    [Export] private MultiplayerSynchronizer _synchronizer;
    [Export] private PlayerCamera _camera;
    [Export] private int _multiplayerAuthority;
    private Character character;
    private float moveSpeed;
    private float currentHealth;
    private AnimationTree _animationTree;
    private PlayerInputs _playerInputs;
    public Vector3 StartPosition { get; set; }
    [Export] private Node3D _playerModel;

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
        var main = GetTree().Root.GetNode<Node>("Main");
		main.Connect("player_teleport", new Callable(this, MethodName.OnPlayerTeleport));
    }
    public override void _Process(double delta)
    {
        _playerInputs.Handler();
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

            // Calculate the look target based on movement direction
            Vector3 lookTarget = GlobalPosition - direction * 3;
            
            // First rotate the root node (CharacterBody3D) for network synchronization
            LookAt(new Vector3(lookTarget.X, GlobalPosition.Y, lookTarget.Z));
            
            // Then rotate the model
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

}
