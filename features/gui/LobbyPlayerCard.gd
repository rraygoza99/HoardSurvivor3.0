extends PanelContainer

@onready var name_label = $VBoxContainer/PlayerName
@onready var character_name_label = $VBoxContainer/CharacterName
@onready var character_image = $VBoxContainer/TextureRect

# Setup the player card with the given information
func setup(player_name: String, character_name: String, texture: Texture2D) -> void:
	name_label.text = player_name
	character_name_label.text = character_name
	character_image.texture = texture
