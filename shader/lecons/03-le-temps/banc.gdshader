shader_type canvas_item;

uniform float vitesse : hint_range(0.0, 3.0) = 0.4;

const float NOMBRE_BANDES = 5.0;

float onde_triangle(float x) {
    return abs(fract(x) * 2.0 - 1.0);
}

void fragment() {
    float indice = floor(UV.y * NOMBRE_BANDES);
    float phase = UV.x - TIME * vitesse;

    float valeur = fract(phase);
    if (indice > 0.5 && indice < 1.5) {
        valeur = sin(phase * TAU) * 0.5 + 0.5;
    } else if (indice > 1.5 && indice < 2.5) {
        valeur = onde_triangle(phase);
    } else if (indice > 2.5 && indice < 3.5) {
        valeur = smoothstep(0.0, 1.0, onde_triangle(phase));
    } else if (indice > 3.5) {
        valeur = step(0.5, fract(phase));
    }

    float dans_bande = fract(UV.y * NOMBRE_BANDES);
    float separation = smoothstep(0.03, 0.0, min(dans_bande, 1.0 - dans_bande));

    COLOR = vec4(mix(vec3(valeur), vec3(0.95, 0.35, 0.25), separation), 1.0);
}
