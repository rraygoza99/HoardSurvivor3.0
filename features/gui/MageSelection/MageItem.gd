class_name MageItem
extends VBoxContainer

signal mage_selected(mage_id: String)

# We'll store mage data as a dictionary instead of typed class to avoid dependencies
var mage_data = {}

@onready var texture_rect = $TextureRect
@onready var name_label = $NameLabel

func setup(data):
	mage_data = data
	name_label.text = data.name
	
	if data.image_path.is_empty():
		return
		
	# Load the image if provided
	var texture = load(data.image_path)
	if texture:
		texture_rect.texture = texture

func _on_select_button_pressed():
	if mage_data.has("id"):
		mage_selected.emit(mage_data.id)
