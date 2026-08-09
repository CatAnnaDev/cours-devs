shader_type canvas_item;

uniform float durete : hint_range(0.001, 1.0) = 0.15;
uniform float echelle_motif : hint_range(2.0, 40.0) = 14.0;
uniform vec3 couleur_rouge : source_color = vec3(0.62, 0.55, 0.45);
uniform vec3 couleur_verte : source_color = vec3(0.28, 0.55, 0.24);
uniform vec3 couleur_bleue : source_color = vec3(0.70, 0.66, 0.50);

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float bruit_valeur(vec2 p) {
    vec2 cellule = floor(p);
    vec2 local = fract(p);
    vec2 lissage = local * local * (3.0 - 2.0 * local);
    float a = hash21(cellule);
    float b = hash21(cellule + vec2(1.0, 0.0));
    float c = hash21(cellule + vec2(0.0, 1.0));
    float d = hash21(cellule + vec2(1.0, 1.0));
    return mix(mix(a, b, lissage.x), mix(c, d, lissage.x), lissage.y);
}

vec3 poids_lineaires(vec3 peinture) {
    float somme = peinture.r + peinture.g + peinture.b;
    return somme > 0.0001 ? peinture / somme : vec3(1.0, 0.0, 0.0);
}

vec3 poids_par_hauteur(vec3 peinture, vec3 hauteur) {
    vec3 combines = peinture * (hauteur + 0.0001);
    float maximum = max(combines.r, max(combines.g, combines.b));
    vec3 retenus = max(combines - (maximum - durete), 0.0);
    float somme = retenus.r + retenus.g + retenus.b;
    return somme > 0.0001 ? retenus / somme : poids_lineaires(peinture);
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = UV * vec2(taille.x / taille.y, 1.0);

    float t = clamp(UV.x * 2.0, 0.0, 1.0);
    vec3 peinture = vec3(1.0 - t, t, 0.35 * sin(UV.y * 6.28318530718) + 0.35);
    peinture = clamp(peinture, 0.0, 1.0);

    vec3 hauteur = vec3(
        bruit_valeur(p * echelle_motif),
        bruit_valeur(p * echelle_motif * 1.7 + 31.0),
        bruit_valeur(p * echelle_motif * 2.3 + 71.0));

    bool en_bas = UV.y > 0.5;
    vec3 poids = en_bas ? poids_par_hauteur(peinture, hauteur) : poids_lineaires(peinture);

    vec3 grain = vec3(
        0.75 + 0.35 * hauteur.r,
        0.75 + 0.35 * hauteur.g,
        0.75 + 0.35 * hauteur.b);

    vec3 couleur = couleur_rouge * grain.r * poids.r
                 + couleur_verte * grain.g * poids.g
                 + couleur_bleue * grain.b * poids.b;

    float separation = smoothstep(0.004, 0.0, abs(UV.y - 0.5));
    couleur = mix(couleur, vec3(0.95, 0.35, 0.25), separation);

    COLOR = vec4(couleur, 1.0);
}
