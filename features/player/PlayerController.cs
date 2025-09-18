using Godot;
using HoardSurvivor3._0.Features.Player.Characters.Base;
using HoardSurvivor3._0.Features.Player.Characters.Types;
using HoardSurvivor3._0.Features.Spells.Base;
using HoardSurvivor3._0.Features.Spells;
using System.Collections.Generic;
using System.Linq;
using SteamMultiplayer.features.player;
using HoardSurvivor3._0.Features.Spells.Data;
using HoardSurvivor3._0.Features.Spells.Types;

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
	[Export] private PlayerUI _playerUI;

	// Player stats affected by upgrades
	[Export] float MaxHealth { get; set; } = 100.0f;
	[Export] float CooldownReduction { get; set; } = 0.0f;
	[Export] float LifeSteal { get; set; } = 0.0f;
	[Export] float CriticalChance { get; set; } = 0.0f;
	[Export] float CriticalDamage { get; set; } = 1.5f;
	[Export] float Armor { get; set; } = 0.0f;
	[Export] float Lucky { get; set; } = 0.0f;
	[Export] float GeneralDamage { get; set; } = 1.0f;
	[Export] float MagicSphereDamage { get; set; } = 1.0f;
	[Export] float ArcaneWaveDamage { get; set; } = 1.0f;
	[Export] float MortarDamage { get; set; } = 1.0f;

	// Spell casting related fields
	private PackedScene _fireballScene;
	private PackedScene _orbitalsScene;
	private List<ISpell> _spells = new(); // ensure non-null for remote peers
	private Orbitals _activeOrbitals;
	private Queue<SpellCastData> _pendingSpells = new();
	private float _rpcBatchTimer = 0f;
	private const float RPC_BATCH_INTERVAL = 0.1f;

	private Godot.Vector3 direction = Godot.Vector3.Zero;

	private bool canCast = true;
	
	// Level up system components
	private LevelUpScreen _levelUpScreen;
	private SpellSelectionScreen _spellSelectionScreen;
	private UpgradeManager _upgradeManager;
	public Area3D pickupArea;
	[ExportGroup("Player Stats")]
	// Individual XP properties are no longer used - keeping for compatibility
	[Export] public int CurrentXp { get; private set; } = 0;
	[Export] float XpGainMultiplier { get; set; } = 1.0f;
	[Export] public int XpToNextLevel { get; private set; } = 100;
	[Export] public int CurrentLevel { get; private set; } = 1;

	[Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

	private bool _isInvulnerable = false;

	public int MultiplayerAuthority
	{
		get => _multiplayerAuthority;
		set
		{
			_multiplayerAuthority = value;
			SetMultiplayerAuthority(value);
			Name = value.ToString();
		}
	}

	public void Initialize(Character selectedCharacter)
	{
		character = selectedCharacter;
		moveSpeed = character.Stats.MoveSpeed;
		MaxHealth = character.Stats.MaxHealth;
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

		// Always load spell scenes BEFORE any authority return so RPC instantiation works on all peers
		_fireballScene ??= GD.Load<PackedScene>("res://features/spells/types/Fireball.tscn");
		_orbitalsScene ??= GD.Load<PackedScene>("res://features/spells/types/Orbitals.tscn");

		// Only show UI for local authority, but do not return before loading spells
		if (_playerUI != null)
		{
			_playerUI.Visible = isMultiplayerAuthority;
		}

		if (!isMultiplayerAuthority)
		{
			// Non-authority still needs spell scenes loaded; skip only input / XP hookup
			return;
		}
		_playerInputs = new PlayerInputs(this);
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_animationTree.Active = true;

		// Initialize spell casting
		_spells = character.Spells;

		ActivatePassiveSpells();

		var main = GetTree().Root.GetNode<Node>("Main");
		main.Connect("player_teleport", new Callable(this, MethodName.OnPlayerTeleport));
		
		// Connect to shared XP system
		if (SharedXPManager.Instance != null)
		{
			SharedXPManager.Instance.SharedXpChanged += OnSharedXpChanged;
			SharedXPManager.Instance.SharedLevelUp += OnSharedLevelUp;
			SharedXPManager.Instance.ShowLevelUpScreen += OnShowLevelUpScreen;
			SharedXPManager.Instance.ShowSpellSelectionScreen += OnShowSpellSelectionScreen;
			
			// Sync current values from shared system
			SyncWithSharedXP();
		}
		else
		{
			GD.Print("SharedXPManager not yet available, will try later");
			// Try again after a short delay
			CallDeferred(nameof(TryConnectToSharedXP));
		}
		
		// Initialize level up system for each player (each gets their own screen and choices)
		SetupLevelUpSystem();

		HealthChanged += _playerUI.SetHealth;
		
		// Connect to the SharedXPManager signals
		SharedXPManager.Instance.SharedXpChanged += (currentXp, xpToNext, level) => _playerUI.SetXP(currentXp, xpToNext);
		SharedXPManager.Instance.SharedLevelUp += (newLevel) => _playerUI.SetLevel(newLevel);
		
		// Set initial UI values
		_playerUI.SetHealth(currentHealth, MaxHealth);
		var progress = SharedXPManager.Instance.GetSharedXpProgress();
		_playerUI.SetXP(progress["current_xp"].AsSingle(), progress["xp_to_next_level"].AsSingle());
		_playerUI.SetLevel(progress["current_level"].AsInt32());

		StartInvulnerability();
	}

	private void StartInvulnerability()
	{
		_isInvulnerable = true;
		
		var invulnerabilityTimer = new Timer();
		invulnerabilityTimer.WaitTime = 3.0f;
		invulnerabilityTimer.OneShot = true;
		invulnerabilityTimer.Timeout += OnInvulnerabilityTimerTimeout;
		AddChild(invulnerabilityTimer);
		invulnerabilityTimer.Start();
		
		var tween = GetTree().CreateTween();
		tween.TweenMethod(Callable.From<float>(SetModelAlpha), 0.5f, 1.0f, 3.0f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		tween.Play();
	}

	private void OnInvulnerabilityTimerTimeout()
	{
		_isInvulnerable = false;
		SetModelAlpha(1.0f);
		GD.Print("Player is no longer invulnerable.");
	}

	private void SetModelAlpha(float alpha)
	{
		var meshInstances = _playerModel.FindChildren("*", "MeshInstance3D", true);
		foreach (var node in meshInstances)
		{
			if (node is MeshInstance3D meshInstance)
			{
				var material = meshInstance.GetActiveMaterial(0) as StandardMaterial3D;
				if (material != null)
				{
					var newColor = material.AlbedoColor;
					newColor.A = alpha;
					material.AlbedoColor = newColor;
				}
			}
		}
	}
	
	private void TryConnectToSharedXP()
	{
		if (SharedXPManager.Instance != null)
		{
			SharedXPManager.Instance.SharedXpChanged += OnSharedXpChanged;
			SharedXPManager.Instance.SharedLevelUp += OnSharedLevelUp;
			SharedXPManager.Instance.ShowLevelUpScreen += OnShowLevelUpScreen;
			SharedXPManager.Instance.ShowSpellSelectionScreen += OnShowSpellSelectionScreen;
			SyncWithSharedXP();
			GD.Print("Successfully connected to SharedXPManager");
		}
	}
	
	private void SetupLevelUpSystem()
	{
		// Load and instantiate level up screen
		var levelUpScreenScene = GD.Load<PackedScene>("res://features/player/xp/level_up/level_up_screen.tscn");
		if (levelUpScreenScene != null)
		{
			_levelUpScreen = levelUpScreenScene.Instantiate<LevelUpScreen>();
			GetTree().CurrentScene.AddChild(_levelUpScreen);
			_levelUpScreen.Hide(); // Hidden by default
			_levelUpScreen.UpgradeChosen += OnUpgradeChosen;
			GD.Print("Level up screen initialized");
		}
		else
		{
			GD.PrintErr("Could not load level up screen scene");
		}

		// Load and instantiate spell selection screen
		var spellSelectionScreenScene = GD.Load<PackedScene>("res://features/player/xp/level_up/spell_selection/spell_selection_screen.tscn");
		if (spellSelectionScreenScene != null)
		{
			_spellSelectionScreen = spellSelectionScreenScene.Instantiate<SpellSelectionScreen>();
			GetTree().CurrentScene.AddChild(_spellSelectionScreen);
			_spellSelectionScreen.Hide(); // Hidden by default
			_spellSelectionScreen.SpellChosen += OnSpellChosen;
			_spellSelectionScreen.Set("_spellCardScene", GD.Load<PackedScene>("res://features/player/xp/level_up/spell_selection/spell_card.tscn"));
			GD.Print("Spell selection screen initialized");
		}
		else
		{
			GD.PrintErr("Could not load spell selection screen scene");
		}
		
		// Initialize upgrade manager
		_upgradeManager = new UpgradeManager();
		AddChild(_upgradeManager);
		
		// Set the upgrade card scene reference
		var upgradeCardScene = GD.Load<PackedScene>("res://features/player/xp/level_up/upgrade_card.tscn");
		if (_levelUpScreen != null && upgradeCardScene != null)
		{
			_levelUpScreen.Set("_upgradeCardScene", upgradeCardScene);
		}
		
		// Load all upgrade resources into the manager
		LoadUpgradeResources();
	}
	
	private void LoadUpgradeResources()
	{
		var upgradePool = new Godot.Collections.Array<Upgrade>();
		
		// Define all upgrade files to load
		string[] upgradeFiles = {
			// Common upgrades
			"res://features/player/Upgrades/common_arcane_wave_damage.tres",
			"res://features/player/Upgrades/common_armor.tres",
			"res://features/player/Upgrades/common_cooldown.tres",
			"res://features/player/Upgrades/common_critical_chance.tres",
			"res://features/player/Upgrades/common_critical_dmg.tres",
			"res://features/player/Upgrades/common_general_damage.tres",
			"res://features/player/Upgrades/common_health.tres",
			"res://features/player/Upgrades/common_lifesteal.tres",
			"res://features/player/Upgrades/common_luck.tres",
			"res://features/player/Upgrades/common_magic_sphere_damage.tres",
			//"res://features/player/Upgrades/common_mortar_damage.tres",
			"res://features/player/Upgrades/common_speed.tres",
			"res://features/player/Upgrades/common_xp.tres",
			// Rare upgrades
			"res://features/player/Upgrades/rare_arcane_wave_damage.tres",
			"res://features/player/Upgrades/rare_armor.tres",
			"res://features/player/Upgrades/rare_cooldown.tres",
			"res://features/player/Upgrades/rare_critical_chance.tres",
			"res://features/player/Upgrades/rare_critical_dmg.tres",
			"res://features/player/Upgrades/rare_general_damage.tres",
			"res://features/player/Upgrades/rare_health.tres",
			"res://features/player/Upgrades/rare_lifesteal.tres",
			"res://features/player/Upgrades/rare_luck.tres",
			"res://features/player/Upgrades/rare_magic_sphere_damage.tres",
			//"res://features/player/Upgrades/rare_mortar_damage.tres",
			"res://features/player/Upgrades/rare_speed.tres",
			"res://features/player/Upgrades/rare_xp.tres",
			// Legendary upgrades
			"res://features/player/Upgrades/legendary_arcane_wave_damage.tres",
			"res://features/player/Upgrades/legendary_armor.tres",
			"res://features/player/Upgrades/legendary_cooldown.tres",
			"res://features/player/Upgrades/legendary_critical_chance.tres",
			"res://features/player/Upgrades/legendary_critical_dmg.tres",
			"res://features/player/Upgrades/legendary_general_damage.tres",
			"res://features/player/Upgrades/legendary_health.tres",
			"res://features/player/Upgrades/legendary_lifesteal.tres",
			"res://features/player/Upgrades/legendary_luck.tres",
			"res://features/player/Upgrades/legendary_magic_sphere_damage.tres",
			//"res://features/player/Upgrades/legendary_mortar_damage.tres",
			"res://features/player/Upgrades/legendary_speed.tres",
			"res://features/player/Upgrades/legendary_xp.tres"
		};
		
		foreach (string path in upgradeFiles)
		{
			var upgrade = GD.Load<Upgrade>(path);
			if (upgrade != null)
			{
				upgradePool.Add(upgrade);
			}
			else
			{
				GD.PrintErr($"Could not load upgrade: {path}");
			}
		}
		
		// Set the upgrade pool in the manager
		_upgradeManager.Set("_upgradePool", upgradePool);
		GD.Print($"Loaded {upgradePool.Count} upgrades into manager");
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
	
	private void OnShowLevelUpScreen(int newLevel)
	{
		// Each player shows their own level up screen with different upgrade choices
		if (_levelUpScreen != null && _upgradeManager != null)
		{
			GD.Print($"Showing level up screen for level {newLevel}");
			
			// Get upgrade choices (each player gets different random options)
			var upgradeChoices = _upgradeManager.GetUpgradeChoices(Lucky, GetPlayerSpellNames());
			
			if (upgradeChoices.Count > 0)
			{
				_levelUpScreen.DisplayUpgrades(upgradeChoices);
			}
			else
			{
				GD.PrintErr("No upgrade choices available for level up");
			}
		}
		else
		{
			GD.PrintErr("Level up screen or upgrade manager not initialized");
		}
	}
	
	private void OnShowSpellSelectionScreen(int newLevel)
	{
		if (_spellSelectionScreen != null)
		{
			GD.Print($"Showing spell selection screen for level {newLevel}");
			var availableSpells = Core.Factories.SpellFactory.GetAllAvailableSpells();
			var currentSpellNames = GetPlayerSpellNames();
			var newSpells = availableSpells.Where(s => !currentSpellNames.Contains(s.Name)).ToList();

			if (character is Wizgod)
			{
				newSpells = newSpells.Where(s => s.Name == "Magic Wave").ToList();
			}

			if (newSpells.Any())
			{
				_spellSelectionScreen.DisplaySpells(newSpells);
			}
			else
			{
				GD.Print("No new spells available for this character.");
				// If no new spells, show regular upgrade screen
				OnShowLevelUpScreen(newLevel);
			}
		}
		else
		{
			GD.PrintErr("Spell selection screen not initialized");
		}
	}

	private void OnUpgradeChosen(Upgrade upgrade)
	{
		GD.Print($"Player chose upgrade: {upgrade.Name} (+{upgrade.Value} {upgrade.StatToUpgrade})");
		
		// Apply the chosen upgrade to this player
		ApplyUpgrade(upgrade);
		
		// TODO: You might want to sync the chosen upgrade to other players for display purposes
		// or handle upgrade effects that affect shared gameplay
	}

	private void OnSpellChosen(int spellType)
	{
		var spell = Core.Factories.SpellFactory.CreateSpell((HoardSurvivor3._0.Core.Enums.SpellType)spellType);
		AddSpell(spell);
	}

	private void AddSpell(ISpell spell)
	{
		if (spell != null && !_spells.Any(s => s.Name == spell.Name))
		{
			_spells.Add(spell);
			GD.Print($"[DEBUG] Player learned new spell: {spell.Name}");

			if (spell is OrbitalsSpell orbitalsSpell)
			{
				GD.Print("[DEBUG] New spell is OrbitalsSpell.");
				if (_activeOrbitals == null)
				{
					GD.Print("[DEBUG] _activeOrbitals is null, requesting network spawn.");
					// Only authority triggers the network-wide spawn to maintain consistency
					if (IsMultiplayerAuthority())
					{
						Rpc(nameof(RpcSpawnOrbitals), orbitalsSpell.Damage, orbitalsSpell.ProjectileAmount, orbitalsSpell.ProjectileSpeed, orbitalsSpell.ProjectileRange, Multiplayer.GetUniqueId());
					}
				}
				else
				{
					GD.Print("[DEBUG] _activeOrbitals already exists.");
				}
			}
			
			// Since we are not showing a UI, we can immediately say the "upgrade" is done.
			SharedXPManager.Instance.OnPlayerSelectedUpgrade(Multiplayer.GetUniqueId());
		}
	}
	
	private void ActivatePassiveSpells()
	{
		foreach (var spell in _spells)
		{
			if (spell is OrbitalsSpell orbitalsSpell)
			{
				GD.Print("[DEBUG] Found starting OrbitalsSpell.");
				if (_activeOrbitals == null && IsMultiplayerAuthority())
				{
					GD.Print("[DEBUG] _activeOrbitals is null at start, spawning via RPC.");
					Rpc(nameof(RpcSpawnOrbitals), orbitalsSpell.Damage, orbitalsSpell.ProjectileAmount, orbitalsSpell.ProjectileSpeed, orbitalsSpell.ProjectileRange, Multiplayer.GetUniqueId());
				}
			}
		}
	}
	
	private void ApplyUpgrade(Upgrade upgrade)
	{
		if (upgrade == null)
		{
			GD.PrintErr("Cannot apply null upgrade");
			return;
		}

		GD.Print($"Applying upgrade: {upgrade.Name} (+{upgrade.Value} to {upgrade.StatToUpgrade})");

		// Apply upgrade effects based on the stat type
		switch (upgrade.StatToUpgrade)
		{
			case Stat.MaxHealth:
				var oldMaxHealth = MaxHealth;
				MaxHealth += upgrade.Value;
				// Heal the player proportionally when max health increases
				var healthPercentage = currentHealth / oldMaxHealth;
				currentHealth = MaxHealth * healthPercentage;
				EmitSignal(nameof(HealthChanged), currentHealth, MaxHealth);
				GD.Print($"Max Health: {oldMaxHealth} -> {MaxHealth}, Current Health: {currentHealth}");
				break;

			case Stat.MovementSpeed:
				moveSpeed *= 1 + (upgrade.Value / 100.0f);
				GD.Print($"Movement Speed: {moveSpeed}");
				break;

			case Stat.XpGain:
				XpGainMultiplier += upgrade.Value;
				GD.Print($"Applied XP gain upgrade: +{upgrade.Value}%. New multiplier: {XpGainMultiplier:F2}x");
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
				GeneralDamage *= (1 + upgrade.Value / 100.0f);
				GD.Print($"General Damage: {GeneralDamage}x");
				break;

			case Stat.MagicSphereDamage:
				MagicSphereDamage *= (1 + upgrade.Value / 100.0f);
				GD.Print($"Magic Sphere Damage: {MagicSphereDamage}x");
				break;

			case Stat.ArcaneWaveDamage:
				ArcaneWaveDamage *= (1 + upgrade.Value / 100.0f);
				GD.Print($"Arcane Wave Damage: {ArcaneWaveDamage}x");
				break;

			case Stat.MortarDamage:
				MortarDamage *= (1 + upgrade.Value / 100.0f);
				GD.Print($"Mortar Damage: {MortarDamage}x");
				break;

			default:
				GD.PrintErr($"Unknown stat type: {upgrade.StatToUpgrade}");
				break;
		}
	}

	private float CalculateFinalDamage(float baseDamage, float spellDamageMultiplier = 1.0f)
	{
		// Apply general damage and spell-specific damage multipliers
		float modifiedDamage = baseDamage * GeneralDamage * spellDamageMultiplier;
		
		// Check for critical hit
		bool isCritical = ShouldCriticalHit();
		if (isCritical)
		{
			modifiedDamage *= CriticalDamage;
			GD.Print($"💥 CRITICAL HIT! Base: {baseDamage:F1} → Final: {modifiedDamage:F1} (x{CriticalDamage:F1})");
		}
		
		return modifiedDamage;
	}
	
	private bool ShouldCriticalHit()
	{
		// Generate random number between 0-100 and check against critical chance
		var random = new RandomNumberGenerator();
		random.Randomize();
		float roll = random.RandfRange(0f, 100f);
		bool isCrit = roll < CriticalChance;
		
		if (isCrit)
		{
			GD.Print($"🎯 Critical hit rolled! ({roll:F1} < {CriticalChance:F1}%)");
		}
		
		return isCrit;
	}

	// Method to get current player stats for UI/debugging
	public string GetStatsDisplay()
	{
		return $"Health: {currentHealth:F0}/{MaxHealth:F0} | " +
			   $"Speed: {moveSpeed:F1} | " +
			   $"Crit: {CriticalChance:F1}%/{CriticalDamage:F1}x | " +
			   $"Damage: {GeneralDamage:F1}x | " +
			   $"XP: {XpGainMultiplier:F1}x | " +
			   $"Lucky: {Lucky:F0}";
	}

	private List<string> GetPlayerSpellNames()
	{
		var spellNames = new List<string>();
		
		if (_spells != null)
		{
			foreach (var spell in _spells)
			{
				spellNames.Add(spell.Name);
			}
		}
		
		GD.Print($"Player has spells: [{string.Join(", ", spellNames)}]");
		return spellNames;
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
				else if (spell.Name == "Magic Wave")
				{
					CastMagicWave(spell);
				}
				else if (spell.Name == "Orbitals")
				{
					GD.Print("[DEBUG] Attempting to cast Orbitals.");
					CastOrbitals(spell);
				}
				// TODO: Add other spell types when implemented
				// else if (spell.Name == "ArcaneWave")
				// {
				//     CastArcaneWave(spell);
				// }
				// else if (spell.Name == "Mortar")
				// {
				//     CastMortar(spell);
				// }
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
		var fireballDamage = CalculateFinalDamage(fireballSpell.Damage, MagicSphereDamage);
		_pendingSpells.Enqueue(new SpellCastData("Fireball", spawnPosition, direction, fireballDamage, fireballSpell.ProjectileSpeed));

	}

	private void CastMagicWave(ISpell spell)
	{
		var magicWaveSpell = _spells.FirstOrDefault(s => s.Name == "Magic Wave") as HoardSurvivor3._0.Features.Spells.MagicWaveSpell;
		if (magicWaveSpell == null || !magicWaveSpell.CanCast()) return;

		var nearestEnemy = FindNearestEnemy(50f);
		if (nearestEnemy == null) return;

		var spawnPosition = GlobalPosition;
		var targetPosition = nearestEnemy.GlobalPosition;
		targetPosition.Y = spawnPosition.Y; // Keep same Y level
		var direction = (targetPosition - spawnPosition).Normalized();

		magicWaveSpell.Cast();
		var magicWaveDamage = CalculateFinalDamage(magicWaveSpell.Damage, ArcaneWaveDamage);
		_pendingSpells.Enqueue(new SpellCastData("Magic Wave", spawnPosition, direction, magicWaveDamage, magicWaveSpell.ProjectileSpeed));
	}

	private void CastOrbitals(ISpell spell)
	{
		var orbitalsSpell = _spells.FirstOrDefault(s => s.Name == "Orbitals") as OrbitalsSpell;
		if (orbitalsSpell == null)
		{
			GD.PrintErr("[DEBUG] CastOrbitals: OrbitalsSpell object not found in player's spell list.");
			return;
		}
		if (!orbitalsSpell.CanCast())
		{
			GD.Print($"[DEBUG] CastOrbitals: CanCast() is false. Cooldown: {orbitalsSpell.CurrentCooldown}");
			return;
		}

		// Since Orbitals is a passive spell that is always active,
		// we just need to reset its cooldown.
		orbitalsSpell.Cast();
		GD.Print("[DEBUG] Orbitals spell cooldown reset.");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcSpawnOrbitals(float damage, int projectileAmount, float projectileSpeed, float projectileRange, int ownerPeerId)
	{
		if (_activeOrbitals != null)
		{
			GD.Print("[DEBUG] RpcSpawnOrbitals called but orbitals already exist.");
			return;
		}
		if (_orbitalsScene == null)
		{
			_orbitalsScene = GD.Load<PackedScene>("res://features/spells/types/Orbitals.tscn");
		}
		_activeOrbitals = _orbitalsScene.Instantiate<Orbitals>();
		// Ensure a deterministic authority is set so only one peer applies damage (same peer id used for projectiles)
		_activeOrbitals.SetMultiplayerAuthority(ownerPeerId);
		AddChild(_activeOrbitals);
		var isAuthorityForDamage = Multiplayer.GetUniqueId() == ownerPeerId; // Only owner processes damage
		_activeOrbitals.InitializeFromData(damage, projectileAmount, projectileSpeed, projectileRange, isAuthorityForDamage);
		GD.Print($"[DEBUG] RpcSpawnOrbitals -> Orbitals instantiated (owner {ownerPeerId}, local {Multiplayer.GetUniqueId()}, authorityDamage={isAuthorityForDamage}).");
	}

	// Example methods for future spell implementations
	// These would use the critical hit system when implemented
	
	/*
	private void CastArcaneWave(ISpell spell)
	{
		var arcaneWaveSpell = _spells.FirstOrDefault(s => s.Name == "ArcaneWave") as ArcaneWaveSpell;
		if (arcaneWaveSpell == null || !arcaneWaveSpell.CanCast()) return;

		// Arcane Wave logic here...
		arcaneWaveSpell.Cast();
		var arcaneWaveDamage = CalculateFinalDamage(arcaneWaveSpell.Damage, ArcaneWaveDamage);
		// Use arcaneWaveDamage for the spell
	}

	private void CastMortar(ISpell spell)
	{
		var mortarSpell = _spells.FirstOrDefault(s => s.Name == "Mortar") as MortarSpell;
		if (mortarSpell == null || !mortarSpell.CanCast()) return;

		// Mortar logic here...
		mortarSpell.Cast();
		var mortarDamage = CalculateFinalDamage(mortarSpell.Damage, MortarDamage);
		// Use mortarDamage for the spell
	}
	*/
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
			// Include owner peer id so only the owner's projectile applies damage
			int ownerPeerId = Multiplayer.GetUniqueId();
			Rpc(nameof(SpawnSingleSpellRpc), spell.SpellType, spell.SpawnPosition, spell.Direction, spell.Damage, spell.Speed, ownerPeerId);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void SpawnSingleSpellRpc(string spellType, Vector3 spawnPosition, Vector3 direction, float damage, float speed, int ownerPeerId)
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
			fireball.SetMultiplayerAuthority(ownerPeerId); // ensure consistent authority
			fireball.Initialize(damage, speed, direction, spawnPosition, ownerPeerId);
			fireball.Show();
		}
		else if (spellType == "Magic Wave")
		{
			var magicWave = SpellProjectilePool.Instance?.GetMagicWave();
			if (magicWave == null)
			{
				// Create a new one if pool is empty (shouldn't happen often)
				var magicWaveScene = GD.Load<PackedScene>("res://features/spells/types/MagicWave.tscn");
				magicWave = magicWaveScene.Instantiate<HoardSurvivor3._0.Features.Spells.MagicWave>();
				GetTree().CurrentScene.AddChild(magicWave);
			}
			magicWave.GlobalPosition = spawnPosition;
			magicWave.SetMultiplayerAuthority(ownerPeerId);
			magicWave.Initialize(damage, speed, direction, spawnPosition, ownerPeerId);
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

	public void TakeDamage(float damage)
	{
		if (currentHealth <= 0 || _isInvulnerable) return;

		var actualDamage = damage * (1 - (Armor / (Armor + 100)));
		currentHealth -= actualDamage;
		currentHealth = Mathf.Max(currentHealth, 0);
		
		EmitSignal(nameof(HealthChanged), currentHealth, MaxHealth);

		GD.Print($"Player took {actualDamage} damage, health is now {currentHealth}");

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print("Player has died.");
		// Handle player death, e.g., respawn, show game over screen, etc.
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
			// Request authoritative pickup to avoid duplicate XP and ensure networked despawn
			orb.RequestCollect(Multiplayer.GetUniqueId());
		}
	}

}
