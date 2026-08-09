shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_disabled, unshaded;

uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;
uniform sampler2D bruit_ecume : filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_peu_profonde : source_color = vec3(0.12, 0.58, 0.62);
uniform vec3 couleur_profonde : source_color = vec3(0.02, 0.10, 0.22);
uniform vec3 couleur_ecume : source_color = vec3(1.0, 1.0, 1.0);
uniform float profondeur_max : hint_range(0.05, 20.0) = 3.0;
uniform float largeur_ecume : hint_range(0.01, 3.0) = 0.35;
uniform float opacite_bord : hint_range(0.0, 1.0) = 0.15;
uniform float opacite_fond : hint_range(0.0, 1.0) = 0.92;
uniform float carrelage_ecume : hint_range(0.1, 30.0) = 6.0;
uniform vec2 vitesse_ecume = vec2(0.02, 0.013);

void fragment() {
    float brut = texture(depth_texture, SCREEN_UV).x;
    vec4 vue = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, brut, 1.0);
    float profondeur_fond = -vue.z / vue.w;
    float profondeur_surface = -VERTEX.z;

    float epaisseur = max(profondeur_fond - profondeur_surface, 0.0);
    float melange = clamp(epaisseur / profondeur_max, 0.0, 1.0);

    float grain = texture(bruit_ecume, UV * carrelage_ecume + vitesse_ecume * TIME).r;
    float bord = 1.0 - clamp(epaisseur / largeur_ecume, 0.0, 1.0);
    float ecume = smoothstep(grain * 0.6, grain * 0.6 + 0.18, bord);

    ALBEDO = mix(mix(couleur_peu_profonde, couleur_profonde, melange), couleur_ecume, ecume);
    ALPHA = mix(mix(opacite_bord, opacite_fond, melange), 1.0, ecume);
}
