class_name MageItem
extends VBoxContainer

signal mage_selected(mage_name: String)

# We'll store mage data as a dictionary instead of typed class to avoid dependencies
var mage_data = {}
var selected = false

@onready var texture_rect = $PanelContainer/TextureRect
@onready var name_label = $NameLabel

func _ready():
	# Make the texture clickable
	texture_rect.gui_input.connect(_on_texture_input)
	# Apply initial styling
	set_selected(false)
	
	# Apply any data that was set before the node was ready
	if !mage_data.is_empty():
		apply_data()

func setup(data):
	mage_data = data
	
	# Store the data and apply it either now (if ready) or later (in _ready)
	if is_inside_tree() and name_label != null:
		# Node is ready, apply immediately
		apply_data()
	# Otherwise, the data will be applied in _ready()
	
func apply_data():
	# Apply the stored data to the UI elements
	if mage_data.has("name"):
		name_label.text = mage_data.name
	
	if mage_data.has("image_path") and not mage_data.image_path.is_empty():
		# Load the image if provided
		var texture = load(mage_data.image_path)
		if texture and texture_rect != null:
			texture_rect.texture = texture

func _on_texture_input(event):
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		# Handle click - emit signal with mage name
		mage_selected.emit(mage_data.id)

func set_selected(is_selected):
	selected = is_selected
	if selected:
		# Visual feedback for selected state (e.g., highlight)
		modulate = Color(1.2, 1.2, 1.2) # Slightly brighter
		# Add a colored outline or background
		texture_rect.modulate = Color(1.0, 1.0, 1.0, 1.0)
		name_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.2))
	else:
		# Normal state
		modulate = Color(1.0, 1.0, 1.0)
		texture_rect.modulate = Color(0.8, 0.8, 0.8, 1.0) # Slightly dimmed
		name_label.remove_theme_color_override("font_color")
		
# Legacy method for compatibility
func _on_select_button_pressed():
	if mage_data.has("name"):
		mage_selected.emit(mage_data.id)
