shader_type spatial;

uniform sampler2D normales : hint_normal, filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_eau : source_color = vec3(0.05, 0.28, 0.38);
uniform vec2 carrelage_a = vec2(3.0, 3.0);
uniform vec2 carrelage_b = vec2(7.0, 7.0);
uniform vec2 vitesse_a = vec2(0.030, 0.018);
uniform vec2 vitesse_b = vec2(-0.021, 0.035);
uniform float force_normales : hint_range(0.0, 3.0) = 0.7;
uniform float rugosite : hint_range(0.0, 1.0) = 0.05;
uniform float reflexion : hint_range(0.0, 1.0) = 0.6;

void fragment() {
    vec3 a = texture(normales, UV * carrelage_a + vitesse_a * TIME).xyz * 2.0 - 1.0;
    vec3 b = texture(normales, UV * carrelage_b + vitesse_b * TIME).xyz * 2.0 - 1.0;

    vec3 melange = normalize(vec3(a.xy + b.xy, a.z * b.z));

    NORMAL_MAP = melange * 0.5 + 0.5;
    NORMAL_MAP_DEPTH = force_normales;

    ALBEDO = couleur_eau;
    ROUGHNESS = rugosite;
    METALLIC = 0.0;
    SPECULAR = reflexion;
}
