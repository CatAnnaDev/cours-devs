shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D normales : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D carte_hauteur : filter_linear_mipmap, repeat_enable;
uniform vec2 carrelage = vec2(1.0, 1.0);
uniform float profondeur : hint_range(0.0, 0.3) = 0.06;
uniform float couches_min : hint_range(4.0, 64.0) = 8.0;
uniform float couches_max : hint_range(4.0, 128.0) = 32.0;
uniform bool couper_les_bords = false;
uniform float rugosite : hint_range(0.0, 1.0) = 0.85;

vec2 parallaxe_occlusion(vec2 uv_depart, vec3 vue) {
    float couches = mix(couches_max, couches_min, clamp(abs(vue.z), 0.0, 1.0));
    float pas_profondeur = 1.0 / couches;
    vec2 pas_uv = vue.xy * profondeur / couches;

    float profondeur_courante = 0.0;
    vec2 uv = uv_depart;
    float hauteur = 1.0 - texture(carte_hauteur, uv).r;

    for (int i = 0; i < 128; i++) {
        if (profondeur_courante >= hauteur || float(i) >= couches) {
            break;
        }
        uv -= pas_uv;
        hauteur = 1.0 - texture(carte_hauteur, uv).r;
        profondeur_courante += pas_profondeur;
    }

    vec2 uv_precedent = uv + pas_uv;
    float ecart_apres = hauteur - profondeur_courante;
    float ecart_avant = (1.0 - texture(carte_hauteur, uv_precedent).r) - profondeur_courante + pas_profondeur;
    float poids = ecart_apres / max(ecart_apres - ecart_avant, 0.0001);

    return mix(uv, uv_precedent, clamp(poids, 0.0, 1.0));
}

void fragment() {
    mat3 tbn = mat3(TANGENT, BINORMAL, NORMAL);
    vec3 vue_tangente = normalize(VIEW * tbn);

    vec2 uv = parallaxe_occlusion(UV * carrelage, vue_tangente);

    if (couper_les_bords && (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)) {
        discard;
    }

    ALBEDO = texture(texture_base, uv).rgb;
    NORMAL_MAP = texture(normales, uv).xyz;
    ROUGHNESS = rugosite;
}
