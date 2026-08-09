shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 presseurs[16];
uniform int nombre_presseurs : hint_range(0, 16) = 0;
uniform vec4 zone = vec4(0.0, 0.0, 20.0, 20.0);
uniform float durete : hint_range(0.0, 0.99) = 0.6;

void fragment() {
    vec2 monde = (UV - 0.5) * zone.zw + zone.xy;

    float total = 0.0;
    for (int i = 0; i < 16; i++) {
        if (i >= nombre_presseurs) {
            break;
        }
        float rayon = presseurs[i].z;
        float force = presseurs[i].w;
        float distance_centre = distance(monde, presseurs[i].xy);
        total += force * smoothstep(rayon, rayon * durete, distance_centre);
    }

    COLOR = vec4(vec3(total), 1.0);
}
