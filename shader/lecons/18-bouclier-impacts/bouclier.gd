extends MeshInstance3D

const MAXIMUM_IMPACTS := 8

@export var duree_onde := 1.2

var _materiau: ShaderMaterial
var _impacts := PackedVector4Array()
var _prochain := 0

func _ready() -> void:
	_materiau = material_override as ShaderMaterial
	_impacts.resize(MAXIMUM_IMPACTS)
	_materiau.set_shader_parameter("nombre_impacts", MAXIMUM_IMPACTS)
	for i in MAXIMUM_IMPACTS:
		_impacts[i] = Vector4(0.0, 0.0, 0.0, -1000.0)
	_materiau.set_shader_parameter("impacts", _impacts)

func encaisser(point_monde: Vector3) -> void:
	var local := to_local(point_monde)
	_impacts[_prochain] = Vector4(local.x, local.y, local.z, float(Time.get_ticks_msec()) * 0.001)
	_prochain = (_prochain + 1) % MAXIMUM_IMPACTS
	_materiau.set_shader_parameter("impacts", _impacts)
