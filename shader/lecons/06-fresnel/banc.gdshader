shader_type canvas_item;

uniform vec3 couleur_contour : source_color = vec3(0.35, 0.75, 1.0);
uniform vec3 couleur_base : source_color = vec3(0.10, 0.11, 0.14);
uniform vec3 couleur_fond : source_color = vec3(0.03, 0.03, 0.04);
uniform float intensite : hint_range(0.0, 4.0) = 1.0;

const float COLONNES = 5.0;

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    float aspect_cellule = (taille.x / COLONNES) / taille.y;

    vec2 grille = UV * vec2(COLONNES, 1.0);
    float indice = floor(grille.x);
    vec2 local = vec2(fract(grille.x), UV.y);

    vec2 p = (local - 0.5) * 2.4;
    p.x *= aspect_cellule;
    p.y = -p.y;

    float carre = dot(p, p);
    vec3 couleur = couleur_fond;

    if (carre < 1.0) {
        vec3 normale = vec3(p, sqrt(1.0 - carre));
        vec3 vue = vec3(0.0, 0.0, 1.0);
        float puissance = pow(2.0, indice);
        float fresnel = pow(1.0 - clamp(dot(normale, vue), 0.0, 1.0), puissance);
        couleur = couleur_base + couleur_contour * fresnel * intensite;
    }

    COLOR = vec4(couleur, 1.0);
}
