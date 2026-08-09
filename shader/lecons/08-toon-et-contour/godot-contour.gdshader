shader_type spatial;
render_mode cull_front, unshaded, shadows_disabled;

uniform vec3 couleur_contour : source_color = vec3(0.05, 0.03, 0.08);
uniform float epaisseur : hint_range(0.0, 0.1) = 0.01;
uniform bool epaisseur_constante = true;

void vertex() {
    float echelle = 1.0;

    if (epaisseur_constante) {
        echelle = length((MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz);
    }

    VERTEX += normalize(NORMAL) * epaisseur * echelle;
}

void fragment() {
    ALBEDO = couleur_contour;
}
