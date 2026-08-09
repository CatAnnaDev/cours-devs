shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;

uniform vec3 couleur : source_color = vec3(0.25, 0.85, 1.0);
uniform float puissance_fresnel : hint_range(0.5, 16.0) = 3.0;
uniform float opacite_base : hint_range(0.0, 1.0) = 0.10;
uniform float densite_lignes : hint_range(1.0, 400.0) = 120.0;
uniform float vitesse_lignes : hint_range(-4.0, 4.0) = -0.6;
uniform float force_lignes : hint_range(0.0, 1.0) = 0.55;
uniform float vitesse_balayage : hint_range(0.0, 2.0) = 0.35;
uniform float force_balayage : hint_range(0.0, 3.0) = 1.2;
uniform float force_glitch : hint_range(0.0, 0.3) = 0.04;
uniform float frequence_glitch : hint_range(0.0, 30.0) = 8.0;
uniform float hauteur_bandes : hint_range(1.0, 60.0) = 14.0;

varying vec3 position_locale;

float bruit1(float x) {
    return fract(sin(x * 91.7) * 43758.5453);
}

void vertex() {
    position_locale = VERTEX;

    float bande = floor(VERTEX.y * hauteur_bandes);
    float pas = floor(TIME * frequence_glitch);
    float actif = step(0.93, bruit1(bande + pas * 13.37));

    VERTEX.x += actif * force_glitch * (bruit1(bande + pas) * 2.0 - 1.0);
}

void fragment() {
    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance_fresnel);

    float lignes = fract(position_locale.y * densite_lignes + TIME * vitesse_lignes);
    lignes = mix(1.0, smoothstep(0.0, 0.45, lignes), force_lignes);

    float balayage = pow(fract(position_locale.y * 0.35 - TIME * vitesse_balayage), 12.0);

    float intensite = (opacite_base + fresnel + balayage * force_balayage) * lignes;

    ALBEDO = couleur * intensite;
    ALPHA = clamp(intensite, 0.0, 1.0);
}
