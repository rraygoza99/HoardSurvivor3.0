extends Node

# Store the player's character selection (lowercase ID)
var selected_character: String = "wizgod"

# Signal to notify when character changes
signal character_changed(character_id: String)

# Display names mapping (lowercase id -> display name)
var character_display_names = {
    "wizgod": "Wizgod",
    "alice": "Alice",
    "sam": "Sam", 
    "carl": "Carl",
    "bern": "Bern",
    "dave": "Dave"
}

# Store character data for easy access (using lowercase IDs)
var character_data = {
    "wizgod": {
        "display_name": "Wizgod",
        "image_path": "res://features/gui/MageSelection/Images/Wizgod_portrait.png",
        "model_path": "res://assets/models/player/wizgod/wizgod.glb",
    },
    "alice": {
        "display_name": "Alice",
        "image_path": "res://features/gui/MageSelection/Images/Alice_portrait.png",
        "model_path": "res://assets/models/player/alice/alice.glb",
    },
    "sam": {
        "display_name": "Sam",
        "image_path": "res://features/gui/MageSelection/Images/Sam_portrait.png", 
        "model_path": "res://assets/models/player/sam/sam.glb",
    },
    "carl": {
        "display_name": "Carl",
        "image_path": "res://features/gui/MageSelection/Images/Carl_portrait.png",
        "model_path": "res://assets/models/player/carl/carl.glb",
    },
    "bern": {
        "display_name": "Bern",
        "image_path": "res://features/gui/MageSelection/Images/Bern_portrait.png",
        "model_path": "res://assets/models/player/bern/bern.glb",
    },
    "dave": {
        "display_name": "Dave",
        "image_path": "res://features/gui/MageSelection/Images/Dave_portrait.png",
        "model_path": "res://assets/models/player/dave/dave.glb",
    }
}

func set_selected_character(character_name: String) -> void:
    # Always store lowercase version for internal use
    var old_character = selected_character
    selected_character = character_name.to_lower()
    
    # Emit signal if character changed
    if old_character != selected_character:
        character_changed.emit(selected_character)

func get_selected_character() -> String:
    return selected_character

func get_character_display_name(character_id: String) -> String:
    # Convert to lowercase to ensure consistency
    var id = character_id.to_lower()
    
    # Return the display name if it exists, otherwise return the id
    if character_display_names.has(id):
        return character_display_names[id]
    return character_id
