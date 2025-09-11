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
	//[Export] private PackedScene _xpOrbScene;
	[Export] private int _xpAmount = 10;
	[Export] private float _mergeRadius = 1.5f;
	
	private Node3D _player;
	private NavigationAgent3D _navAgent;
	private AnimationTree _animationTree;
	private float _lastAttackTime = 0.0f;
	
	public override void _Ready(){
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		Velocity = Vector3.Zero;
		// Add to enemies group for targeting
		AddToGroup("enemies");
		
		// Setup navigation agent after a frame
		CallDeferred(nameof(SetupNavigation));
		
		GD.Print($"CocoChaser _Ready called. Player found: {_player != null}, NavAgent: {_navAgent != null}");
	}
	
	private void SetupNavigation()
	{
		if (_navAgent != null)
		{
			_navAgent.PathDesiredDistance = 0.5f;
			_navAgent.TargetDesiredDistance = 1.0f;
		}
	}
	
	public override void _PhysicsProcess(double delta){
		if(_player == null) {
			_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
			if (_player == null) {
				return;
			}
		}
		
		if (_navAgent == null) {
			return;
		}
		
		_lastAttackTime += (float)delta;
		
		// Apply gravity
		Vector3 velocity = Velocity;
		
		// Clamp velocity to prevent insane values (same fix as player)
		if (velocity.Length() > 100.0f)
		{
			GD.Print($"WARNING: Enemy velocity too high ({velocity}), resetting to zero");
			velocity = Vector3.Zero;
		}
		
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		
		if (_navAgent.IsNavigationFinished() == false)
		{
			_navAgent.TargetPosition = _player.GlobalPosition;
			Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
			
			Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
			
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
			// Try to deal damage to the player
			if (_player.HasMethod("TakeDamage"))
			{
				_player.Call("TakeDamage", Damage);
				_lastAttackTime = 0.0f; // Reset attack cooldown
				GD.Print($"Enemy dealt {Damage} damage to player");
			}
		}
	}
	/*private void DropXpOrb()
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
		XpOrb newOrb = _xpOrbScene.Instantiate<XpOrb>();
		newOrb.SetInitialValue(_xpAmount);
		GetParent().AddChild(newOrb); // Add to the main scene
		newOrb.GlobalPosition = this.GlobalPosition;
	}*/
	public void TakeDamage(float damage){
		Health -= damage;
		GD.Print($"CocoChaser took {damage} damage. Health: {Health}");
		if(Health <= 0){
			//DropXpOrb();
			GD.Print("CocoChaser destroyed");
			Die();
		}
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

	public void Reset()
	{
		// Reset all enemy state to default values
		Health = 30.0f;
		Speed = 3.0f;
		Damage = 10.0f;
		AttackCooldown = 1.0f;
		_lastAttackTime = 0.0f;
		
		// Reset physics
		Velocity = Vector3.Zero;
		
		// Reset navigation
		if (_navAgent != null)
		{
			_navAgent.TargetPosition = Vector3.Zero;
		}
		
		// Find player again
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		
		// Ensure it's properly added to enemies group
		if (!IsInGroup("enemies"))
		{
			AddToGroup("enemies");
		}
		
		// Enable collision
		SetCollisionLayerValue(1, true);
		SetCollisionMaskValue(1, true);
		
		GD.Print("CocoChaser reset to default state");
	}
}
