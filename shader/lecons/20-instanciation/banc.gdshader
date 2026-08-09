shader_type canvas_item;

uniform vec3 couleur_jeune : source_color = vec3(0.35, 0.70, 0.30);
uniform vec3 couleur_seche : source_color = vec3(0.70, 0.62, 0.28);
uniform float variation_echelle : hint_range(0.0, 1.0) = 0.4;
uniform float densite : hint_range(4.0, 24.0) = 10.0;
uniform float force_vent : hint_range(0.0, 0.4) = 0.12;
uniform float frequence : hint_range(0.0, 3.0) = 1.2;

const float COLONNES = 4.0;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    float niveau = floor(UV.x * COLONNES);

    vec2 locale = vec2(fract(UV.x * COLONNES), UV.y);
    float aspect_colonne = (taille.x / COLONNES) / taille.y;

    vec2 p = locale * vec2(aspect_colonne, 1.0) * densite;
    vec2 cellule = floor(p);
    vec2 dans_cellule = fract(p) - 0.5;

    float graine = hash21(cellule + niveau * 17.0);
    float maturite = hash21(cellule + 91.3);
    float grandeur = hash21(cellule + 53.7);

    vec3 couleur_touffe = couleur_jeune;
    if (niveau > 0.5) {
        couleur_touffe = mix(couleur_jeune, couleur_seche, maturite);
    }

    float rayon = 0.30;
    if (niveau > 1.5) {
        rayon *= mix(1.0 - variation_echelle, 1.0 + variation_echelle, grandeur);
    }

    vec2 decalage = vec2(0.0);
    if (niveau > 2.5) {
        decalage.x = sin(TIME * frequence + graine * 6.28318530718) * force_vent;
    } else {
        decalage.x = sin(TIME * frequence) * force_vent;
    }

    float disque = smoothstep(rayon, rayon * 0.82, length(dans_cellule - decalage));

    vec3 couleur = vec3(0.05, 0.06, 0.08);
    couleur = mix(couleur, couleur_touffe, disque);

    float separation = smoothstep(0.004, 0.0, abs(fract(UV.x * COLONNES) - 0.0));
    couleur = mix(couleur, vec3(0.45, 0.45, 0.5), separation);

    COLOR = vec4(couleur, 1.0);
}
