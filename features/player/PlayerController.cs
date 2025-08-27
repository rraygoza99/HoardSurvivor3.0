using Godot;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using SteamMultiplayer.features.player;

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
    public override void _PhysicsProcess(double delta)
    {
        if (character == null)
            return;
        Godot.Vector3 velocity = Velocity;
        if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
        UpdateMovement(delta);
    }

    private void UpdateMovement(double delta)
    {
        Godot.Vector2 inputDir = Input.GetVector("Right", "Left", "Down", "Up");
        direction = new Godot.Vector3(inputDir.X, 0, inputDir.Y).Normalized();
        bool isMoving = inputDir != Godot.Vector2.Zero;
        if (isMoving)
        {
            Velocity = new Godot.Vector3(direction.X * moveSpeed, Velocity.Y, direction.Z * moveSpeed);
            LookAt(Position + direction);
            _animationTree.Set("parameters/conditions/Run", true);
			_animationTree.Set("parameters/conditions/Idle", false);
        }
        else
        {
            Velocity = new Godot.Vector3(Mathf.MoveToward(Velocity.X, 0, moveSpeed), Velocity.Y, Mathf.MoveToward(Velocity.Z, 0, moveSpeed));
            _animationTree.Set("parameters/conditions/Run", false);
			_animationTree.Set("parameters/conditions/Idle", true);
        }
        MoveAndSlide();
    }
    private void OnPlayerTeleport(Vector3 newPosition)
	{
		GD.Print("Teleporting.. - ", Name);
		GD.Print("New pos: ", newPosition);
		GlobalPosition = newPosition;
	}

}
