shader_type spatial;
render_mode blend_mix, cull_front, depth_draw_never, depth_test_disabled, unshaded;

uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;
uniform sampler2D texture_decal : source_color, filter_linear_mipmap;
uniform vec3 teinte : source_color = vec3(1.0, 1.0, 1.0);
uniform float opacite : hint_range(0.0, 1.0) = 1.0;
uniform float angle_max_degres : hint_range(0.0, 89.0) = 60.0;
uniform float adoucissement_bord : hint_range(0.0, 0.49) = 0.08;

void fragment() {
    float brut = texture(depth_texture, SCREEN_UV).x;
    vec4 vue = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, brut, 1.0);
    vue.xyz /= vue.w;

    vec3 position_monde = (INV_VIEW_MATRIX * vec4(vue.xyz, 1.0)).xyz;
    vec3 normale_scene = normalize(cross(dFdx(position_monde), dFdy(position_monde)));

    vec3 position_locale = (inverse(MODEL_MATRIX) * vec4(position_monde, 1.0)).xyz;
    vec3 distance_bord = abs(position_locale);

    vec3 hors_boite = step(vec3(0.5), distance_bord);
    if (hors_boite.x + hors_boite.y + hors_boite.z > 0.0) {
        discard;
    }

    vec3 axe = normalize((MODEL_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
    if (abs(dot(normale_scene, axe)) < cos(radians(angle_max_degres))) {
        discard;
    }

    vec4 echantillon = texture(texture_decal, position_locale.xz + 0.5);

    vec3 marge = smoothstep(vec3(0.5), vec3(0.5 - adoucissement_bord - 0.001), distance_bord);
    float bord = marge.x * marge.y * marge.z;

    ALBEDO = echantillon.rgb * teinte;
    ALPHA = echantillon.a * opacite * bord;
}
