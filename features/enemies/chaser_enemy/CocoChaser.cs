using Godot;
using System;
using Godot.Collections;

public partial class CocoChaser : CharacterBody3D
{
    [Export] public float Speed {get;set;} = 3.0f;
	[Export] public float Health {get; set;} = 30.0f;
	[Export] public float Damage {get; set;} = 10.0f; // Damage dealt to player
	[Export] public float AttackCooldown {get; set;} = 1.0f; // Time between attacks
	
	[ExportGroup("Loot")]
	[Export] private PackedScene _xpOrbScene;
	[Export] private int _xpAmount = 10;
	[Export] private float _mergeRadius = 1.5f;
	
	private Node3D _player;
	private NavigationAgent3D _navAgent;
	private AnimationTree _animationTree;
	private float _lastAttackTime = 0.0f;
	private float _playerUpdateTimer = 0.0f;
	private const float PLAYER_UPDATE_INTERVAL = 2.0f; // Update target player every 2 seconds
	private int _enemyId = 0; // Unique ID for positioning offset
	private static int _nextEnemyId = 0; // Static counter for unique IDs
	
	public override void _Ready(){
		// Assign unique ID for enemy positioning
		_enemyId = _nextEnemyId++;
		
		_player = FindNearestPlayer();
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		Velocity = Vector3.Zero;
		// Add to enemies group for targeting
		AddToGroup("enemies");
		
		// Setup collision layers - enemies on layer 3, collide with ground and other enemies
		SetCollisionLayerValue(1, false);  // Not on ground layer
		SetCollisionLayerValue(2, false);  // Not on player layer  
		SetCollisionLayerValue(3, true);   // On enemy layer
		SetCollisionMaskValue(1, true);    // Collide with ground/environment
		SetCollisionMaskValue(2, false);   // Don't physically interact with players
		SetCollisionMaskValue(3, true);    // DO collide with other enemies to prevent stacking
		
		// Setup navigation agent after a frame
		CallDeferred(nameof(SetupNavigation));
	}
	private void SetupNavigation()
	{
		if (_navAgent != null)
		{
			_navAgent.PathDesiredDistance = 0.5f;
			_navAgent.TargetDesiredDistance = 1.0f;
		}
	}
	
	private Node3D FindNearestPlayer()
	{
		var players = GetTree().GetNodesInGroup("player");
		Node3D nearestPlayer = null;
		float minDistance = float.MaxValue;
		
		foreach (Node3D player in players)
		{
			if (player == null) continue;
			
			float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearestPlayer = player;
			}
		}
		
		return nearestPlayer;
	}
	
	private Vector3 GetSpreadTargetPosition(Vector3 playerPosition)
	{
		// Create spread pattern around player using enemy ID
		float spread = 2.0f; // Distance from player center
		float angleOffset = (_enemyId * 60.0f) % 360.0f; // 60 degrees apart, wrapping around
		float angleRad = Mathf.DegToRad(angleOffset);
		
		// Calculate offset position in a circle around the player
		Vector3 offset = new Vector3(
			Mathf.Cos(angleRad) * spread,
			0.0f, // Keep on same Y level
			Mathf.Sin(angleRad) * spread
		);
		
		Vector3 targetPosition = playerPosition + offset;
		
		return targetPosition;
	}
	
	private Vector3 CalculateSeparationForce()
	{
		Vector3 separationForce = Vector3.Zero;
		var nearbyEnemies = GetTree().GetNodesInGroup("enemies");
		float separationDistance = 1.5f; // Distance to maintain from other enemies
		int neighborCount = 0;
		
		foreach (Node3D enemy in nearbyEnemies)
		{
			if (enemy == this || enemy == null) continue;
			
			float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
			if (distance < separationDistance && distance > 0.1f) // Avoid division by zero
			{
				// Calculate direction away from this enemy
				Vector3 awayDirection = (GlobalPosition - enemy.GlobalPosition).Normalized();
				// Stronger force when closer
				float force = (separationDistance - distance) / separationDistance;
				separationForce += awayDirection * force;
				neighborCount++;
			}
		}
		
		// Average the separation force
		if (neighborCount > 0)
		{
			separationForce /= neighborCount;
		}
		
		return separationForce;
	}
	
	public override void _PhysicsProcess(double delta){
		if(Health <= 0) return; // Prevent any action if dead
		_lastAttackTime += (float)delta;
		_playerUpdateTimer += (float)delta;
		
		// Update target player periodically to find nearest player
		if (_playerUpdateTimer >= PLAYER_UPDATE_INTERVAL || _player == null)
		{
			_player = FindNearestPlayer();
			_playerUpdateTimer = 0.0f;
		}
		
		if (_player == null || _navAgent == null) {
			return;
		}
		
		// Apply gravity
		Vector3 velocity = Velocity;
		
		// Clamp velocity to prevent insane values (same fix as player)
		if (velocity.Length() > 100.0f)
		{
			velocity = Vector3.Zero;
		}
		
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		
		if (_navAgent.IsNavigationFinished() == false)
		{
			// Use spread positioning instead of exact player position
			Vector3 spreadTarget = GetSpreadTargetPosition(_player.GlobalPosition);
			_navAgent.TargetPosition = spreadTarget;
			Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
			
			Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
			
			// Add separation force to prevent clustering
			Vector3 separationForce = CalculateSeparationForce();
			direction += separationForce * 0.3f; // Mix in 30% separation force
			direction = direction.Normalized();
			
			if (direction.Length() > 0.1f) // Only move if we have a valid direction
			{
				velocity.X = direction.X * Speed;
				velocity.Z = direction.Z * Speed;
				
				// Look at the target
				if (direction != Vector3.Zero)
				{
					LookAt(GlobalPosition + direction, Vector3.Up);
				}
			}
		}
		else
		{
			// Fallback: move directly toward player if navigation fails
			Vector3 directDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
			directDirection.Y = 0; // Keep on same Y level
			
			if (directDirection.Length() > 0.1f)
			{
				velocity.X = directDirection.X * Speed;
				velocity.Z = directDirection.Z * Speed;
				
				if (directDirection != Vector3.Zero)
				{
					LookAt(GlobalPosition + directDirection, Vector3.Up);
				}
			}
		}
		
		Velocity = velocity;
		MoveAndSlide();
		
		// Check for collision with player
		CheckPlayerCollision();
	}
	
	private void CheckPlayerCollision()
	{
		if (_player == null || _lastAttackTime < AttackCooldown) return;
		
		// Check if we're close enough to the player to deal damage
		float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		float damageRange = 1.5f; // Close enough to deal damage
		
		if (distanceToPlayer <= damageRange)
		{
			if (Visible)
			{
				// Try to deal damage to the player
				if (_player.HasMethod("TakeDamage"))
				{
					_player.Call("TakeDamage", Damage);
					_lastAttackTime = 0.0f; // Reset attack cooldown
				}
			}
		}
	}
	private void DropXpOrb()
	{
		if (_xpOrbScene == null) return;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		var sphereShape = new SphereShape3D { Radius = _mergeRadius };
		
		query.Transform = new Transform3D(Basis.Identity, GlobalPosition);
		query.Shape = sphereShape;
		query.CollideWithAreas = true;

		var nearbyObjects = spaceState.IntersectShape(query);
		foreach(Dictionary obj in nearbyObjects){
			if(obj["collider"].As<Node>() is XpOrb existingOrb)
			{
				existingOrb.Combine(_xpAmount);
				return;
			}
		}
		// Spawn orb across the network with a deterministic ID
		SpawnXpOrbNetworked(GlobalPosition, _xpAmount);
	}

	private void SpawnXpOrbNetworked(Vector3 position, int amount)
	{
		// Only authority decides where and when to spawn orbs
		//if (!IsMultiplayerAuthority()) return;
		string orbId = System.Guid.NewGuid().ToString();
		Rpc(nameof(RpcSpawnXpOrb), position, amount, orbId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcSpawnXpOrb(Vector3 position, int amount, string orbId)
	{
		XpOrb orb = _xpOrbScene.Instantiate<XpOrb>();
		orb.SetInitialValue(amount);
		orb.Name = $"XpOrb_{orbId}"; // deterministic name so all peers can reference it
		// Make orbs server-authoritative so collection always executes on the host
		// This guarantees SharedXPManager.GainSharedXp runs on the server and then syncs to all clients.
		try
		{
			orb.SetMultiplayerAuthority(1); // Server peer id is always 1 in Godot
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[CocoChaser] Failed to set orb authority: {ex.Message}");
		}
		GetParent().AddChild(orb);
		orb.GlobalPosition = position;
	}
	public void TakeDamage(float damage){
		Health -= damage;
		if(Health <= 0){
			DropXpOrb();
			Die();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void RpcTakeDamage(float damage)
	{
		// Only the authority should apply health changes; call_local ensures local visual feedback as well
		TakeDamage(damage);
		if (Health <= 0)
		{
			Rpc(nameof(RpcDie));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcDie()
	{
		Die();
	}

	public void Die()
	{
		// Return to pool instead of destroying
		if (EnemyPool.Instance != null)
		{
			EnemyPool.Instance.ReturnChaser(this);
		}
		else
		{
			QueueFree();
		}
	}

	public void ClearTarget()
	{
		_player = null;
	}

	public void Reset()
	{
		// Assign new unique ID for positioning when reset
		_enemyId = _nextEnemyId++;
		
		// Reset all enemy state to default values
		Health = 30.0f;
		Speed = 3.0f;
		Damage = 10.0f;
		AttackCooldown = 1.0f;
		_lastAttackTime = 0.0f;
		_playerUpdateTimer = 0.0f;
		
		// Reset physics
		Velocity = Vector3.Zero;
		
		// Reset navigation
		
		
		// Find nearest player again
		_player = FindNearestPlayer();
		
		// Ensure it's properly added to enemies group
		if (!IsInGroup("enemies"))
		{
			AddToGroup("enemies");
		}
		
		// Setup collision layers - enemies on layer 3, collide with ground and other enemies
		// This allows players to walk through enemies while enemies can still pathfind
		SetCollisionLayerValue(1, false);  // Not on ground layer
		SetCollisionLayerValue(2, false);  // Not on player layer
		SetCollisionLayerValue(3, true);   // On enemy layer
		SetCollisionMaskValue(1, true);    // Collide with ground/environment
		SetCollisionMaskValue(2, false);   // Don't physically interact with players
		SetCollisionMaskValue(3, true);    // DO collide with other enemies to prevent stacking
		
		// Force enemy to snap to the navigation mesh ground
		CallDeferred(nameof(SnapToNavMesh));
	}

	private void SnapToNavMesh()
	{
		// Raycast down to find the navigation mesh floor
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(
			GlobalPosition + Vector3.Up, // Start slightly above
			GlobalPosition + Vector3.Down * 5 // Raycast down 5 units
		);
		// Only check for collisions with the navigation mesh layer (assuming it's layer 1)
		query.CollisionMask = 1; 

		var result = spaceState.IntersectRay(query);
		if (result.ContainsKey("position"))
		{
			var groundPosition = result["position"].AsVector3();
			GlobalPosition = new Vector3(GlobalPosition.X, groundPosition.Y, GlobalPosition.Z);
		}
		else
		{
			// Could not find nav mesh, maybe it's better to just place it slightly above origin
		}
	}
}
