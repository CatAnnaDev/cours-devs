shader_type canvas_item;

uniform sampler2D bruit : filter_linear_mipmap, repeat_enable;
uniform float progression : hint_range(0.0, 1.0) = 0.0;
uniform float echelle_bruit : hint_range(0.5, 16.0) = 4.0;
uniform vec3 couleur_bord : source_color = vec3(1.0, 0.35, 0.05);
uniform float largeur_bord : hint_range(0.001, 0.4) = 0.08;
uniform float intensite_bord : hint_range(0.0, 20.0) = 6.0;

void fragment() {
    vec4 sprite = texture(TEXTURE, UV);
    float grain = texture(bruit, UV * echelle_bruit).r;

    float seuil = mix(-largeur_bord, 1.0 + largeur_bord, progression);
    float visible = grain - seuil;

    float presence = step(0.0, visible);
    float bord = (1.0 - smoothstep(0.0, largeur_bord, visible)) * presence;

    vec3 couleur = sprite.rgb + couleur_bord * bord * intensite_bord;
    COLOR = vec4(couleur, sprite.a * presence) * COLOR;
}
