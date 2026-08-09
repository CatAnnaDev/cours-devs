shader_type canvas_item;

uniform float images_par_seconde : hint_range(0.5, 60.0) = 12.0;
uniform bool interpoler = true;
uniform float taille_point : hint_range(0.005, 0.08) = 0.022;

const int NOMBRE_SOMMETS = 32;
const int NOMBRE_IMAGES = 16;
const float TAU_LOCAL = 6.28318530718;

vec2 position_sommet(float sommet, float image) {
    float angle = sommet / float(NOMBRE_SOMMETS) * TAU_LOCAL;
    float phase = image / float(NOMBRE_IMAGES) * TAU_LOCAL;
    float rayon = 0.58
        + 0.18 * sin(angle * 3.0 + phase)
        + 0.08 * sin(angle * 7.0 - phase * 2.0);
    return vec2(cos(angle), sin(angle)) * rayon;
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    float curseur = TIME * images_par_seconde;
    float image_a = mod(floor(curseur), float(NOMBRE_IMAGES));
    float image_b = mod(floor(curseur) + 1.0, float(NOMBRE_IMAGES));
    float melange = fract(curseur) * float(interpoler);

    vec3 couleur = vec3(0.03, 0.03, 0.05);

    if (UV.x < 0.5) {
        vec2 uv = vec2(UV.x / 0.5, UV.y);
        vec2 cellule = floor(uv * vec2(float(NOMBRE_SOMMETS), float(NOMBRE_IMAGES)));
        vec2 encode = position_sommet(cellule.x, cellule.y) * 0.5 + 0.5;

        couleur = vec3(encode, 0.25);

        float ligne_active = step(abs(cellule.y - image_a), 0.5);
        vec2 locale = fract(uv * vec2(float(NOMBRE_SOMMETS), float(NOMBRE_IMAGES)));
        float bord = min(min(locale.x, 1.0 - locale.x), min(locale.y, 1.0 - locale.y));
        couleur = mix(couleur, vec3(0.30, 0.95, 0.50), ligne_active * smoothstep(0.12, 0.0, bord));
    } else {
        vec2 p = (vec2((UV.x - 0.5) / 0.5, UV.y) - 0.5) * 2.2;
        p.x *= (taille.x * 0.5) / taille.y;
        p.y = -p.y;

        for (int i = 0; i < NOMBRE_SOMMETS; i++) {
            vec2 a = position_sommet(float(i), image_a);
            vec2 b = position_sommet(float(i), image_b);
            vec2 sommet = mix(a, b, melange);

            float point = smoothstep(taille_point, taille_point * 0.6, distance(p, sommet));
            float teinte = float(i) / float(NOMBRE_SOMMETS);
            couleur = mix(couleur, vec3(0.4 + 0.6 * teinte, 0.85, 1.0 - 0.5 * teinte), point);
        }
    }

    float separation = smoothstep(0.004, 0.0, abs(UV.x - 0.5));
    couleur = mix(couleur, vec3(0.5, 0.5, 0.55), separation);

    COLOR = vec4(couleur, 1.0);
}
