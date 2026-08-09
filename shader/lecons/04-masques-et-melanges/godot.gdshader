shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D bruit : filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_neige : source_color = vec3(0.90, 0.93, 1.0);
uniform vec2 carrelage = vec2(2.0, 2.0);
uniform float couverture : hint_range(0.0, 1.0) = 0.5;
uniform float nettete : hint_range(0.002, 0.5) = 0.12;
uniform float irregularite : hint_range(0.0, 1.0) = 0.35;
uniform float rugosite_base : hint_range(0.0, 1.0) = 0.85;
uniform float rugosite_neige : hint_range(0.0, 1.0) = 0.45;

void fragment() {
    vec2 uv = UV * carrelage;
    vec3 base = texture(texture_base, uv).rgb;

    vec3 normale_monde = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
    float vers_le_haut = normale_monde.y * 0.5 + 0.5;

    float grain = texture(bruit, uv * 1.7).r;
    float valeur = vers_le_haut - irregularite * (1.0 - grain);

    float seuil = 1.0 - couverture;
    float masque = smoothstep(seuil - nettete, seuil + nettete, valeur);

    ALBEDO = mix(base, couleur_neige, masque);
    ROUGHNESS = mix(rugosite_base, rugosite_neige, masque);
}
