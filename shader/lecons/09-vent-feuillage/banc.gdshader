shader_type canvas_item;

uniform vec2 direction_vent = vec2(1.0, 0.3);
uniform float frequence : hint_range(0.0, 4.0) = 1.2;
uniform float longueur_rafale : hint_range(0.5, 20.0) = 4.0;
uniform float force : hint_range(0.0, 0.5) = 0.22;
uniform float melange_rafales : hint_range(0.0, 1.0) = 0.8;
uniform float densite : hint_range(4.0, 30.0) = 14.0;

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

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = UV * vec2(taille.x / taille.y, 1.0) * densite;

    vec2 cellule = floor(p);
    vec2 local = fract(p) - 0.5;

    vec2 direction = normalize(direction_vent);
    float phase = dot(cellule, direction) / longueur_rafale;

    float rafale = smoothstep(0.35, 0.85, bruit_valeur(cellule * 0.11 - direction * TIME * 0.35));
    float amplitude = mix(1.0, rafale, melange_rafales);
    float souplesse = 0.4 + 0.6 * hash21(cellule + 7.0);

    vec2 decalage = direction * sin(TIME * frequence - phase) * force * amplitude * souplesse;

    float disque = smoothstep(0.30, 0.24, length(local - decalage));
    float ombre = smoothstep(0.34, 0.26, length(local + vec2(0.04, 0.05)));

    vec3 couleur = vec3(0.05, 0.06, 0.08);
    couleur = mix(couleur, vec3(0.02, 0.03, 0.04), ombre);
    couleur = mix(couleur, vec3(0.30, 0.70, 0.38) * (0.6 + 0.4 * souplesse), disque);

    COLOR = vec4(couleur, 1.0);
}
