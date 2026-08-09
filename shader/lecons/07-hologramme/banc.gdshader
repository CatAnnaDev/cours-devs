shader_type canvas_item;

uniform vec3 couleur : source_color = vec3(0.25, 0.85, 1.0);
uniform float puissance_fresnel : hint_range(0.5, 16.0) = 3.0;
uniform float opacite_base : hint_range(0.0, 1.0) = 0.10;
uniform float densite_lignes : hint_range(1.0, 200.0) = 60.0;
uniform float vitesse_lignes : hint_range(-2.0, 2.0) = -0.35;
uniform float force_lignes : hint_range(0.0, 1.0) = 0.55;
uniform float vitesse_balayage : hint_range(0.0, 2.0) = 0.35;
uniform float force_balayage : hint_range(0.0, 3.0) = 1.2;

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = (UV - 0.5) * 2.2;
    p.x *= taille.x / taille.y;
    p.y = -p.y;

    float carre = dot(p, p);
    if (carre > 1.0) {
        COLOR = vec4(vec3(0.02, 0.02, 0.03), 1.0);
    } else {
        vec3 normale = vec3(p, sqrt(1.0 - carre));
        float fresnel = pow(1.0 - clamp(normale.z, 0.0, 1.0), puissance_fresnel);

        float lignes = fract(normale.y * densite_lignes + TIME * vitesse_lignes);
        lignes = mix(1.0, smoothstep(0.0, 0.45, lignes), force_lignes);

        float balayage = pow(fract(normale.y * 0.5 - TIME * vitesse_balayage), 12.0);

        float intensite = (opacite_base + fresnel + balayage * force_balayage) * lignes;
        COLOR = vec4(couleur * intensite, 1.0);
    }
}
