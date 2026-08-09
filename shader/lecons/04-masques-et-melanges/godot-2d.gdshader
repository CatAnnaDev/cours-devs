shader_type canvas_item;

uniform sampler2D texture_usure : source_color, filter_linear;
uniform sampler2D masque : filter_linear;
uniform float couverture : hint_range(0.0, 1.0) = 0.5;
uniform float nettete : hint_range(0.002, 0.5) = 0.12;
uniform float irregularite : hint_range(0.0, 1.0) = 0.35;

void fragment() {
    vec4 base = texture(TEXTURE, UV);
    vec4 usure = texture(texture_usure, UV);

    float grain = texture(masque, UV).r;
    float valeur = UV.y - irregularite * (1.0 - grain);

    float seuil = 1.0 - couverture;
    float melange = smoothstep(seuil - nettete, seuil + nettete, valeur);

    COLOR = vec4(mix(base.rgb, usure.rgb, melange), base.a) * COLOR;
}
