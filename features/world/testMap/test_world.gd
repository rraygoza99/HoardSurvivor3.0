extends Node3D
class_name TestWorld

@export var spawnLocations: Array[Marker3D]

@onready var spawner: MultiplayerSpawner = $MultiplayerSpawner
@onready var enemies: Node = $Enemies



