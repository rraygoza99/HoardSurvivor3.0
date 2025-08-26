extends Node
class_name SteamNetworking

@export var main: Main
@export var gui: Gui

var peer := SteamMultiplayerPeer.new();

var players : Dictionary = {};

var playerSteamName: String;
var playerSteamId: int;

var lobbyId: int;

signal player_list_changed;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	gui.lobby_host_requested.connect(createSteamLobby)
	gui.lobby_play_requested.connect(_on_lobby_play_requested);
	
	Steam.lobby_joined.connect(_on_lobby_joined);
	Steam.lobby_created.connect(_on_lobby_created);
	
	multiplayer.peer_connected.connect(_on_peer_connected);
	multiplayer.peer_disconnected.connect(_on_peer_disconnected);
	pass # Replace with function body.

func createSteamSocketHost():
	print("Creating host..")
	peer.create_host(0)
	multiplayer.multiplayer_peer = peer
	players[multiplayer.get_unique_id()] = playerSteamName
	print("Host created..")
	pass

func connectSteamSocket(remoteSteamId):
	print("Creating client..")
	peer.create_client(remoteSteamId, 0)
	multiplayer.multiplayer_peer = peer
	print("Client created..")
	pass

func createSteamLobby(lobbyType: Steam.LobbyType, maxPlayers: int):
	Steam.createLobby(lobbyType, maxPlayers)
	pass

func _on_lobby_created(connection: int, id: int):
	print("Creatin lobby..")
	if connection != 1:
		print("Failed creating steam lobby")
		return
	Steam.setLobbyJoinable(id, true);
	Steam.setLobbyData(id, "name", playerSteamName + "'s Cool Lobby")
	Steam.allowP2PPacketRelay(true)
	print("Lobby created..")
	createSteamSocketHost()
	pass

func _on_lobby_joined(targetLobbyId, _permissions, _locked, response):
	var lobbyOwnerId = Steam.getLobbyOwner(targetLobbyId);
	print("Attempting to join steam lobby")
	
	if response != 1:
		print("Error joining steam lobby")
		return
	
	lobbyId = targetLobbyId;
	
	if lobbyOwnerId == Steam.getSteamID(): #No need to run logic below for host
		player_list_changed.emit()
		return
	
	connectSteamSocket(lobbyOwnerId);
	players[multiplayer.get_unique_id()] = playerSteamName
	player_list_changed.emit()
	pass

func _on_lobby_play_requested():
	main.start_game()
	pass

func _on_peer_connected(peerId):
	print("Peer connected: ", peerId)
	# Let the new peer know about us
	register_player.rpc_id(peerId, playerSteamName)
	
	# If we are the server and a game is in progress, make sure the new player gets added
	if multiplayer.is_server() and is_instance_valid(main.world):
		print("Server detected new player, checking if game is in progress...")
		# If the world exists, the game has started
		# Schedule the new player to be added to the game
		await get_tree().create_timer(1.0).timeout
		_add_late_joining_player(peerId)
	pass

func _add_late_joining_player(peerId):
	if not multiplayer.is_server():
		return
		
	print("Adding late-joining player: ", peerId)
	
	# Find a spawn position
	var map = main.world.get_node_or_null("TestWorld")
	if not map or not "spawnLocations" in map:
		print("ERROR: Map not found or doesn't have spawn locations")
		return
		
	var spawnPositions = map.spawnLocations
	if spawnPositions.size() == 0:
		print("ERROR: No spawn positions available")
		return
		
	# Choose a random spawn position
	var spawnPoint = spawnPositions[randi() % spawnPositions.size()]
	var startPos = spawnPoint.global_position
	
	# Load the world for this player
	main.load_world.rpc_id(peerId)
	
	# Wait a bit for the world to load
	await get_tree().create_timer(0.5).timeout
	
	# Spawn the player
	main.load_player.rpc_id(peerId, peerId, startPos)
	main.teleport_player.rpc_id(peerId, startPos)
	
	# Inform this player about all existing players
	for existing_player in players:
		if existing_player != peerId and existing_player != multiplayer.get_unique_id():
			# Find the existing player's position
			var existing_player_node = main.world.playersContainer.get_node_or_null(str(existing_player))
			if existing_player_node:
				var pos = existing_player_node.global_position
				# Tell the new player about this existing player
				main.spawn_player.rpc_id(peerId, players[existing_player], pos)
	
	# And inform all existing players about this new player
	for existing_player in players:
		if existing_player != peerId:
			main.spawn_player.rpc_id(existing_player, players[peerId], startPos)

func _on_peer_disconnected(peerId):
	print("Peer disconnected: ", peerId)
	
	# Remove the player from our dictionary
	if players.has(peerId):
		var playerName = players[peerId]
		print("Player ", playerName, " (", peerId, ") disconnected")
		players.erase(peerId)
		player_list_changed.emit()
		
		# If a game is in progress, remove their character too
		if is_instance_valid(main.world):
			var player_node = main.world.playersContainer.get_node_or_null(str(peerId))
			if player_node:
				print("Removing player node for disconnected player")
				player_node.queue_free()
	pass

@rpc("any_peer", "call_local")
func register_player(playerName: String):
	print("Registering player:", playerName)
	var remoteSenderId:= multiplayer.get_remote_sender_id()
	players[remoteSenderId] = playerName
	player_list_changed.emit()
	pass

# Helper function to broadcast an enemy spawn to all clients
# This should be called by enemy spawners when they need to replicate enemies
@rpc("authority", "call_remote")
func broadcast_enemy_spawn(position: Vector3, rotation: Vector3):
	print("SteamNetworking: broadcast_enemy_spawn called")
	
	# Find all EnemySpawnerNode nodes in the scene
	var spawners = get_tree().get_nodes_in_group("EnemySpawner")
	if spawners.size() > 0:
		print("Found ", spawners.size(), " enemy spawners")
		spawners[0].spawn_enemy_network(position, rotation)
	else:
		print("ERROR: No enemy spawners found in the scene")
