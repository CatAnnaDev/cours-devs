shader_type canvas_item;
render_mode blend_mul, unshaded;

uniform float persistance : hint_range(0.9, 1.0) = 0.999;

void fragment() {
    COLOR = vec4(vec3(persistance), 1.0);
}
