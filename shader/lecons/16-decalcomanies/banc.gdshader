shader_type canvas_item;

uniform float angle_max_degres : hint_range(0.0, 89.0) = 55.0;
uniform float largeur_boite : hint_range(0.05, 0.5) = 0.22;
uniform float hauteur_boite : hint_range(0.05, 0.5) = 0.22;
uniform float vitesse : hint_range(0.0, 1.0) = 0.12;
uniform float centre_y : hint_range(0.0, 1.0) = 0.45;

float hauteur(float x) {
    float collines = 0.5 + 0.28 * sin(x * 7.0) + 0.12 * sin(x * 17.0 + 1.3);
    return clamp(collines, 0.05, 0.95);
}

float surface(float x) {
    return 1.0 - hauteur(x);
}

void fragment() {
    vec2 uv = UV;

    float y_surface = surface(uv.x);
    float dans_matiere = step(y_surface, uv.y);
    vec3 couleur = mix(vec3(0.04, 0.05, 0.07), vec3(0.15, 0.14, 0.13), dans_matiere);

    float centre_x = 0.5 + 0.35 * sin(TIME * vitesse * 6.28318530718);

    vec2 depuis_centre = abs(vec2(uv.x - centre_x, uv.y - centre_y));
    vec2 demi = vec2(largeur_boite, hauteur_boite);
    float dedans = step(depuis_centre.x, demi.x) * step(depuis_centre.y, demi.y);
    float cadre = dedans * step(min(demi.x - depuis_centre.x, demi.y - depuis_centre.y), 0.004);

    float pas = 0.002;
    float pente = (surface(uv.x + pas) - surface(uv.x - pas)) / (2.0 * pas);
    float cosinus = 1.0 / sqrt(1.0 + pente * pente);
    float accepte = step(cos(radians(angle_max_degres)), cosinus);

    float surface_dans_boite =
        step(abs(uv.x - centre_x), demi.x) * step(abs(y_surface - centre_y), demi.y);

    vec3 teinte_surface = mix(vec3(0.55, 0.50, 0.45), vec3(0.95, 0.30, 0.25), surface_dans_boite);
    teinte_surface = mix(teinte_surface, vec3(0.30, 0.95, 0.50), surface_dans_boite * accepte);

    float trait_surface = smoothstep(0.007, 0.002, abs(uv.y - y_surface));
    couleur = mix(couleur, vec3(0.30, 0.35, 0.45), dedans * 0.12);
    couleur = mix(couleur, teinte_surface, trait_surface);
    couleur = mix(couleur, vec3(0.60, 0.65, 0.80), cadre);

    COLOR = vec4(couleur, 1.0);
}
