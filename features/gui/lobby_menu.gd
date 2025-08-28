extends PanelContainer
class_name LobbyMenu

@export var hostButton: Button
@export var joinButton: Button
@export var playButton: Button
@export var leaveButton: Button

@export var lobbies: Control
@export var lobbyList: LobbyList
@export var waitingRoom: Control

@export var lobbyPlayersList: LobbyPlayersList

signal lobby_leave_requested

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	joinButton.pressed.connect(
		func():
			if(lobbyList.selectedRow != null):
				Steam.joinLobby(lobbyList.selectedRow.id)
				lobbies.hide()
				waitingRoom.show()
	)
	hostButton.pressed.connect(
		func():
			lobbies.hide()
			waitingRoom.show()
	)
	leaveButton.pressed.connect(
		func():
			emit_signal("lobby_leave_requested")
			lobbies.show()
			waitingRoom.hide()
	)
	lobbies.show()
	waitingRoom.hide()
	pass # Replace with function body.
