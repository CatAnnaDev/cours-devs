shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 couleur_neon : source_color = vec3(0.10, 0.70, 1.0);
uniform float intensite : hint_range(0.0, 20.0, 0.1) = 4.0;

void fragment() {
    ALBEDO = vec3(0.0);
    SPECULAR = 0.0;
    ROUGHNESS = 1.0;
    EMISSION = couleur_neon * intensite;
}
