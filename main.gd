extends Node
class_name Main

@export var networking : SteamNetworking
@export var world: WorldState

signal game_started;
signal game_ended;
signal player_teleport(position: Vector3);
signal enemy_teleport(position: Vector3);

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	Steam.steamInit(true, 3965800);
	
	networking.playerSteamId = Steam.getSteamID();
	networking.playerSteamName = Steam.getPersonaName();
	
	Steam.requestLobbyList();
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	Steam.run_callbacks();
	pass

func start_game():
	if not multiplayer.is_server():
		return
	print("Starting game..")
	var spawnLocation := 0;

	var packedMap: PackedScene = load("res://features/world/testMap/testWorld.tscn")
	var mapScene: TestWorld = packedMap.instantiate()
	world.add_child(mapScene)

	var spawnPositions: Array[Marker3D] = mapScene.spawnLocations;	
	var spawnCount := spawnPositions.size()
	
	if spawnCount == 0:
		print("ERROR: No spawn positions found in the map!")
		return
		
	print("Found ", spawnCount, " spawn positions")
	
	# First, make sure all clients have the world loaded
	for player in networking.players:
		if player != multiplayer.get_unique_id():
			load_world.rpc_id(player)
	
	# Wait a bit to ensure worlds are loaded before spawning players
	await get_tree().create_timer(0.5).timeout
	var player_positions = {}
	var plasyersArray = networking.players.keys()
	# Then spawn all players
	for i in range(plasyersArray.size()):
		var player = plasyersArray[i]
		var spawnIndex = i % spawnCount

		var startPos: Vector3
		if spawnIndex < spawnCount:
			startPos = spawnPositions[spawnIndex].global_position
		else:
			var basePos = spawnPositions[spawnIndex % spawnCount].global_position
			startPos = Vector3(basePos.x + (spawnIndex * 1.5), basePos.y, basePos.z + (spawnIndex * 1.5))
		player_positions[player] = startPos
		print("Player ", player, " will spawn at ", startPos)

	for player in networking.players:
		# Get a unique spawn position with wraparound if needed
		var startPos = player_positions[player]
		print("Spawning player ", player, " at position ", startPos)
		# Load the player for this peer with their specific spawn position
		load_player.rpc_id(player, player, startPos)
		teleport_player.rpc_id(player, startPos)
		# If this is the server's player, we need to make sure clients know about it too
		for other_player in networking.players:
			if other_player != player:
				spawn_player.rpc_id(other_player, networking.playerSteamName, startPos)
	pass

@rpc("call_local")
func load_player(peerId: int, startPos: Vector3):
	print("Loading player ", peerId, " at position ", startPos)
	var packedPlayer: PackedScene = load("res://features/player/player.tscn")
	var playerScene: Node3D = packedPlayer.instantiate()
	playerScene.name = str(peerId)
	playerScene.MultiplayerAuthority = peerId
	playerScene.StartPosition = startPos
	world.addPlayer(playerScene)
	
	# Notify all other players about this player
	# But only do this for the local player to avoid duplicates
	if multiplayer.get_unique_id() == peerId:
		game_started.emit()
	
	print("Player ", peerId, " loaded")
	pass

@rpc("call_local")
func load_world():
	print("Loading world..")
	var packedMap: PackedScene = load("res://features/world/testMap/testWorld.tscn")
	var mapScene: Node3D = packedMap.instantiate()
	world.add_child(mapScene)
	print("World loaded..")
	pass

@rpc("any_peer")
func spawn_player(playerId: int, steamName: String, startPos: Vector3):
	var senderId := multiplayer.get_remote_sender_id()
	print("Spawning remote Player: ", steamName, " (ID: ", senderId, ") at position ", startPos)
	
	# Check if the player already exists (to prevent duplicates)
	var existingPlayer = world.playersContainer.get_node_or_null(str(playerId))
	if existingPlayer:
		print("Player ", playerId, " already exists, not spawning again")
		return
		
	var packedPlayer: PackedScene = load("res://features/player/player.tscn")
	var playerScene: Node3D = packedPlayer.instantiate()
	playerScene.name = str(playerId)
	playerScene.MultiplayerAuthority = playerId
	playerScene.StartPosition = startPos
	world.addPlayer(playerScene)
	print("Remote player ", playerId, " added successfully")
	pass
	
@rpc("call_local")
func teleport_player(position: Vector3):
	print("Teleporting player..")
	player_teleport.emit(position)
	print("Player teleported..")
	pass
