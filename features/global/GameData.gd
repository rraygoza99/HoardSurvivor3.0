extends Node

# Store the player's character selection (lowercase ID)
var selected_character: String = "wizgod"

# Shared XP and Level system - all players have the same progression
var shared_current_xp: int = 0
var shared_xp_to_next_level: int = 100
var shared_current_level: int = 1

# Signal to notify when character changes
signal character_changed(character_id: String)

# Signals for shared XP system
signal shared_xp_gained(amount: int, total_xp: int)
signal shared_level_up(new_level: int)
signal shared_xp_changed(current_xp: int, xp_to_next: int, level: int)

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
        "image_path": "res://features/gui/MageSelection/Images/dave_portrait.png",
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



func level_up_shared() -> void:
    shared_current_xp -= shared_xp_to_next_level
    shared_xp_to_next_level = int(shared_xp_to_next_level * 1.5)  # Increase XP requirement
    shared_current_level += 1
    
    print("Shared Level Up! New level: ", shared_current_level)
    print("XP to next level: ", shared_xp_to_next_level)
    
    # Emit signals
    shared_level_up.emit(shared_current_level)
    broadcast_level_up.rpc(shared_current_level) # RPC to all clients
    shared_xp_changed.emit(shared_current_xp, shared_xp_to_next_level, shared_current_level)

@rpc("any_peer", "call_local")
func sync_xp_data(current_xp: int, xp_to_next: int, level: int):
    if multiplayer.is_server():
        return

    shared_current_xp = current_xp
    shared_xp_to_next_level = xp_to_next
    shared_current_level = level
    shared_xp_changed.emit(shared_current_xp, shared_xp_to_next_level, shared_current_level)

# Shared XP System Functions
@rpc("authority", "call_local")
func gain_shared_xp(amount: int) -> void:
    if not multiplayer.is_server():
        return
        
    shared_current_xp += amount
    print("Shared XP gained: ", amount, " Total: ", shared_current_xp)
    
    # Check for level up
    while shared_current_xp >= shared_xp_to_next_level:
        level_up_shared()
    
    # Emit signal for UI updates
    shared_xp_changed.emit(shared_current_xp, shared_xp_to_next_level, shared_current_level)
    shared_xp_gained.emit(amount, shared_current_xp)
    sync_xp_data.rpc(shared_current_xp, shared_xp_to_next_level, shared_current_level)

@rpc("authority", "call_local")
func broadcast_level_up(new_level: int):
    if multiplayer.is_server():
        return # Host already emitted this
    shared_level_up.emit(new_level)

func get_shared_xp_progress() -> Dictionary:
    return {
        "current_xp": shared_current_xp,
        "xp_to_next_level": shared_xp_to_next_level,
        "current_level": shared_current_level,
        "xp_percentage": float(shared_current_xp) / float(shared_xp_to_next_level) if shared_xp_to_next_level > 0 else 0.0
    }