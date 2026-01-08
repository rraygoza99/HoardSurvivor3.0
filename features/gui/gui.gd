extends Control
class_name Gui

@export var main: Main
@export var debugMode: bool

@export var networking: SteamNetworking

@export var lobbyMenu: LobbyMenu
@export var lobbyOptions: LobbyOptions

signal lobby_host_requested
signal lobby_play_requested
signal lobby_leave_requested
signal character_selected(character_name: String)

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	lobbyMenu.hostButton.pressed.connect(
		func():
			lobby_host_requested.emit(Steam.LobbyType.LOBBY_TYPE_PUBLIC, 5)
	)
	lobbyMenu.playButton.pressed.connect(
		func():
			lobby_play_requested.emit()
	)
	lobbyMenu.leaveButton.pressed.connect(
		func():
			lobby_leave_requested.emit()
	)
	
	lobbyMenu.character_selected.connect(
		func(character: String):
			character_selected.emit(character)
	)
	lobbyMenu.character_selected.connect(character_selected)
	
	main.game_started.connect(
		func():
			lobbyOptions.hide()
			lobbyMenu.hide()
	)
	main.game_ended.connect(
		func():
			lobbyOptions.show()
			lobbyMenu.show()
	)
	
	# The networking layer should be responsible for providing complete player data.
	# The GUI's role is to display it.
	networking.player_list_changed.connect(
		func():
			# First refresh the players based on Steam lobby data
			lobbyMenu.lobbyPlayersList.refreshPlayers(networking.lobbyId)
			
			# Then force an update of the player list with character information
			networking._update_player_list()
		func(players_data: Dictionary):
			# This assumes `player_list_changed` now provides all necessary data.
			# The `SteamNetworking` class should be updated to emit this dictionary.
			update_lobby_players(players_data)
	)
	
	pass

# Function to update the lobby players list with character information
func update_lobby_players(players_data: Dictionary):
	lobbyMenu.lobbyPlayersList.update_player(players_data)
	pass
	# It seems more intuitive for this function to clear and rebuild the list
	# or for the LobbyPlayersList to handle diffing.
	# Renaming `update_player` to `update_players` in LobbyPlayersList is recommended.
	lobbyMenu.lobbyPlayersList.update_players(players_data)
