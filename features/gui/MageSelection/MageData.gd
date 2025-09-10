class_name MageData
extends Resource

@export var id: String = ""
@export var name: String = ""
@export var description: String = ""
@export_file("*.png") var image_path: String = ""
@export var skills: Array = []

func _init(p_id: String = "", p_name: String = "", p_description: String = "", p_image_path: String = "", p_skills: Array = []):
	id = p_id
	name = p_name
	description = p_description
	image_path = p_image_path
	skills = p_skills
