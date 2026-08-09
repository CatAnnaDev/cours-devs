shader_type spatial;
render_mode cull_disabled;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D bruit : filter_linear_mipmap, repeat_enable;
uniform float progression : hint_range(0.0, 1.0) = 0.0;
uniform float echelle_bruit : hint_range(0.5, 16.0) = 4.0;
uniform vec3 couleur_bord : source_color = vec3(1.0, 0.35, 0.05);
uniform float largeur_bord : hint_range(0.001, 0.4) = 0.08;
uniform float intensite_bord : hint_range(0.0, 20.0) = 6.0;
uniform float rugosite : hint_range(0.0, 1.0) = 0.8;

void fragment() {
    float grain = texture(bruit, UV * echelle_bruit).r;

    float seuil = mix(-largeur_bord, 1.0 + largeur_bord, progression);
    float visible = grain - seuil;

    if (visible < 0.0) {
        discard;
    }

    float bord = 1.0 - smoothstep(0.0, largeur_bord, visible);

    ALBEDO = texture(texture_base, UV).rgb;
    ROUGHNESS = rugosite;
    EMISSION = couleur_bord * bord * intensite_bord;
}
