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
signal character_selected(character_name: String)

var selected_character = "wizgod" # Now using lowercase IDs
var mage_selection_scene = preload("res://features/gui/MageSelection/MageSelection.tscn")
var current_mage_selection = null

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	# Get the selected character from GameData
	selected_character = get_node("/root/GameData").get_selected_character()
	# Emit the character selection signal to update the networking
	emit_signal("character_selected", selected_character)
	
	# Connect the Change Character button
	var change_character_button = $WaitingRoom/VBoxContainer/ChangeCharacterButton
	if change_character_button:
		change_character_button.pressed.connect(_on_change_character_pressed)
	
	joinButton.pressed.connect(
		func():
			if(lobbyList.selectedRow != null):
				# Get the current selected character from GameData
				selected_character = get_node("/root/GameData").get_selected_character()
				# Emit character selected signal to ensure it's set in the lobby
				emit_signal("character_selected", selected_character)
				# Join the lobby
				Steam.joinLobby(lobbyList.selectedRow.id)
				lobbies.hide()
				waitingRoom.show()
	)
	hostButton.pressed.connect(
		func():
			# Get the current selected character from GameData
			selected_character = get_node("/root/GameData").get_selected_character()
			# Emit character selected signal to ensure it's set in the lobby
			emit_signal("character_selected", selected_character)
			# Show the waiting room
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

func _on_character_selected(character_name: String) -> void:
	selected_character = character_name
	emit_signal("character_selected", selected_character)

func _on_change_character_pressed() -> void:
	# Hide the lobby menu temporarily
	self.hide()
	
	# Create the MageSelection scene if it doesn't exist
	if not current_mage_selection:
		current_mage_selection = mage_selection_scene.instantiate()
		get_parent().add_child(current_mage_selection)
		
		# Connect to the mage_selected signal
		current_mage_selection.mage_selected.connect(_on_mage_selection_mage_selected)
		
		# Add a back button to return to the lobby
		var back_button = Button.new()
		back_button.text = "Back to Lobby"
		back_button.pressed.connect(_on_back_to_lobby_pressed)
		
		# Position the back button at the top of the screen
		back_button.size_flags_horizontal = Control.SIZE_SHRINK_END
		back_button.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
		
		# Add the back button to the MageSelection scene
		current_mage_selection.add_child(back_button)
		
		# Position the back button in the top-right corner
		back_button.custom_minimum_size = Vector2(150, 40)
		back_button.position = Vector2(get_viewport_rect().size.x - 170, 20)
	else:
		# Show the existing MageSelection scene
		current_mage_selection.show()
		
		# Update the selected character in the MageSelection scene
		current_mage_selection.select_mage_by_id(selected_character)

func _on_mage_selection_mage_selected(mage_id: String) -> void:
	# Update the selected character
	selected_character = mage_id
	
	# Emit the character selection signal to update the networking
	emit_signal("character_selected", selected_character)

func _on_back_to_lobby_pressed() -> void:
	# Hide the MageSelection scene
	if current_mage_selection:
		current_mage_selection.hide()
	
	# Update the selected character from GameData
	selected_character = get_node("/root/GameData").get_selected_character()
	
	# Emit the character selection signal to update the networking
	emit_signal("character_selected", selected_character)
	
	# Show the lobby menu again
	self.show()
