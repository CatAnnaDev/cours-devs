shader_type canvas_item;

uniform int nombre_pas : hint_range(2, 32) = 12;
uniform float profondeur : hint_range(0.05, 1.0) = 0.65;
uniform float amplitude_angle : hint_range(0.0, 1.5) = 0.8;
uniform float vitesse : hint_range(0.0, 1.0) = 0.15;
uniform float depart_x : hint_range(0.05, 0.95) = 0.5;

float hauteur(float x) {
    float joints = smoothstep(0.015, 0.055, abs(fract(x * 4.0) - 0.5));
    float grain = 0.5 + 0.5 * sin(x * 37.0);
    return clamp(joints * (0.72 + 0.18 * grain), 0.0, 1.0);
}

float distance_segment(vec2 p, vec2 a, vec2 b) {
    vec2 ab = b - a;
    float t = clamp(dot(p - a, ab) / max(dot(ab, ab), 0.000001), 0.0, 1.0);
    return length(p - (a + ab * t));
}

void fragment() {
    vec2 uv = UV;
    float surface = 1.0 - hauteur(uv.x);

    float dans_matiere = step(surface, uv.y);
    vec3 couleur = mix(vec3(0.04, 0.05, 0.07), vec3(0.17, 0.15, 0.13), dans_matiere);

    float ligne_surface = smoothstep(0.004, 0.0, abs(uv.y - surface));
    couleur = mix(couleur, vec3(0.55, 0.50, 0.45), ligne_surface);

    float pente = sin(TIME * vitesse * 6.28318530718) * amplitude_angle;
    vec2 depart = vec2(depart_x, 0.0);
    vec2 arrivee = vec2(depart_x + pente * profondeur, profondeur);

    float rayon = smoothstep(0.005, 0.0, distance_segment(uv, depart, arrivee));
    couleur = mix(couleur, vec3(0.30, 0.50, 0.90), rayon);

    float touche = 0.0;
    for (int i = 0; i <= 32; i++) {
        if (i > nombre_pas) {
            break;
        }
        float t = float(i) / float(nombre_pas);
        vec2 p = mix(depart, arrivee, t);
        float dedans = step(1.0 - hauteur(p.x), p.y);
        float premier = dedans * (1.0 - touche);
        touche = max(touche, dedans);

        vec3 teinte = mix(vec3(0.95, 0.50, 0.25), vec3(0.35, 0.35, 0.40), dedans);
        teinte = mix(teinte, vec3(0.30, 0.95, 0.50), premier);

        float point = smoothstep(0.013, 0.008, length(uv - p));
        couleur = mix(couleur, teinte, point);
    }

    COLOR = vec4(couleur, 1.0);
}
