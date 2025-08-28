extends PanelContainer
class_name LobbyOptions

@export var playButton: Button
@export var magesButton: Button
@export var optionsButton: Button

@export var lobbyMenu: LobbyMenu
@export var mageSelection: MageSelection

func _ready() -> void:
	playButton.pressed.connect(
		func():
			lobbyMenu.show()
			mageSelection.hide()
	)
	magesButton.pressed.connect(
		func():
			lobbyMenu.hide()
			mageSelection.show()
	)
	optionsButton.pressed.connect(
		func():
			lobbyMenu.hide()
			mageSelection.hide()
	)
