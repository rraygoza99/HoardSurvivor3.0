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

	for player in networking.players:
		if player != multiplayer.get_unique_id():
			load_world.rpc_id(player)
			
		var startPos := spawnPositions[spawnLocation].global_position;
		
		load_player.rpc_id(player, player, startPos)
		teleport_player.rpc_id(player, startPos)
		spawnLocation += 1
	pass
	
func end_game():
	# Clean up the world
	for child in world.get_children():
		world.remove_child(child)
		child.queue_free()
	
	# Signal that the game has ended
	game_ended.emit()
	
	# Update the lobby with the latest character selections
	networking.on_return_to_lobby()
	pass

@rpc("call_local")
func load_player(peerId: int, startPos: Vector3):
	print("Loading player..")
	
	# Get the player's selected character
	var character_id = "wizgod"  # Default lowercase ID
	if networking.player_characters.has(peerId):
		character_id = networking.player_characters[peerId]
	
	# Convert first letter to uppercase for scene path (wizgod -> Wizgod)
	var character_name = character_id.capitalize()
	print("Loading character: ", character_name, " (ID: ", character_id, ")")
	
	# Load the appropriate character scene based on selection
	var character_path = "res://features/player/characters/scenes/%s.tscn" % character_name
	var packedPlayer: PackedScene
	
	# Check if the character scene exists
	if ResourceLoader.exists(character_path):
		print("Loading character scene from: ", character_path)
		packedPlayer = load(character_path)
	else:
		# Fall back to default character if selected one doesn't exist
		print("Character scene not found, falling back to Wizgod")
		packedPlayer = load("res://features/player/characters/scenes/Wizgod.tscn")
	
	var playerScene: Node3D = packedPlayer.instantiate()
	playerScene.name = str(peerId) + "_" + character_name  # Add character name to the node name
	playerScene.MultiplayerAuthority = peerId
	playerScene.StartPosition = startPos
	world.addPlayer(playerScene)
	
	spawn_player.rpc(networking.playerSteamName, startPos, character_name)
	
	game_started.emit()
	print("Player loaded..")
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
func spawn_player(steamName: String, startPos: Vector3, character_name: String = "Wizgod"):
	print("Spawning remote Player: ", steamName, " as character: ", character_name)
	var senderId := multiplayer.get_remote_sender_id()
	
	# Ensure character_name has proper capitalization (for scene path)
	if character_name.to_lower() == character_name:
		character_name = character_name.capitalize()
	
	# Load the appropriate character scene based on selection
	var character_path = "res://features/player/characters/scenes/%s.tscn" % character_name
	var packedPlayer: PackedScene
	
	# Check if the character scene exists
	if ResourceLoader.exists(character_path):
		print("Loading remote character scene from: ", character_path)
		packedPlayer = load(character_path)
	else:
		# Fall back to default character if selected one doesn't exist
		print("Remote character scene not found, falling back to Wizgod")
		packedPlayer = load("res://features/player/characters/scenes/Wizgod.tscn")
	
	var playerScene: Node3D = packedPlayer.instantiate()
	playerScene.name = str(senderId) + "_" + character_name  # Add character name to the node name
	playerScene.MultiplayerAuthority = senderId
	playerScene.StartPosition = startPos
	world.addPlayer(playerScene)
	pass
	
@rpc("call_local")
func teleport_player(position: Vector3):
	print("Teleporting player..")
	player_teleport.emit(position)
	print("Player teleported..")
	pass
