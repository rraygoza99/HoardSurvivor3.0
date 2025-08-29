extends GridContainer
class_name LobbyPlayersList

var players_data = {}

# Character ID mapping (display name -> lowercase id)
var character_id_map = {
	"Wizgod": "wizgod",
	"Alice": "alice",
	"Sam": "sam",
	"Carl": "carl",
	"Bern": "bern",
	"Dave": "dave",
	"DefaultWizard": "wizgod"
}

var character_textures = {
	"wizgod": preload("res://features/gui/MageSelection/Images/Wizgod_portrait.png"),
	"alice": preload("res://features/gui/MageSelection/Images/Alice_portrait.png"),
	"sam": preload("res://features/gui/MageSelection/Images/Sam_portrait.png"),
	"carl": preload("res://features/gui/MageSelection/Images/Carl_portrait.png"),
	"bern": preload("res://features/gui/MageSelection/Images/Bern_portrait.png"),
	"dave": preload("res://features/gui/MageSelection/Images/Dave_portrait.png"),
	"defaultwizard": preload("res://features/gui/MageSelection/Images/Wizgod_portrait.png")
}

# The scene for each player card
var player_card_scene = preload("res://features/gui/LobbyPlayerCard.tscn")

func _ready() -> void:
	# Setup the grid properties
	columns = 3
	add_theme_constant_override("h_separation", 15)
	add_theme_constant_override("v_separation", 15)
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL
	pass

func update_player(players_data_dict: Dictionary):
	# Clear current players
	for child in get_children():
		child.queue_free()
	
	players_data = players_data_dict
	
	# Add player cards for each player
	for player_id in players_data:
		var player_name = players_data[player_id].name
		var character_id = players_data[player_id].character if players_data[player_id].has("character") else "wizgod"
		
		# Convert to lowercase for consistency
		character_id = character_id.to_lower()
		
		# Get the display name from GameData
		var display_character = get_node("/root/GameData").get_character_display_name(character_id)
		
		# Fallback if the character is not found
		if !character_textures.has(character_id):
			character_id = "wizgod" # Default if not found
		
		# Get the appropriate texture for the character
		var texture = character_textures[character_id]
		
		# Create the player card
		var player_card = player_card_scene.instantiate()
		add_child(player_card)
		
		# Setup the card with player info
		player_card.setup(player_name, display_character, texture)

func refreshPlayers(lobbyId: int):
	if !lobbyId:
		return
		
	# Clear all existing cards
	for child in get_children():
		child.queue_free()
	
	var playerCount = Steam.getNumLobbyMembers(lobbyId)
	
	# Create a new card for each player
	for num in playerCount:
		var steamId = Steam.getLobbyMemberByIndex(lobbyId, num)
		var steamName = Steam.getFriendPersonaName(steamId)
		
		# Create the player card (with default character since we don't know it yet)
		var player_card = player_card_scene.instantiate()
		add_child(player_card)
		
		# Setup with default values - note display name can be capitalized
		player_card.setup(steamName, "Wizgod", character_textures["wizgod"])

