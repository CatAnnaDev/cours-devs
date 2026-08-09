shader_type canvas_item;

uniform float nettete : hint_range(0.0, 0.4) = 0.06;
uniform float vitesse : hint_range(0.0, 0.5) = 0.12;
uniform float echelle : hint_range(1.0, 12.0) = 4.0;

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
    float valeur = fbm(UV * echelle);
    float seuil = sin(TIME * vitesse * 6.28318530718) * 0.35 + 0.5;

    vec2 quadrant = floor(UV * 2.0);
    float resultat = valeur;

    if (quadrant.x > 0.5 && quadrant.y < 0.5) {
        resultat = step(seuil, valeur);
    } else if (quadrant.x < 0.5 && quadrant.y > 0.5) {
        resultat = smoothstep(seuil - nettete, seuil + nettete, valeur);
    } else if (quadrant.x > 0.5 && quadrant.y > 0.5) {
        float largeur = fwidth(valeur);
        resultat = smoothstep(seuil - largeur, seuil + largeur, valeur);
    }

    vec2 dans_quadrant = fract(UV * 2.0);
    float separation = smoothstep(0.006, 0.0, min(dans_quadrant.x, dans_quadrant.y));

    COLOR = vec4(mix(vec3(resultat), vec3(0.95, 0.35, 0.25), separation), 1.0);
}
