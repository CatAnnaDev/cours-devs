extends MultiMeshInstance3D

@export var nombre := 2000
@export var rayon_zone := 25.0
@export var graine_aleatoire := 12345

func _ready() -> void:
	var hasard := RandomNumberGenerator.new()
	hasard.seed = graine_aleatoire

	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_custom_data = true
	multimesh.instance_count = nombre

	for i in nombre:
		var angle := hasard.randf() * TAU
		var distance := sqrt(hasard.randf()) * rayon_zone
		var position := Vector3(cos(angle) * distance, 0.0, sin(angle) * distance)

		var repere := Transform3D.IDENTITY
		repere = repere.rotated(Vector3.UP, hasard.randf() * TAU)
		repere.origin = position

		multimesh.set_instance_transform(i, repere)
		multimesh.set_instance_custom_data(i, Color(
			hasard.randf(),
			hasard.randf(),
			hasard.randf(),
			1.0))
