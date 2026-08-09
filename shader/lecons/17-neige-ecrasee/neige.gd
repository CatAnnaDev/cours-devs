extends Node3D

@export var terrain: MeshInstance3D
@export var vue_deformation: SubViewport
@export var pinceau: ColorRect
@export var effacement: ColorRect
@export var presseurs: Array[Node3D] = []
@export var centre_zone := Vector2.ZERO
@export var taille_zone := Vector2(20.0, 20.0)
@export var rayon_presseur := 0.35
@export var force_presseur := 1.0
@export var persistance_par_seconde := 0.15

const MAXIMUM_PRESSEURS := 16

var _materiau_terrain: ShaderMaterial
var _materiau_pinceau: ShaderMaterial
var _materiau_effacement: ShaderMaterial
var _tampon := PackedVector4Array()

func _ready() -> void:
	_materiau_terrain = terrain.material_override as ShaderMaterial
	_materiau_pinceau = pinceau.material as ShaderMaterial
	_materiau_effacement = effacement.material as ShaderMaterial

	vue_deformation.render_target_clear_mode = SubViewport.CLEAR_MODE_NEVER
	vue_deformation.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	var zone := Vector4(centre_zone.x, centre_zone.y, taille_zone.x, taille_zone.y)
	_materiau_terrain.set_shader_parameter("zone", zone)
	_materiau_terrain.set_shader_parameter("texture_deformation", vue_deformation.get_texture())
	_materiau_pinceau.set_shader_parameter("zone", zone)

	_tampon.resize(MAXIMUM_PRESSEURS)

func _process(delta: float) -> void:
	var actifs := 0
	for presseur in presseurs:
		if actifs >= MAXIMUM_PRESSEURS or presseur == null:
			continue
		var monde := presseur.global_position
		_tampon[actifs] = Vector4(monde.x, monde.z, rayon_presseur, force_presseur)
		actifs += 1

	_materiau_pinceau.set_shader_parameter("presseurs", _tampon)
	_materiau_pinceau.set_shader_parameter("nombre_presseurs", actifs)

	var persistance := pow(persistance_par_seconde, delta)
	_materiau_effacement.set_shader_parameter("persistance", persistance)
