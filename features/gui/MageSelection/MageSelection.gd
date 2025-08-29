extends Control
class_name MageSelection

@onready var grid_container = $MageGridContainer/GridContainer
@onready var mage_model_container = $MageModelViewport/SubViewport/MageModelContainer
@onready var mage_name_label = $MageInfo/VBoxContainer/MageName
@onready var mage_description_label = $MageInfo/VBoxContainer/MageDescription
@onready var mage_skills_label = $MageInfo/VBoxContainer/MageSkills

var mage_item_scene = preload("res://features/gui/MageSelection/MageItem.tscn")
var current_mage_model: Node3D = null
var current_mage_id: String = ""

# Signal when a mage is selected
signal mage_selected(mage_id: String)

# Example mage data - in a real game you might load this from a file or resource
var mage_data_list = [
	{
		"id": "wizgod",
		"name": "WizGod",
		"description": "Es como un God, pero más sabroso.",
		"image_path": "res://features/gui/MageSelection/Images/Wizgod_portrait.png",
		"model_path": "res://assets/models/player/wizgod/wizgod.glb",
		"skills": ["Fireball"]
	},
	{
		"id": "dave",
		"name": "Dave",
		"description": "Controls the battlefield with ice and slowing effects.",
		"image_path": "res://features/gui/MageSelection/Images/Dave_portrait.png",
		"model_path": "res://assets/models/player/dave/dave.glb",
		"skills": ["Ice Spike", "Frost Nova", "Blizzard"]
	},
	{
		"id": "alice",
		"name": "Alice",
		"description": "Delivers quick, precise damage with lightning spells.",
		"image_path": "res://features/gui/MageSelection/Images/Alice_portrait.png",
		"model_path": "res://assets/models/player/alice/alice.glb",
		"skills": ["Lightning Bolt", "Chain Lightning", "Thunder Storm"]
	},
	{
		"id": "sam",
		"name": "Sam",
		"description": "Durable caster with protective and control abilities.",
		"image_path": "res://features/gui/MageSelection/Images/Sam_portrait.png",
		"model_path": "res://assets/models/player/sam/sam.glb",
		"skills": ["Rock Throw", "Earthquake", "Stone Wall"]
	},
	{
		"id": "carl",
		"name": "Carl",
		"description": "Manipulates pure magical energy for complex effects.",
		"image_path": "res://features/gui/MageSelection/Images/Carl_portrait.png",
		"model_path": "res://assets/models/player/carl/carl.glb",
		"skills": ["Magic Missile", "Arcane Explosion", "Time Warp"]
	},
	{
		"id": "bern",
		"name": "Bern",
		"description": "Combines healing and poison abilities with plant control.",
		"image_path": "res://features/gui/MageSelection/Images/Bern_portrait.png",
		"model_path": "res://assets/models/player/bern/bern.glb",
		"skills": ["Healing Touch", "Entangling Roots", "Poison Spray"]
	},
]

func _ready():
	populate_mage_grid()
	
	# Get the currently selected character from GameData
	var selected_character_id = get_node("/root/GameData").get_selected_character()
	
	# Select the current character or default to wizgod if not found
	select_mage_by_id(selected_character_id)

func populate_mage_grid():
	# Clear any existing children first
	for child in grid_container.get_children():
		child.queue_free()
	
	# Create mage items from our list
	for mage_info in mage_data_list:
		# Instance the mage item scene
		var mage_item = mage_item_scene.instantiate()
		grid_container.add_child(mage_item)
		
		# Set up the mage item with our data
		mage_item.setup(mage_info)
		
		# Connect the signal
		mage_item.mage_selected.connect(_on_mage_item_selected)

func _on_mage_item_selected(mage_id: String):
	# Convert to lowercase for consistency before selecting
	select_mage_by_id(mage_id.to_lower())

func select_mage_by_id(mage_id: String):
	# Convert mage_id to lowercase for consistent handling
	var lowercase_mage_id = mage_id.to_lower()
	
	# Don't do anything if this mage is already selected
	if current_mage_id == lowercase_mage_id:
		return
		
	current_mage_id = lowercase_mage_id
	
	# Find the mage data
	var mage_data = null
	for data in mage_data_list:
		if data.id == lowercase_mage_id:
			mage_data = data
			break
	
	if mage_data == null:
		print("Mage with ID ", lowercase_mage_id, " not found")
		return
	
	# Update the UI with mage info
	update_mage_info(mage_data)
	
	# Load and display the 3D model
	load_mage_model(mage_data)
	
	# Store the selection in the global GameData
	var character_name = mage_data.name
	get_node("/root/GameData").set_selected_character(character_name)
	
	# Forward the signal - emit lowercase ID
	mage_selected.emit(lowercase_mage_id)

func update_mage_info(mage_data):
	# Update UI elements
	mage_name_label.text = mage_data.name
	mage_description_label.text = mage_data.description
	
	# Format skills list
	var skills_text = "Skills: "
	for i in range(mage_data.skills.size()):
		if i > 0:
			skills_text += ", "
		skills_text += mage_data.skills[i]
	mage_skills_label.text = skills_text

func load_mage_model(mage_data):
	# Remove current model if it exists
	if current_mage_model != null:
		current_mage_model.queue_free()
		current_mage_model = null
	
	# Check if model path exists and is valid
	if "model_path" in mage_data and not mage_data.model_path.is_empty():
		# Default to wizgod model for testing if the specified model doesn't exist
		var model_path = mage_data.model_path
		if not ResourceLoader.exists(model_path):
			model_path = "res://assets/models/player/wizgod/wizgod.glb"
			print("Model not found, using default: ", model_path)
		
		# Try to load the model
		var model_resource = load(model_path)
		if model_resource:
			current_mage_model = model_resource.instantiate()
			mage_model_container.add_child(current_mage_model)
			
			# Position and scale the model appropriately
			current_mage_model.scale = Vector3(0.5, 0.5, 0.5)
			current_mage_model.position = Vector3(0, 0, 0)
			current_mage_model.rotation_degrees = Vector3(0, 180, 0)  # Turn the model to face the camera
			
			# Find and play the idle animation if it exists
			var animation_player = find_animation_player(current_mage_model)
			if animation_player:
				# Check if "idle" animation exists
				if animation_player.has_animation("idle"):
					animation_player.play("idle")
				elif animation_player.has_animation("Idle"):
					animation_player.play("Idle")
				else:
					# Try to find an animation that might be the idle animation
					var animations = animation_player.get_animation_list()
					for anim in animations:
						if anim.to_lower().contains("idle"):
							animation_player.play(anim)
							break
					
					# If no idle animation found, play the first animation if any exist
					if animations.size() > 0 and not animation_player.is_playing():
						animation_player.play(animations[0])
				
				print("Playing animation for model: ", mage_data.name)
			else:
				print("No AnimationPlayer found for model: ", mage_data.name)
		else:
			print("Failed to load model: ", model_path)

# Helper function to find the AnimationPlayer node in the model
func find_animation_player(node):
	# Check if this node is an AnimationPlayer
	if node is AnimationPlayer:
		return node
	
	# Recursively search through all children
	for child in node.get_children():
		var found = find_animation_player(child)
		if found:
			return found
	
	return null
