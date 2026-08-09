shader_type canvas_item;

uniform float force : hint_range(0.0, 0.2) = 0.05;
uniform float echelle : hint_range(1.0, 20.0) = 5.0;
uniform float vitesse : hint_range(0.0, 2.0) = 0.6;
uniform float rayon : hint_range(0.05, 1.2) = 0.5;
uniform float courbure : hint_range(0.0, 4.0) = 1.6;

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
    for (int octave = 0; octave < 4; octave++) {
        somme += amplitude * bruit_valeur(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return somme;
}

vec3 decor(vec2 uv) {
    float damier = mod(floor(uv.x * 16.0) + floor(uv.y * 16.0), 2.0);
    vec3 fond = mix(vec3(0.10, 0.12, 0.16), vec3(0.16, 0.19, 0.24), damier);

    float rayures = smoothstep(0.45, 0.5, fract((uv.x + uv.y) * 6.0));
    fond = mix(fond, vec3(0.90, 0.55, 0.25), rayures * 0.7);

    float ligne = smoothstep(0.03, 0.0, abs(uv.y - 0.5));
    return mix(fond, vec3(0.35, 0.85, 0.95), ligne);
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = (UV - 0.5) * 2.0;
    p.x *= taille.x / taille.y;

    float distance_centre = length(p);
    float masque = smoothstep(rayon, rayon * 0.9, distance_centre);

    vec2 lentille = p / max(rayon, 0.001);
    lentille *= pow(clamp(distance_centre / rayon, 0.0, 1.0), courbure);

    vec2 turbulence = vec2(
        fbm(p * echelle + vec2(0.0, -TIME * vitesse)),
        fbm(p * echelle + vec2(5.7, -TIME * vitesse * 1.3))) - 0.5;

    vec2 decalage = (lentille * 0.6 + turbulence) * force * masque;

    vec3 couleur = decor(UV + decalage);
    couleur += vec3(0.6, 0.7, 0.9) * pow(clamp(distance_centre / rayon, 0.0, 1.0), 6.0) * masque * 0.35;

    COLOR = vec4(couleur, 1.0);
}
