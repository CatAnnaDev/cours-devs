shader_type spatial;
render_mode cull_disabled;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform float seuil_alpha : hint_range(0.0, 1.0) = 0.5;
uniform vec2 direction_vent = vec2(1.0, 0.3);
uniform float force_vent : hint_range(0.0, 1.0) = 0.15;
uniform float frequence_vent : hint_range(0.0, 4.0) = 1.2;
uniform float longueur_rafale : hint_range(0.5, 40.0) = 8.0;
uniform float force_feuille : hint_range(0.0, 0.2) = 0.02;
uniform float frequence_feuille : hint_range(0.0, 20.0) = 6.0;

void vertex() {
    vec3 position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec2 direction = normalize(direction_vent);

    float souplesse = COLOR.r;
    float phase = dot(position_monde.xz, direction) / longueur_rafale;

    float rafale = sin(TIME * frequence_vent - phase);
    vec3 poussee = vec3(direction.x, 0.0, direction.y) * rafale * force_vent;

    float flottement = sin(TIME * frequence_feuille + position_monde.x * 3.1 + position_monde.z * 2.3);
    vec3 tremblement = vec3(0.0, flottement * force_feuille, 0.0);

    VERTEX += (poussee + tremblement) * souplesse;
}

void fragment() {
    vec4 echantillon = texture(texture_base, UV);

    ALBEDO = echantillon.rgb;
    ALPHA = echantillon.a;
    ALPHA_SCISSOR_THRESHOLD = seuil_alpha;
    ROUGHNESS = 0.9;
    SPECULAR = 0.2;
}
