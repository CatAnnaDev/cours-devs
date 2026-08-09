shader_type canvas_item;

uniform vec3 couleur_base : source_color = vec3(0.95, 0.60, 0.45);
uniform vec3 couleur_ombre : source_color = vec3(0.45, 0.42, 0.60);
uniform vec3 couleur_fond : source_color = vec3(0.03, 0.03, 0.05);
uniform float douceur : hint_range(0.0, 0.5) = 0.02;
uniform vec2 direction_lumiere = vec2(-0.5, 0.6);

const float COLONNES = 5.0;

float marches(float valeur, float nombre, float largeur) {
    float echelle = valeur * nombre;
    float palier = floor(echelle);
    float reste = fract(echelle);
    return (palier + smoothstep(0.5 - largeur, 0.5 + largeur, reste)) / nombre;
}

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
        vec3 lumiere = normalize(vec3(direction_lumiere, 0.75));

        float eclairement = dot(normale, lumiere) * 0.5 + 0.5;
        float niveaux = indice + 1.0;
        float marche = marches(eclairement, niveaux, douceur);

        couleur = couleur_base * mix(couleur_ombre, vec3(1.0), marche);
    }

    COLOR = vec4(couleur, 1.0);
}
