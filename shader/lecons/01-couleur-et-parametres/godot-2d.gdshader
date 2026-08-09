shader_type canvas_item;

uniform vec3 couleur_neon : source_color = vec3(0.10, 0.70, 1.0);
uniform float intensite : hint_range(0.0, 20.0, 0.1) = 4.0;

void fragment() {
    vec4 sprite = texture(TEXTURE, UV);
    COLOR = vec4(sprite.rgb * couleur_neon * intensite, sprite.a) * COLOR;
}
