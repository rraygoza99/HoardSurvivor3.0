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
	
	networking.player_list_changed.connect(
		func():
			# First refresh the players based on Steam lobby data
			lobbyMenu.lobbyPlayersList.refreshPlayers(networking.lobbyId)
			
			# Then force an update of the player list with character information
			networking._update_player_list()
	)
	
	pass

# Function to update the lobby players list with character information
func update_lobby_players(players_data: Dictionary):
	lobbyMenu.lobbyPlayersList.update_player(players_data)
	pass
