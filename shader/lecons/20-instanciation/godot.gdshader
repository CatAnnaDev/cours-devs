shader_type spatial;
render_mode cull_disabled;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_jeune : source_color = vec3(0.35, 0.70, 0.30);
uniform vec3 couleur_seche : source_color = vec3(0.65, 0.58, 0.25);
uniform float variation_echelle : hint_range(0.0, 1.0) = 0.35;
uniform vec2 direction_vent = vec2(1.0, 0.3);
uniform float force_vent : hint_range(0.0, 1.0) = 0.12;
uniform float frequence_vent : hint_range(0.0, 4.0) = 1.4;
uniform float longueur_rafale : hint_range(0.5, 40.0) = 6.0;
uniform float seuil_alpha : hint_range(0.0, 1.0) = 0.5;

varying vec3 teinte_instance;

void vertex() {
    float graine = INSTANCE_CUSTOM.x;
    float maturite = INSTANCE_CUSTOM.y;
    float taille = INSTANCE_CUSTOM.z;

    teinte_instance = mix(couleur_jeune, couleur_seche, maturite);

    VERTEX *= mix(1.0 - variation_echelle, 1.0 + variation_echelle, taille);

    vec3 position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec2 direction = normalize(direction_vent);
    float phase = dot(position_monde.xz, direction) / longueur_rafale + graine * 6.28318530718;

    float souplesse = clamp(VERTEX.y, 0.0, 4.0) * COLOR.r;
    float rafale = sin(TIME * frequence_vent - phase);

    VERTEX.xz += direction * rafale * force_vent * souplesse;
}

void fragment() {
    vec4 echantillon = texture(texture_base, UV);

    ALBEDO = echantillon.rgb * teinte_instance;
    ALPHA = echantillon.a;
    ALPHA_SCISSOR_THRESHOLD = seuil_alpha;
    ROUGHNESS = 0.9;
    SPECULAR = 0.15;
}
