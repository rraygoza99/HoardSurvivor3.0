extends Area3D

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
	var enemy_to_spawn = _enemy_scene.instantiate()
	if spawn_location.is_empty():
		return
	var idx := _rng.randi_range(0, spawn_location.size() - 1)
	enemy_to_spawn.position = spawn_location[idx].position
	enemy_to_spawn.rotation = spawn_location[idx].rotation
	var time_to_live = _create_death_timer()
	# Remove the enemy after the TTL; use a valid callable.
	time_to_live.timeout.connect(enemy_to_spawn.queue_free)
	enemy_to_spawn.add_child(time_to_live)
	_enemy_spawner.add_child(enemy_to_spawn, true)

func _create_death_timer() -> Timer:
	var time_to_live = Timer.new()
	time_to_live.wait_time = 3
	time_to_live.one_shot = true
	time_to_live.autostart = true
	return time_to_live
