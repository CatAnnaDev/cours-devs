@tool
extends EditorScript

const DOSSIER_MAILLES := "res://vat/mailles"
const SORTIE_POSITIONS := "res://vat/positions.exr"
const SORTIE_NORMALES := "res://vat/normales.exr"

func _run() -> void:
	var chemins := _lister_mailles(DOSSIER_MAILLES)
	if chemins.is_empty():
		push_error("Aucune maille trouvee dans %s" % DOSSIER_MAILLES)
		return

	var toutes_positions: Array[PackedVector3Array] = []
	var toutes_normales: Array[PackedVector3Array] = []
	var minimum := Vector3.ONE * INF
	var maximum := Vector3.ONE * -INF

	for chemin in chemins:
		var ressource := load(chemin)
		var maille: ArrayMesh = ressource if ressource is ArrayMesh else ressource.get_mesh()
		var tableaux := maille.surface_get_arrays(0)

		var positions: PackedVector3Array = tableaux[Mesh.ARRAY_VERTEX]
		var normales: PackedVector3Array = tableaux[Mesh.ARRAY_NORMAL]

		toutes_positions.append(positions)
		toutes_normales.append(normales)

		for sommet in positions:
			minimum = minimum.min(sommet)
			maximum = maximum.max(sommet)

	var nombre_sommets := toutes_positions[0].size()
	var nombre_images := toutes_positions.size()
	var etendue := (maximum - minimum).max(Vector3.ONE * 0.0001)

	var image_positions := Image.create(nombre_sommets, nombre_images, false, Image.FORMAT_RGBF)
	var image_normales := Image.create(nombre_sommets, nombre_images, false, Image.FORMAT_RGBF)

	for image in nombre_images:
		if toutes_positions[image].size() != nombre_sommets:
			push_error("La maille %d n'a pas le meme nombre de sommets" % image)
			return
		for sommet in nombre_sommets:
			var p := (toutes_positions[image][sommet] - minimum) / etendue
			image_positions.set_pixel(sommet, image, Color(p.x, p.y, p.z))
			var n := toutes_normales[image][sommet] * 0.5 + Vector3.ONE * 0.5
			image_normales.set_pixel(sommet, image, Color(n.x, n.y, n.z))

	DirAccess.make_dir_recursive_absolute(SORTIE_POSITIONS.get_base_dir())
	image_positions.save_exr(SORTIE_POSITIONS)
	image_normales.save_exr(SORTIE_NORMALES)

	print("VAT cuite : %d sommets x %d images" % [nombre_sommets, nombre_images])
	print("nombre_images = %d" % nombre_images)
	print("borne_min = Vector3%s" % minimum)
	print("borne_max = Vector3%s" % maximum)

func _lister_mailles(dossier: String) -> PackedStringArray:
	var chemins := PackedStringArray()
	var acces := DirAccess.open(dossier)
	if acces == null:
		return chemins
	for nom in acces.get_files():
		if nom.ends_with(".res") or nom.ends_with(".tres") or nom.ends_with(".obj"):
			chemins.append(dossier.path_join(nom))
	chemins.sort()
	return chemins
