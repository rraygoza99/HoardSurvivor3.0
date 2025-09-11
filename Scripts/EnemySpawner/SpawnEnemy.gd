extends Area3D

@export var networking : SteamNetworking
@export var spawn_location: Array[Marker3D]
@export var enemy_spawn_node: Node3D
@export var spawn_distance_min: float = 10.0
@export var spawn_distance_max: float = 25.0
@export var spawn_timer_interval: float = 3.0
@export var max_enemies: int = 20

var _enemy_scene = preload("res://features/enemies/dummy_enemy/dummy_enemy.tscn")
var _chaser_scene = preload("res://features/enemies/chaser_enemy/CocoChaser.tscn")
var _rng := RandomNumberGenerator.new()

@onready var _enemy_spawner = $EnemySpawner

func _ready():
	_rng.randomize()
	if enemy_spawn_node:
		_enemy_spawner.spawn_path = enemy_spawn_node.get_path()
		print("Enemy spawn path set to: ", _enemy_spawner.spawn_path)
	else:
		print("No enemy spawn node assigned.")
	
	if multiplayer.is_server():
		var timer := Timer.new()
		timer.wait_time = spawn_timer_interval
		timer.autostart = true
		timer.one_shot = false
		add_child(timer)
		timer.timeout.connect(_spawn_chaser_enemy)

func _spawn_chaser_enemy():
	# Check if we've reached the maximum enemy count using the pool's count
	var enemy_pool = get_node("/root/EnemyPool")
	if not enemy_pool:
		print("EnemyPool not found!")
		return
		
	if enemy_pool.ActiveEnemyCount >= max_enemies:
		return
	
	# Get the player position
	var player = get_tree().get_first_node_in_group("player")
	if not player:
		print("No player found for enemy spawning")
		return
	
	var player_position = player.global_position
	var spawn_position = _get_random_spawn_position(player_position)
	
	if spawn_position == Vector3.ZERO:
		print("Failed to find valid spawn position")
		return
	
	var chaser_enemy = enemy_pool.GetChaser()
	if not chaser_enemy:
		print("Failed to get chaser from pool")
		return
	
	chaser_enemy.global_position = spawn_position
	chaser_enemy.add_to_group("enemies")
	
	# Spawn on all clients
	spawn_chaser_rpc.rpc(spawn_position)
	print("Spawned chaser enemy at: ", spawn_position, " (Active: ", enemy_pool.ActiveEnemyCount, ", Pooled: ", enemy_pool.PooledEnemyCount, ")")

func _get_random_spawn_position(player_position: Vector3) -> Vector3:
	var max_attempts = 10
	var attempts = 0
	
	while attempts < max_attempts:
		# Generate random angle
		var angle = _rng.randf() * 2.0 * PI
		
		# Generate random distance within range
		var distance = _rng.randf_range(spawn_distance_min, spawn_distance_max)
		
		# Calculate spawn position (start higher than player)
		var spawn_position = Vector3(
			player_position.x + cos(angle) * distance,
			player_position.y + 10.0,  # Start well above player
			player_position.z + sin(angle) * distance
		)
		
		# Find the ground level at this position
		var ground_position = _find_ground_level(spawn_position)
		if ground_position != Vector3.ZERO:
			return ground_position
		
		attempts += 1
	
	# Fallback: return a position even if not ideal
	var fallback_angle = _rng.randf() * 2.0 * PI
	var fallback_distance = spawn_distance_max
	var fallback_position = Vector3(
		player_position.x + cos(fallback_angle) * fallback_distance,
		player_position.y + 10.0,  # Start above player
		player_position.z + sin(fallback_angle) * fallback_distance
	)
	
	# Try to find ground for fallback position too
	var fallback_ground = _find_ground_level(fallback_position)
	if fallback_ground != Vector3.ZERO:
		return fallback_ground
	
	# Last resort: spawn above player level
	return Vector3(
		player_position.x + cos(fallback_angle) * fallback_distance,
		player_position.y + 2.0,  # Just above player
		player_position.z + sin(fallback_angle) * fallback_distance
	)

func _find_ground_level(start_position: Vector3) -> Vector3:
	var space_state = get_world_3d().direct_space_state
	var query = PhysicsRayQueryParameters3D.create(
		start_position,  # Start from the given position
		start_position + Vector3(0, -20, 0)  # Ray down 20 units
	)
	
	# Make sure we're checking for static bodies (ground)
	query.collision_mask = 1  # Assuming ground is on layer 1
	
	var result = space_state.intersect_ray(query)
	
	if result.has("position"):
		# Found ground, spawn slightly above it
		var ground_position = result["position"]
		return Vector3(ground_position.x, ground_position.y + 1.0, ground_position.z)
	
	return Vector3.ZERO  # No ground found

func _is_valid_spawn_position(position: Vector3) -> bool:
	# Check if there's ground below and no obstacles at spawn point
	var space_state = get_world_3d().direct_space_state
	
	# Check for ground below
	var ground_query = PhysicsRayQueryParameters3D.create(
		position + Vector3(0, 1, 0),   # Start slightly above
		position + Vector3(0, -5, 0)   # Ray down 5 units
	)
	ground_query.collision_mask = 1  # Ground layer
	
	var ground_result = space_state.intersect_ray(ground_query)
	if not ground_result.has("position"):
		return false  # No ground found
	
	# Check for obstacles at spawn point (enemy height check)
	var obstacle_query = PhysicsRayQueryParameters3D.create(
		position,  # Start at spawn position
		position + Vector3(0, 2, 0)  # Check 2 units up (enemy height)
	)
	obstacle_query.collision_mask = 1  # Static bodies
	
	var obstacle_result = space_state.intersect_ray(obstacle_query)
	if obstacle_result.has("position"):
		return false  # There's an obstacle where enemy would spawn
	
	return true  # Valid spawn position

func _create_death_timer() -> Timer:
	var time_to_live = Timer.new()
	time_to_live.wait_time = 3
	time_to_live.one_shot = true
	time_to_live.autostart = true
	return time_to_live

@rpc("any_peer", "call_local")
func spawn_chaser_rpc(spawn_position: Vector3):
	# Only spawn on clients (not server, as server already spawned)
	if multiplayer.is_server():
		return
	
	print("Client spawning chaser enemy at: ", spawn_position)
	
	# Get chaser enemy from pool instead of instantiating
	var enemy_pool = get_node("/root/EnemyPool")  # Assuming it's an autoload
	if not enemy_pool:
		print("EnemyPool not found on client!")
		return
	
	var chaser_enemy = enemy_pool.GetChaser()
	if not chaser_enemy:
		print("Failed to get chaser from pool on client")
		return
	
	chaser_enemy.global_position = spawn_position
	chaser_enemy.add_to_group("enemies")

# Legacy spawn method - keeping for compatibility
@rpc("any_peer")
func spawn_enemy(startPos: Vector3, startRot: Vector3):
	print("Spawning enemy at: ", startPos)
	if not multiplayer.is_server():
		print("Not the server, cannot spawn enemy.")
		return
	var packedEnemy: PackedScene = load("res://features/enemies/dummy_enemy/dummy_enemy.tscn")
	var enemy_to_spawn: Node3D = packedEnemy.instantiate()
	enemy_to_spawn.position = startPos
	enemy_to_spawn.rotation = startRot
	var time_to_live = _create_death_timer()
	# Remove the enemy after the TTL; use a valid callable.
	time_to_live.timeout.connect(enemy_to_spawn.queue_free)
	enemy_spawn_node.add_child(enemy_to_spawn, true)
