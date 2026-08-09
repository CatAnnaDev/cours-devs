shader_type canvas_item;

uniform int nombre_traces : hint_range(1, 64) = 40;
uniform float duree_trace : hint_range(0.5, 20.0) = 6.0;
uniform float rayon : hint_range(0.005, 0.15) = 0.035;
uniform float durete : hint_range(0.0, 0.99) = 0.55;
uniform float largeur_bourrelet : hint_range(0.0, 2.0) = 0.6;
uniform vec3 couleur_surface : source_color = vec3(0.90, 0.93, 1.0);
uniform vec3 couleur_creux : source_color = vec3(0.45, 0.52, 0.70);

vec2 chemin(float t) {
    return vec2(0.5 + 0.32 * sin(t * 0.7), 0.5 + 0.30 * sin(t * 1.1 + 1.3));
}

float enfoncement(vec2 p) {
    float total = 0.0;
    for (int i = 0; i < 64; i++) {
        if (i >= nombre_traces) {
            break;
        }
        float age = float(i) / float(nombre_traces);
        float instant = TIME - age * duree_trace;
        float force = 1.0 - age;
        total += force * smoothstep(rayon, rayon * durete, distance(p, chemin(instant)));
    }
    return clamp(total, 0.0, 1.0);
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = UV * vec2(taille.x / taille.y, 1.0);

    float pas = 1.5 / taille.y;
    float centre = enfoncement(p);
    float dx = enfoncement(p + vec2(pas, 0.0)) - enfoncement(p - vec2(pas, 0.0));
    float dy = enfoncement(p + vec2(0.0, pas)) - enfoncement(p - vec2(0.0, pas));

    vec3 normale = normalize(vec3(dx / (2.0 * pas) * 0.05, 1.0, dy / (2.0 * pas) * 0.05));
    float eclairement = clamp(dot(normale, normalize(vec3(-0.4, 1.0, -0.5))), 0.0, 1.0);

    float bourrelet = smoothstep(0.0, largeur_bourrelet * pas * 40.0, length(vec2(dx, dy))) * (1.0 - centre);

    vec3 couleur = mix(couleur_surface, couleur_creux, centre);
    couleur = mix(couleur, vec3(1.0), bourrelet * 0.6);
    couleur *= 0.55 + 0.45 * eclairement;

    COLOR = vec4(couleur, 1.0);
}
