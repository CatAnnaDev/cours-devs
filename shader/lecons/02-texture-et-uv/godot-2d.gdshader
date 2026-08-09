shader_type canvas_item;

uniform sampler2D texture_carrelee : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 teinte : source_color = vec3(1.0);
uniform vec2 carrelage = vec2(4.0, 4.0);
uniform vec2 decalage = vec2(0.0);

void fragment() {
    vec2 uv = UV * carrelage + decalage;
    vec4 echantillon = texture(texture_carrelee, uv);
    COLOR = vec4(echantillon.rgb * teinte, echantillon.a) * COLOR;
}
