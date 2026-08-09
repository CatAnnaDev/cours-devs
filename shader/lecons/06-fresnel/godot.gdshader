shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_contour : source_color = vec3(0.35, 0.75, 1.0);
uniform float puissance : hint_range(0.5, 16.0) = 4.0;
uniform float intensite : hint_range(0.0, 10.0) = 2.5;
uniform float seuil_bas : hint_range(0.0, 1.0) = 0.0;
uniform float rugosite : hint_range(0.0, 1.0) = 0.6;

void fragment() {
    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance);
    fresnel = smoothstep(seuil_bas, 1.0, fresnel);

    ALBEDO = texture(texture_base, UV).rgb;
    ROUGHNESS = rugosite;
    EMISSION = couleur_contour * fresnel * intensite;
}
