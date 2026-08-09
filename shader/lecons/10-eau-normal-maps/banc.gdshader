shader_type canvas_item;

uniform vec3 couleur_creux : source_color = vec3(0.01, 0.07, 0.16);
uniform vec3 couleur_crete : source_color = vec3(0.10, 0.45, 0.55);
uniform vec3 couleur_ciel : source_color = vec3(0.55, 0.72, 0.95);
uniform float force_normales : hint_range(0.0, 4.0) = 1.0;
uniform float echelle : hint_range(1.0, 20.0) = 6.0;
uniform float brillance : hint_range(0.0, 6.0) = 2.5;

vec3 vague(vec2 p, vec2 direction, float longueur, float amplitude, float vitesse) {
    float k = 6.28318530718 / longueur;
    float phase = dot(direction, p) * k + TIME * vitesse * k;
    return vec3(amplitude * sin(phase), direction * k * amplitude * cos(phase));
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = UV * vec2(taille.x / taille.y, 1.0) * echelle;

    vec3 total = vec3(0.0);
    total += vague(p, normalize(vec2( 1.00,  0.35)), 3.10, 0.100, 1.0);
    total += vague(p, normalize(vec2(-0.70,  1.00)), 1.70, 0.060, 1.3);
    total += vague(p, normalize(vec2( 0.40, -1.00)), 0.90, 0.030, 1.9);
    total += vague(p, normalize(vec2(-1.00, -0.20)), 0.50, 0.015, 2.4);

    vec3 normale = normalize(vec3(-total.y * force_normales, 1.0, -total.z * force_normales));
    vec3 lumiere = normalize(vec3(-0.4, 0.7, -0.5));
    vec3 vue = vec3(0.0, 1.0, 0.0);

    float diffus = clamp(dot(normale, lumiere), 0.0, 1.0);
    float speculaire = pow(clamp(dot(normale, normalize(lumiere + vue)), 0.0, 1.0), 128.0);
    float fresnel = pow(1.0 - clamp(dot(normale, vue), 0.0, 1.0), 5.0);

    vec3 couleur = mix(couleur_creux, couleur_crete, clamp(total.x * 3.0 + 0.5, 0.0, 1.0));
    couleur = mix(couleur, couleur_ciel, clamp(fresnel * 3.0, 0.0, 1.0) * 0.7);
    couleur *= 0.35 + 0.65 * diffus;
    couleur += vec3(1.0) * speculaire * brillance;

    COLOR = vec4(couleur, 1.0);
}
