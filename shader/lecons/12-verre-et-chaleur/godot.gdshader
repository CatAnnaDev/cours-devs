shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_back, unshaded;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D normales : hint_normal, filter_linear_mipmap, repeat_enable;
uniform vec3 teinte : source_color = vec3(0.86, 0.94, 1.0);
uniform vec3 couleur_bord : source_color = vec3(1.0, 1.0, 1.0);
uniform float force_refraction : hint_range(0.0, 0.2) = 0.03;
uniform float flou : hint_range(0.0, 5.0) = 0.0;
uniform float carrelage : hint_range(0.1, 20.0) = 2.0;
uniform vec2 vitesse = vec2(0.03, 0.05);
uniform float puissance_fresnel : hint_range(0.5, 16.0) = 4.0;
uniform float force_bord : hint_range(0.0, 2.0) = 0.35;

void fragment() {
    vec3 perturbation = texture(normales, UV * carrelage + vitesse * TIME).xyz * 2.0 - 1.0;
    vec2 decalage = perturbation.xy * force_refraction;

    vec2 uv_ecran = clamp(SCREEN_UV + decalage, vec2(0.001), vec2(0.999));
    vec3 derriere = textureLod(screen_texture, uv_ecran, flou).rgb;

    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance_fresnel);

    ALBEDO = derriere * teinte + couleur_bord * fresnel * force_bord;
    ALPHA = 1.0;
}
