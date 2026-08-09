shader_type canvas_item;

const vec3 couleur_neon = vec3(0.10, 0.70, 1.0);
const float intensite_max = 20.0;

vec3 reinhard(vec3 c) {
    return c / (1.0 + c);
}

void fragment() {
    vec3 hdr = couleur_neon * (UV.x * intensite_max);

    vec3 ecrete = clamp(hdr, 0.0, 1.0);
    vec3 tonemappe = reinhard(hdr);
    vec3 sortie = mix(ecrete, tonemappe, step(0.5, UV.y));

    float separation = smoothstep(0.004, 0.0, abs(UV.y - 0.5));
    COLOR = vec4(mix(sortie, vec3(1.0), separation), 1.0);
}
