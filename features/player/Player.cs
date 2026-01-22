using Godot;

namespace SteamMultiplayer.features.player;

public partial class Player : CharacterBody3D
{
	[Export] private MultiplayerSynchronizer _synchronizer;
	[Export]private float _speed = 5.0f;
	[Export]private PlayerCamera _camera;
	[Export] private PlayerUI _playerUI;

	[Export] private int _multiplayerAuthority;

	// Base stats
	[Export] private float _baseMaxHealth = 100.0f;
	[Export] private float _baseMovementSpeed = 5.0f;
	[Export] private float _baseXpGain = 1.0f;
	[Export] private float _baseCooldownReduction = 0.0f;
	[Export] private float _baseLifeSteal = 0.0f;
	[Export] private float _baseCriticalChance = 0.0f;
	[Export] private float _baseCriticalDamage = 1.5f;
	[Export] private float _baseArmor = 0.0f;
	[Export] private float _baseLucky = 0.0f;
	[Export] private float _baseGeneralDamage = 1.0f;
	[Export] private float _baseMagicSphereDamage = 1.0f;
	[Export] private float _baseArcaneWaveDamage = 1.0f;
	[Export] private float _baseMortarDamage = 1.0f;

	// Current stats (base + upgrades)
	public float MaxHealth { get; private set; }
	public float MovementSpeed { get; private set; }
	public float XpGain { get; private set; }
	public float CooldownReduction { get; private set; }
	public float LifeSteal { get; private set; }
	public float CriticalChance { get; private set; }
	public float CriticalDamage { get; private set; }
	public float Armor { get; private set; }
	public float Lucky { get; private set; }
	public float GeneralDamage { get; private set; }
	public float MagicSphereDamage { get; private set; }
	public float ArcaneWaveDamage { get; private set; }
	public float MortarDamage { get; private set; }

	// Current health
	public float CurrentHealth { get; private set; }
	
	[Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);


	public int MultiplayerAuthority
	{
		get => _multiplayerAuthority;
		set
		{
			_multiplayerAuthority = value;
			//SetMultiplayerAuthority(value);
		}
	}
	
	public Vector3 StartPosition { get; set; }
	
	private PlayerInputs _playerInputs;

	public override void _Ready()
	{
		// Initialize stats with base values
		InitializeStats();

		var isMultiplayerAuthority = IsMultiplayerAuthority();

		// Only local authority should display its UI
		if (_playerUI != null)
		{
			_playerUI.Visible = isMultiplayerAuthority;
		}
		
		SetProcess(isMultiplayerAuthority);
		SetPhysicsProcess(isMultiplayerAuthority);

		// Setup collision layers - players on layer 2
		SetCollisionLayerValue(1, false);  // Not on ground layer
		SetCollisionLayerValue(2, true);   // On player layer
		SetCollisionMaskValue(1, true);    // Collide with ground/environment
		SetCollisionMaskValue(2, false);   // Don't collide with other players
		SetCollisionMaskValue(3, false);   // Don't collide with enemies (phase through)

		GlobalPosition = StartPosition;
		
		if (!isMultiplayerAuthority)
		{
			return;
		}
		
		_playerInputs = new PlayerInputs(this);

		var main = GetTree().Root.GetNode<Node>("Main");
		main.Connect("player_teleport", new Callable(this, MethodName.OnPlayerTeleport));
		
		// Connect to the HealthChanged signal
		if (_playerUI != null)
		{
			HealthChanged += _playerUI.SetHealth;
		}
		else
		{
			GD.PrintErr("[Player] _playerUI is null; skipping HealthChanged hookup");
		}
		
		// Connect to the SharedXPManager signals
		if (SharedXPManager.Instance != null)
		{
			SharedXPManager.Instance.SharedXpChanged += (currentXp, xpToNext, level) => _playerUI?.SetXP(currentXp, xpToNext);
			SharedXPManager.Instance.SharedLevelUp += (newLevel) => _playerUI?.SetLevel(newLevel);
		}
		else
		{
			GD.PrintErr("[Player] SharedXPManager.Instance is null; skipping XP/Level event hookups");
		}
		
		// Set initial UI values
		if (_playerUI != null && SharedXPManager.Instance != null)
		{
			_playerUI.SetHealth(CurrentHealth, MaxHealth);
			var progress = SharedXPManager.Instance.GetSharedXpProgress();
			_playerUI.SetXP(progress["current_xp"].AsSingle(), progress["xp_to_next_level"].AsSingle());
			_playerUI.SetLevel(progress["current_level"].AsInt32());
		}
		else
		{
			GD.PrintErr("[Player] Skipping initial UI setup due to null _playerUI or SharedXPManager.Instance");
		}
	}

	private void InitializeStats()
	{
		MaxHealth = _baseMaxHealth;
		MovementSpeed = _baseMovementSpeed;
		XpGain = _baseXpGain;
		CooldownReduction = _baseCooldownReduction;
		LifeSteal = _baseLifeSteal;
		CriticalChance = _baseCriticalChance;
		CriticalDamage = _baseCriticalDamage;
		Armor = _baseArmor;
		Lucky = _baseLucky;
		GeneralDamage = _baseGeneralDamage;
		MagicSphereDamage = _baseMagicSphereDamage;
		ArcaneWaveDamage = _baseArcaneWaveDamage;
		MortarDamage = _baseMortarDamage;

		CurrentHealth = MaxHealth;
		_speed = MovementSpeed; // Update the movement speed used in physics
	}

	public void TakeDamage(float damage)
	{
		if (CurrentHealth <= 0) return;

		var actualDamage = damage * (1 - (Armor / (Armor + 100)));
		CurrentHealth -= actualDamage;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);
		
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		GD.Print($"Player took {actualDamage} damage, health is now {CurrentHealth}");

		if (CurrentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print("Player has died.");
		// Handle player death, e.g., respawn, show game over screen, etc.
	}

	public void ApplyUpgrade(Upgrade upgrade)
	{
		if (upgrade == null)
		{
			GD.PrintErr("Cannot apply null upgrade");
			return;
		}

		GD.Print($"Applying upgrade: {upgrade.Name} (+{upgrade.Value} to {upgrade.StatToUpgrade})");

		switch (upgrade.StatToUpgrade)
		{
			case Stat.MaxHealth:
				var oldMaxHealth = MaxHealth;
				MaxHealth += upgrade.Value;
				// Heal the player proportionally when max health increases
				var healthPercentage = CurrentHealth / oldMaxHealth;
				CurrentHealth = MaxHealth * healthPercentage;
				EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
				GD.Print($"Max Health: {oldMaxHealth} -> {MaxHealth}, Current Health: {CurrentHealth}");
				break;

			case Stat.MovementSpeed:
				MovementSpeed += upgrade.Value;
				_speed = MovementSpeed; // Update the physics speed
				GD.Print($"Movement Speed: {MovementSpeed}");
				break;

			case Stat.XpGain:
				XpGain += upgrade.Value;
				GD.Print($"XP Gain: {XpGain}");
				break;

			case Stat.CooldownReduction:
				CooldownReduction += upgrade.Value;
				// Cap at 90% cooldown reduction
				CooldownReduction = Mathf.Min(CooldownReduction, 90.0f);
				GD.Print($"Cooldown Reduction: {CooldownReduction}%");
				break;

			case Stat.LifeSteal:
				LifeSteal += upgrade.Value;
				GD.Print($"Life Steal: {LifeSteal}%");
				break;

			case Stat.CriticalChance:
				CriticalChance += upgrade.Value;
				// Cap at 100% critical chance
				CriticalChance = Mathf.Min(CriticalChance, 100.0f);
				GD.Print($"Critical Chance: {CriticalChance}%");
				break;

			case Stat.CriticalDamage:
				CriticalDamage += upgrade.Value;
				GD.Print($"Critical Damage: {CriticalDamage}x");
				break;

			case Stat.Armor:
				Armor += upgrade.Value;
				GD.Print($"Armor: {Armor}");
				break;

			case Stat.Lucky:
				Lucky += upgrade.Value;
				GD.Print($"Lucky: {Lucky}");
				break;

			case Stat.GeneralDamage:
				GeneralDamage += upgrade.Value;
				GD.Print($"General Damage: {GeneralDamage}x");
				break;

			case Stat.MagicSphereDamage:
				MagicSphereDamage += upgrade.Value;
				GD.Print($"Magic Sphere Damage: {MagicSphereDamage}x");
				break;

			case Stat.ArcaneWaveDamage:
				ArcaneWaveDamage += upgrade.Value;
				GD.Print($"Arcane Wave Damage: {ArcaneWaveDamage}x");
				break;

			case Stat.MortarDamage:
				MortarDamage += upgrade.Value;
				GD.Print($"Mortar Damage: {MortarDamage}x");
				break;

			default:
				GD.PrintErr($"Unknown stat type: {upgrade.StatToUpgrade}");
				break;
		}
	}

	public override void _Process(double delta)
	{
		try
		{
			_playerInputs?.Handler();
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Player._Process] Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		try
		{
			Vector3 velocity = Velocity;
			
			var lookTarget = GetMousePosition3D();
			if (lookTarget != Vector3.Zero)
			{
				LookAt(lookTarget);
			}

			// Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}
			
			// Get the input direction and handle the movement/deceleration.
			Vector3 direction = _playerInputs != null ? _playerInputs.CalculatedDirection : Vector3.Zero;
			if (_playerInputs != null && _playerInputs.IsMoving)
			{
				velocity.X = direction.X * _speed;
				velocity.Z = direction.Z * _speed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, _speed);
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _speed);
			}

			Velocity = velocity;
			MoveAndSlide();
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Player._PhysicsProcess] Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
		}
	}
	
	private Vector3 GetMousePosition3D()
	{
		if (_camera == null)
		{
			return Vector3.Zero;
		}
		var targetPlane = new Plane(new(0, 1, 0), GlobalPosition.Y);
		var mousePosition = GetViewport().GetMousePosition();
		var camera = _camera;

		var rayStart = camera.ProjectRayOrigin(mousePosition);
		var rayEnd = rayStart + camera.ProjectRayNormal(mousePosition) * 2000;

		return targetPlane.IntersectsRay(rayStart, rayEnd) ?? Vector3.Zero;
	}

	private void OnPlayerTeleport(Vector3 newPosition)
	{
		GD.Print("Teleporting.. - ", Name);
		GD.Print("New pos: ", newPosition);
		GlobalPosition = newPosition;
	}
}