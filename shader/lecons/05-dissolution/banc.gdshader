shader_type canvas_item;

uniform float echelle : hint_range(1.0, 16.0) = 5.0;
uniform float largeur_bord : hint_range(0.001, 0.4) = 0.08;
uniform vec3 couleur_bord : source_color = vec3(1.0, 0.35, 0.05);
uniform vec3 couleur_base : source_color = vec3(0.22, 0.25, 0.30);
uniform float vitesse : hint_range(0.0, 1.0) = 0.15;

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
    float grain = smoothstep(0.25, 0.75, fbm(UV * echelle));

    float progression = fract(TIME * vitesse);
    float seuil = mix(-largeur_bord, 1.0 + largeur_bord, progression);
    float visible = grain - seuil;

    if (visible < 0.0) {
        discard;
    }

    float bord = 1.0 - smoothstep(0.0, largeur_bord, visible);

    COLOR = vec4(couleur_base + couleur_bord * bord * 3.0, 1.0);
}
