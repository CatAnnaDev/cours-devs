shader_type canvas_item;

uniform vec2 grille = vec2(6.0, 4.0);
uniform float images_par_seconde : hint_range(0.0, 60.0) = 8.0;
uniform bool melanger_images = true;

vec4 contenu_image(vec2 uv, float indice) {
    float total = grille.x * grille.y;
    float avancement = clamp(indice / max(total - 1.0, 1.0), 0.0, 1.0);

    vec2 p = uv * 2.0 - 1.0;
    float rayon = 0.12 + avancement * 0.85;
    float epaisseur = 0.30 * (1.0 - avancement) + 0.04;
    float anneau = smoothstep(epaisseur, 0.0, abs(length(p) - rayon));

    vec3 couleur = mix(vec3(1.0, 0.92, 0.55), vec3(0.85, 0.15, 0.05), avancement);
    return vec4(couleur, anneau * (1.0 - avancement * 0.85));
}

void fragment() {
    float total = grille.x * grille.y;
    float curseur = TIME * images_par_seconde;
    float active = mod(floor(curseur), total);

    vec3 couleur = vec3(0.03, 0.03, 0.05);

    if (UV.x < 0.6) {
        vec2 uv = vec2(UV.x / 0.6, UV.y);
        vec2 grille_uv = uv * grille;
        vec2 cellule = floor(grille_uv);
        vec2 locale = fract(grille_uv);

        float indice = cellule.y * grille.x + cellule.x;
        vec4 contenu = contenu_image(locale, indice);

        couleur = mix(vec3(0.07, 0.07, 0.09), contenu.rgb, contenu.a);

        float bord = min(min(locale.x, 1.0 - locale.x), min(locale.y, 1.0 - locale.y));
        float cadre = smoothstep(0.03, 0.0, bord);
        float est_active = step(abs(indice - active), 0.5);
        couleur = mix(couleur, mix(vec3(0.25, 0.25, 0.30), vec3(0.30, 0.95, 0.50), est_active), cadre);
    } else {
        vec2 uv = vec2((UV.x - 0.6) / 0.4, UV.y);
        vec4 image = contenu_image(uv, active);

        if (melanger_images) {
            vec4 suivante = contenu_image(uv, mod(active + 1.0, total));
            image = mix(image, suivante, fract(curseur));
        }

        couleur = mix(vec3(0.05, 0.05, 0.07), image.rgb, image.a);

        float barre = step(UV.y, 0.04) * step(uv.x, fract(curseur));
        couleur = mix(couleur, vec3(0.30, 0.60, 0.95), barre);
    }

    float separation = smoothstep(0.004, 0.0, abs(UV.x - 0.6));
    couleur = mix(couleur, vec3(0.5, 0.5, 0.55), separation);

    COLOR = vec4(couleur, 1.0);
}
