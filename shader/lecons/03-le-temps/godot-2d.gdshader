shader_type canvas_item;

uniform vec3 teinte : source_color = vec3(0.35, 0.80, 1.0);
uniform vec2 carrelage = vec2(1.0, 2.0);
uniform vec2 vitesse = vec2(0.0, -0.35);
uniform float pulsation_vitesse : hint_range(0.0, 12.0) = 2.0;
uniform float pulsation_force : hint_range(0.0, 1.0) = 0.35;
uniform float intensite : hint_range(0.0, 10.0) = 2.0;

void fragment() {
    vec2 uv = fract(UV * carrelage + vitesse * TIME);
    vec4 sprite = texture(TEXTURE, uv);

    float pulsation = sin(TIME * pulsation_vitesse) * 0.5 + 0.5;
    float force = mix(1.0 - pulsation_force, 1.0, pulsation);

    COLOR = vec4(sprite.rgb * teinte * intensite * force, sprite.a) * COLOR;
}
