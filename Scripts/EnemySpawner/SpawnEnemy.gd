extends Area3D

@export var networking : SteamNetworking
@export var spawn_location: Array[Marker3D]
@export var enemy_spawn_node: Node3D
var _enemy_scene = preload("res://features/enemies/dummy_enemy/dummy_enemy.tscn")
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
		timer.wait_time = 5.0
		timer.autostart = true
		timer.one_shot = false
		add_child(timer)
		timer.timeout.connect(_create_enemy)

func _create_enemy():
	var packedEnemy: PackedScene = load("res://features/enemies/dummy_enemy/dummy_enemy.tscn")
	var enemy_to_spawn: Node3D = packedEnemy.instantiate()

	if spawn_location.is_empty():
		return
	var idx := _rng.randi_range(0, spawn_location.size() - 1)
	enemy_to_spawn.position = spawn_location[idx].position
	enemy_to_spawn.rotation = spawn_location[idx].rotation
	var time_to_live = _create_death_timer()
	# Remove the enemy after the TTL; use a valid callable.
	time_to_live.timeout.connect(enemy_to_spawn.queue_free)
	
	enemy_spawn_node.add_child(enemy_to_spawn, true)
	spawn_enemy.rpc(enemy_to_spawn.position, enemy_to_spawn.rotation)

func _create_death_timer() -> Timer:
	var time_to_live = Timer.new()
	time_to_live.wait_time = 3
	time_to_live.one_shot = true
	time_to_live.autostart = true
	return time_to_live

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
	pass
