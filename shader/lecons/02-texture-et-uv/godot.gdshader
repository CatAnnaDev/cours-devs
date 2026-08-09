shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec3 teinte : source_color = vec3(1.0);
uniform vec2 carrelage = vec2(4.0, 4.0);
uniform vec2 decalage = vec2(0.0);
uniform float rugosite : hint_range(0.0, 1.0) = 0.85;

void fragment() {
    vec2 uv = UV * carrelage + decalage;
    vec4 echantillon = texture(texture_base, uv);

    ALBEDO = echantillon.rgb * teinte;
    ROUGHNESS = rugosite;
    SPECULAR = 0.3;
}
