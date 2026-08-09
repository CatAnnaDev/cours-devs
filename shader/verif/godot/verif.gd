extends Node

func _ready() -> void:
	var dossier := DirAccess.open("res://shaders")
	if dossier == null:
		printerr("dossier res://shaders introuvable")
		get_tree().quit(1)
		return

	var noms := dossier.get_files()
	noms.sort()

	var total := 0
	for nom in noms:
		if not nom.ends_with(".gdshader"):
			continue
		total += 1
		var shader := load("res://shaders/" + nom)
		if shader == null:
			printerr("VERIF | chargement impossible | ", nom)
			continue
		var materiau := ShaderMaterial.new()
		materiau.shader = shader
		var maillage := MeshInstance3D.new()
		maillage.mesh = SphereMesh.new()
		maillage.material_override = materiau
		add_child(maillage)
		print("VERIF | teste | ", nom)

	print("VERIF | BILAN | ", total, " shaders parcourus")
	await get_tree().process_frame
	await get_tree().process_frame
	get_tree().quit()
