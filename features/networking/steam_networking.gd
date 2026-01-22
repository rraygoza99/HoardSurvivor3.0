extends Node
class_name SteamNetworking

@export var main: Main
@export var gui: Node

var peer := SteamMultiplayerPeer.new();

var players : Dictionary = {};
var player_characters = {} # {player_id: character_name}

var playerSteamName: String;
var playerSteamId: int;

var lobbyId: int;

signal player_list_changed;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	playerSteamName = Steam.getPersonaName()
	gui.lobby_host_requested.connect(createSteamLobby)
	gui.lobby_play_requested.connect(_on_lobby_play_requested)
	gui.lobby_leave_requested.connect(leaveSteamLobby)
	gui.character_selected.connect(_on_character_selected)
	
	# Connect to GameData's character_changed signal
	get_node("/root/GameData").character_changed.connect(_on_character_changed_in_gamedata)

	# Connect to chat update to detect Host Migration
	Steam.lobby_chat_update.connect(_on_lobby_chat_update)
	
	Steam.lobby_joined.connect(_on_lobby_joined)
	Steam.lobby_kicked.connect(_on_lobby_left)
	Steam.lobby_created.connect(_on_lobby_created)
	
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	pass # Replace with function body.

func _process(_delta: float) -> void:
	Steam.run_callbacks()
	pass

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

func leaveSteamLobby():
	if(lobbyId != 0):
		Steam.leaveLobby(lobbyId)
		lobbyId = 0
	else:
		print("Not in a lobby")
		_on_lobby_left()
	pass

func _on_lobby_created(connection: int, id: int):
	print("Creating lobby..")
	if connection != 1:
		print("Failed creating steam lobby")
		return
	Steam.setLobbyJoinable(id, true);
	Steam.setLobbyData(id, "name", playerSteamName + "'s Cool Lobby")
	Steam.allowP2PPacketRelay(true)
	print("Lobby created..")
	createSteamSocketHost()
	
	# Set the player's character from GameData when creating a lobby
	var selected_character = get_node("/root/GameData").get_selected_character()
	player_characters[multiplayer.get_unique_id()] = selected_character
	
	# Update the player list to show the selected character
	_update_player_list()

	# Check ownership
	_update_owner_ui_state()
	pass

func _on_lobby_joined(targetLobbyId, _permissions, _locked, response):
	var lobbyOwnerId = Steam.getLobbyOwner(targetLobbyId);
	print("Attempting to join steam lobby")
	
	if response != 1:
		print("Error joining steam lobby")
		return
	
	lobbyId = targetLobbyId;
	
	# Get the selected character from GameData
	var selected_character = get_node("/root/GameData").get_selected_character()
	player_characters[multiplayer.get_unique_id()] = selected_character

	# Check ownership
	_update_owner_ui_state()
	
	if lobbyOwnerId == Steam.getSteamID(): #No need to run logic below for host
		player_list_changed.emit()
		_update_player_list()  # Update the display immediately for host
		return
	
	connectSteamSocket(lobbyOwnerId);
	players[multiplayer.get_unique_id()] = playerSteamName
	
	# Share the selected character with others in the lobby
	update_player_character.rpc(selected_character)
	player_list_changed.emit()
	pass

func _on_lobby_left():
	print("Left lobby..")
	# Cleanup multiplayer peer
	if multiplayer.multiplayer_peer:
		multiplayer.multiplayer_peer = null
		peer = SteamMultiplayerPeer.new()
	players.clear()
	player_list_changed.emit()
	if gui.has_method("set_owner_mode"):
		gui.set_owner_mode(false)
	pass

func _on_lobby_chat_update(lobby_id: int, _changed_id: int, _making_change_id: int, _chat_state: int) -> void:
	# Only care about updates in our current lobby
	if lobby_id == lobbyId:
		# Someone left or disconnected, potentially changing the owner. Re-check.
		_update_owner_ui_state()

func _update_owner_ui_state() -> void:
	if lobbyId == 0:
		if gui.has_method("set_owner_mode"):
			gui.set_owner_mode(false)
		return

	var owner_id = Steam.getLobbyOwner(lobbyId)
	var my_id = Steam.getSteamID()
	var is_owner = (owner_id == my_id)
	
	# Call the UI to update the button visibility
	# NOTE: We assume 'gui' script (or LobbyMenu) has this method.
	if gui.has_method("set_owner_mode"):
		gui.set_owner_mode(is_owner)
	else:
		print("Warning: 'gui' node does not implement 'set_owner_mode'")

func _on_lobby_play_requested():
	# Store the current character selection before starting game
	main.start_game()
	pass

# Called when returning to the lobby (e.g., after a game ends)
func on_return_to_lobby():
	# Refresh the player list to update character selections
	_update_player_list()
	pass

func _on_peer_connected(peerId):
	register_player.rpc_id(peerId, playerSteamName)
	
	# Always share the current character from GameData
	var selected_character = get_node("/root/GameData").get_selected_character()
	player_characters[multiplayer.get_unique_id()] = selected_character
	update_player_character.rpc_id(peerId, selected_character)
	pass

func _on_peer_disconnected():
	pass

func _on_character_selected(character_name: String):
	# Store the character selection in the local player_characters dictionary
	player_characters[multiplayer.get_unique_id()] = character_name
	
	# Also update in GameData for persistence
	get_node("/root/GameData").set_selected_character(character_name)
	
	# If in a lobby, broadcast the character selection to all peers
	if lobbyId != 0:
		update_player_character.rpc(character_name)

	# Update the lobby display right away
	_update_player_list()
	if multiplayer.is_server() and lobbyId != 0:
		Steam.setLobbyData(lobbyId, "host_character", character_name)
	pass
	
# Called when character is changed via GameData
func _on_character_changed_in_gamedata(character_id: String):
	# Update our local character and broadcast to others
	player_characters[multiplayer.get_unique_id()] = character_id
	
	# If in a lobby, broadcast the character selection to all peers
	if lobbyId != 0:
		update_player_character.rpc(character_id)
		
	# Update the lobby display right away
	_update_player_list()
	pass

@rpc("any_peer")
func update_player_character(character_name: String):
	var sender_id = multiplayer.get_remote_sender_id()
	player_characters[sender_id] = character_name
	
	# If this is the local player's character update, store it in GameData
	if sender_id == multiplayer.get_unique_id():
		get_node("/root/GameData").set_selected_character(character_name)
		
	# Update the UI
	_update_player_list()
	pass

func _update_player_list():
	var players_data = {}
	for player_id in players:
		# Get character from player_characters or use default
		var character = "wizgod"  # Default lowercase ID
		
		if player_characters.has(player_id):
			character = player_characters[player_id]
		
		# For the local player, always use the character from GameData to ensure consistency
		if player_id == multiplayer.get_unique_id():
			character = get_node("/root/GameData").get_selected_character()
			# Make sure our local dictionary is also updated
			player_characters[player_id] = character
		
		players_data[player_id] = {
			"name": players[player_id],
			"character": character
		}
	
	gui.update_lobby_players(players_data)
	pass

@rpc("any_peer", "call_local")
func register_player(playerName: String):
	print("Registering player:", playerName)
	var remoteSenderId:= multiplayer.get_remote_sender_id()
	players[remoteSenderId] = playerName
	
	# If this is the local player, make sure their character is set from GameData
	if remoteSenderId == multiplayer.get_unique_id():
		var selected_character = get_node("/root/GameData").get_selected_character()
		player_characters[remoteSenderId] = selected_character
	
	_update_player_list()
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
