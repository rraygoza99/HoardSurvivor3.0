extends Node3D
class_name TestWorld

@export var spawnLocations: Array[Marker3D]


@onready var enemies: Node = $Enemies
@onready var fireballs: Node = $Fireballs
@onready var magicWaves: Node = $MagicWaves
@onready var xpOrbs: Node = $XPOrbs


