shader_type spatial;
render_mode cull_disabled;

uniform sampler2D motif : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 teinte : source_color = vec3(0.35, 0.80, 1.0);
uniform vec2 carrelage = vec2(1.0, 2.0);
uniform vec2 vitesse = vec2(0.0, -0.35);
uniform float pulsation_vitesse : hint_range(0.0, 12.0) = 2.0;
uniform float pulsation_force : hint_range(0.0, 1.0) = 0.35;
uniform float emission_force : hint_range(0.0, 10.0) = 2.5;

void fragment() {
    vec2 uv = UV * carrelage + vitesse * TIME;
    float bande = texture(motif, uv).r;

    float pulsation = sin(TIME * pulsation_vitesse) * 0.5 + 0.5;
    float intensite = mix(1.0 - pulsation_force, 1.0, pulsation);

    ALBEDO = vec3(0.0);
    SPECULAR = 0.0;
    EMISSION = teinte * bande * emission_force * intensite;
}
