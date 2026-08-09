shader_type canvas_item;

uniform float nettete : hint_range(1.0, 16.0) = 4.0;
uniform float echelle : hint_range(1.0, 30.0) = 10.0;
uniform float vitesse : hint_range(0.0, 1.0) = 0.2;
uniform bool montrer_poids = true;

float damier(vec2 uv) {
    vec2 cellule = floor(uv);
    return mod(cellule.x + cellule.y, 2.0) * 0.4 + 0.4;
}

void fragment() {
    vec2 taille = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = (UV - 0.5) * 2.4;
    p.x *= taille.x / taille.y;
    p.y = -p.y;

    float carre = dot(p, p);
    if (carre > 1.0) {
        COLOR = vec4(0.03, 0.03, 0.05, 1.0);
    } else {
        vec3 normale = vec3(p, sqrt(1.0 - carre));

        float angle = TIME * vitesse;
        float c = cos(angle);
        float s = sin(angle);
        normale = vec3(normale.x * c + normale.z * s, normale.y, -normale.x * s + normale.z * c);

        vec3 position = normale;

        vec3 poids = pow(abs(normale), vec3(nettete));
        poids /= (poids.x + poids.y + poids.z);

        vec3 couleur;
        if (montrer_poids) {
            couleur = poids;
        } else {
            vec3 q = position * echelle;
            float x = damier(q.zy);
            float y = damier(q.xz);
            float z = damier(q.xy);
            couleur = vec3(x * poids.x + y * poids.y + z * poids.z);
            couleur *= 0.4 + 0.6 * clamp(dot(normale, normalize(vec3(-0.4, 0.6, 0.7))), 0.0, 1.0);
        }

        COLOR = vec4(couleur, 1.0);
    }
}
