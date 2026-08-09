shader_type canvas_item;

uniform vec3 couleur_peu_profonde : source_color = vec3(0.12, 0.58, 0.62);
uniform vec3 couleur_profonde : source_color = vec3(0.02, 0.10, 0.22);
uniform vec3 couleur_sable : source_color = vec3(0.72, 0.66, 0.50);
uniform float profondeur_max : hint_range(0.02, 1.5) = 0.45;
uniform float largeur_ecume : hint_range(0.005, 0.4) = 0.07;
uniform float echelle : hint_range(0.5, 8.0) = 2.5;
uniform float niveau_mer : hint_range(-0.5, 0.5) = 0.0;

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

float fbm(vec2 p) {
    float somme = 0.0;
    float amplitude = 0.5;
    for (int octave = 0; octave < 5; octave++) {
        somme += amplitude * bruit_valeur(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return somme;
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = UV * vec2(taille.x / taille.y, 1.0) * echelle;

    float terrain = fbm(p) - 0.45 + niveau_mer;
    float epaisseur = -terrain;

    if (epaisseur <= 0.0) {
        COLOR = vec4(couleur_sable * (0.75 + 0.5 * terrain), 1.0);
    } else {
        float melange = clamp(epaisseur / profondeur_max, 0.0, 1.0);
        vec3 couleur = mix(couleur_peu_profonde, couleur_profonde, melange);

        float grain = fbm(p * 7.0 + vec2(TIME * 0.06, TIME * 0.03));
        float bord = 1.0 - clamp(epaisseur / largeur_ecume, 0.0, 1.0);
        float ecume = smoothstep(grain * 0.6, grain * 0.6 + 0.18, bord);

        COLOR = vec4(mix(couleur, vec3(1.0), ecume), 1.0);
    }
}
