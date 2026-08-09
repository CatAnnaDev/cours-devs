shader_type canvas_item;

uniform sampler2D couche_secondaire : source_color, filter_linear_mipmap, repeat_enable;
uniform vec2 carrelage = vec2(1.0, 1.0);
uniform float nettete : hint_range(0.001, 0.5) = 0.15;
uniform float decalage_seuil : hint_range(-0.5, 0.5) = 0.0;

void fragment() {
    vec4 base = texture(TEXTURE, UV);
    vec4 secondaire = texture(couche_secondaire, UV * carrelage);

    float peinture = COLOR.r;
    float seuil = 0.5 + decalage_seuil;
    float melange = smoothstep(seuil - nettete, seuil + nettete, peinture);

    vec3 couleur = mix(base.rgb, secondaire.rgb, melange);
    float alpha = base.a * COLOR.a;

    COLOR = vec4(couleur, alpha);
}
