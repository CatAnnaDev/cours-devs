shader_type canvas_item;

uniform vec2 carrelage = vec2(4.0, 3.0);
uniform float epaisseur_grille : hint_range(0.0, 0.1) = 0.02;

float grille(vec2 local) {
    vec2 distance_bord = min(local, 1.0 - local);
    return smoothstep(epaisseur_grille, 0.0, min(distance_bord.x, distance_bord.y));
}

vec3 couleur_cellule(vec2 cellule) {
    float graine = fract(sin(dot(cellule, vec2(12.9898, 78.233))) * 43758.5453);
    return 0.30 + 0.55 * vec3(graine, fract(graine * 3.7), fract(graine * 7.3));
}

void fragment() {
    vec2 uv = UV * carrelage;
    vec2 cellule = floor(uv);
    vec2 local = fract(uv);

    vec3 couleur = couleur_cellule(cellule) * (0.35 + 0.65 * local.x);
    couleur = mix(couleur, vec3(1.0), grille(local));

    COLOR = vec4(couleur, 1.0);
}
