shader_type canvas_item;

uniform vec3 couleur : source_color = vec3(0.25, 0.85, 1.0);
uniform float opacite_base : hint_range(0.0, 1.0) = 0.65;
uniform float densite_lignes : hint_range(1.0, 400.0) = 90.0;
uniform float vitesse_lignes : hint_range(-4.0, 4.0) = -0.6;
uniform float force_lignes : hint_range(0.0, 1.0) = 0.55;
uniform float vitesse_balayage : hint_range(0.0, 2.0) = 0.35;
uniform float force_balayage : hint_range(0.0, 3.0) = 1.2;
uniform float force_glitch : hint_range(0.0, 0.2) = 0.02;
uniform float frequence_glitch : hint_range(0.0, 30.0) = 8.0;
uniform float hauteur_bandes : hint_range(1.0, 60.0) = 14.0;

float bruit1(float x) {
    return fract(sin(x * 91.7) * 43758.5453);
}

void fragment() {
    float bande = floor(UV.y * hauteur_bandes);
    float pas = floor(TIME * frequence_glitch);
    float actif = step(0.93, bruit1(bande + pas * 13.37));
    float decalage = actif * force_glitch * (bruit1(bande + pas) * 2.0 - 1.0);

    vec2 uv = vec2(clamp(UV.x + decalage, 0.0, 1.0), UV.y);
    vec4 sprite = texture(TEXTURE, uv);

    float lignes = fract(UV.y * densite_lignes + TIME * vitesse_lignes);
    lignes = mix(1.0, smoothstep(0.0, 0.45, lignes), force_lignes);

    float balayage = pow(fract(UV.y - TIME * vitesse_balayage), 12.0);

    float intensite = (opacite_base + balayage * force_balayage) * lignes;

    COLOR = vec4(couleur * intensite, sprite.a * intensite) * COLOR;
}
