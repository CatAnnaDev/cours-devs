shader_type canvas_item;

uniform sampler2D palette : source_color, filter_nearest, repeat_disable;
uniform float niveaux : hint_range(2.0, 32.0, 1.0) = 5.0;
uniform bool utiliser_palette = false;
uniform float saturation : hint_range(0.0, 2.0) = 1.15;

float luminance(vec3 couleur) {
    return dot(couleur, vec3(0.2126, 0.7152, 0.0722));
}

void fragment() {
    vec4 sprite = texture(TEXTURE, UV);

    vec3 quantifiee = floor(sprite.rgb * niveaux + 0.5) / niveaux;

    float gris = luminance(quantifiee);
    quantifiee = mix(vec3(gris), quantifiee, saturation);

    vec3 finale = quantifiee;
    if (utiliser_palette) {
        finale = texture(palette, vec2(clamp(luminance(sprite.rgb), 0.01, 0.99), 0.5)).rgb;
    }

    COLOR = vec4(clamp(finale, 0.0, 1.0), sprite.a) * COLOR;
}
