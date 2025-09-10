extends GridContainer
class_name LobbyPlayersGrid

# Dictionary to store player data {player_id: {name: String, character: String}}
var players_data = {}

# Dictionary of character textures for selection
var character_textures = {
	"Wizgod": preload("res://features/gui/MageSelection/Images/Wizgod_portrait.png"),
	"Alice": preload("res://features/gui/MageSelection/Images/Alice_portrait.png"),
	"Sam": preload("res://features/gui/MageSelection/Images/Sam_portrait.png"),
	"Carl": preload("res://features/gui/MageSelection/Images/Carl_portrait.png"),
	"Bern": preload("res://features/gui/MageSelection/Images/Bern_portrait.png"),
	"Ben": preload("res://features/gui/MageSelection/Images/Ben_portrait.png"),
	"DefaultWizard": preload("res://features/gui/MageSelection/Images/Wizgod_portrait.png")
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

# Update the player list with new player data
func update_players(players_data_dict: Dictionary):
	# Clear current players
	for child in get_children():
		child.queue_free()
	
	players_data = players_data_dict
	
	# Add player cards for each player
	for player_id in players_data:
		var player_name = players_data[player_id].name
		var character = players_data[player_id].character if players_data[player_id].has("character") else "DefaultWizard"
		
		# Get the appropriate texture for the character
		var texture = character_textures[character] if character_textures.has(character) else character_textures["DefaultWizard"]
		
		# Create the player card
		var player_card = player_card_scene.instantiate()
		add_child(player_card)
		
		# Setup the card with player info
		player_card.setup(player_name, character, texture)

# Legacy method for compatibility with the Steam refresh
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
		
		# Setup with default values
		player_card.setup(steamName, "DefaultWizard", character_textures["DefaultWizard"])
