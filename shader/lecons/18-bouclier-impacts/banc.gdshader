shader_type canvas_item;

uniform vec3 couleur : source_color = vec3(0.30, 0.70, 1.0);
uniform float echelle_hexagones : hint_range(1.0, 40.0) = 12.0;
uniform float epaisseur_hexagones : hint_range(0.001, 0.5) = 0.06;
uniform float force_hexagones : hint_range(0.0, 2.0) = 0.35;
uniform float vitesse_onde : hint_range(0.1, 4.0) = 1.1;
uniform float duree_onde : hint_range(0.1, 5.0) = 1.6;
uniform float largeur_onde : hint_range(0.01, 0.6) = 0.10;
uniform float force_onde : hint_range(0.0, 8.0) = 3.0;
uniform float intervalle_impacts : hint_range(0.2, 4.0) = 0.7;

const int NOMBRE_IMPACTS = 6;

float bord_hexagone(vec2 p) {
    p = abs(p);
    return max(dot(p, normalize(vec2(1.0, 1.7320508))), p.x);
}

float grille_hexagonale(vec2 p) {
    vec2 maille = vec2(1.0, 1.7320508);
    vec2 a = mod(p, maille) - maille * 0.5;
    vec2 b = mod(p - maille * 0.5, maille) - maille * 0.5;
    vec2 local = mix(a, b, step(dot(b, b), dot(a, a)));
    return 0.5 - bord_hexagone(local);
}

float hash11(float x) {
    return fract(sin(x * 127.1) * 43758.5453);
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = (UV - 0.5) * 2.2;
    p.x *= taille.x / taille.y;
    p.y = -p.y;

    float carre = dot(p, p);
    if (carre > 1.0) {
        COLOR = vec4(0.02, 0.02, 0.03, 1.0);
    } else {
        vec3 normale = vec3(p, sqrt(1.0 - carre));
        float fresnel = pow(1.0 - normale.z, 3.0);

        float distance_bord = grille_hexagonale(normale.xy * echelle_hexagones);
        float hexagones = smoothstep(epaisseur_hexagones, 0.0, distance_bord) * force_hexagones;

        float onde = 0.0;
        for (int i = 0; i < NOMBRE_IMPACTS; i++) {
            float numero = floor(TIME / intervalle_impacts) - float(i);
            float instant = numero * intervalle_impacts;
            float age = TIME - instant;
            if (age < 0.0 || age > duree_onde) {
                continue;
            }
            float angle = hash11(numero) * 6.28318530718;
            float rayon_source = 0.85 * sqrt(hash11(numero + 31.7));
            vec3 source = vec3(cos(angle) * rayon_source, sin(angle) * rayon_source, 0.0);
            source.z = sqrt(max(1.0 - dot(source.xy, source.xy), 0.0));

            float rayon = age * vitesse_onde;
            float anneau = smoothstep(largeur_onde, 0.0, abs(distance(normale, source) - rayon));
            onde += anneau * (1.0 - age / duree_onde);
        }

        float intensite = 0.04 + fresnel + hexagones + onde * force_onde;
        COLOR = vec4(couleur * intensite, 1.0);
    }
}
