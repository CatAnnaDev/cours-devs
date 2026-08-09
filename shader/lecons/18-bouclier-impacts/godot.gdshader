shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;

uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;
uniform vec3 couleur : source_color = vec3(0.30, 0.70, 1.0);
uniform vec3 couleur_intersection : source_color = vec3(0.85, 0.95, 1.0);
uniform float opacite_base : hint_range(0.0, 1.0) = 0.04;
uniform float puissance_fresnel : hint_range(0.5, 16.0) = 3.0;
uniform float force_fresnel : hint_range(0.0, 4.0) = 1.0;
uniform float echelle_hexagones : hint_range(1.0, 60.0) = 14.0;
uniform float epaisseur_hexagones : hint_range(0.001, 0.5) = 0.06;
uniform float force_hexagones : hint_range(0.0, 2.0) = 0.35;
uniform vec4 impacts[8];
uniform int nombre_impacts : hint_range(0, 8) = 0;
uniform float vitesse_onde : hint_range(0.1, 10.0) = 2.5;
uniform float duree_onde : hint_range(0.1, 5.0) = 1.2;
uniform float largeur_onde : hint_range(0.01, 1.0) = 0.22;
uniform float force_onde : hint_range(0.0, 8.0) = 3.0;
uniform float largeur_intersection : hint_range(0.01, 2.0) = 0.35;

varying vec3 position_locale;

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

void vertex() {
    position_locale = VERTEX;
}

void fragment() {
    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance_fresnel) * force_fresnel;

    float distance_bord = grille_hexagonale(UV * echelle_hexagones);
    float hexagones = smoothstep(epaisseur_hexagones, 0.0, distance_bord) * force_hexagones;

    float onde = 0.0;
    for (int i = 0; i < 8; i++) {
        if (i >= nombre_impacts) {
            break;
        }
        float age = TIME - impacts[i].w;
        if (age < 0.0 || age > duree_onde) {
            continue;
        }
        float rayon = age * vitesse_onde;
        float anneau = smoothstep(largeur_onde, 0.0, abs(distance(position_locale, impacts[i].xyz) - rayon));
        onde += anneau * (1.0 - age / duree_onde);
    }
    onde *= force_onde;

    float brut = texture(depth_texture, SCREEN_UV).x;
    vec4 vue = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, brut, 1.0);
    float profondeur_scene = -vue.z / vue.w;
    float epaisseur = max(profondeur_scene + VERTEX.z, 0.0);
    float intersection = 1.0 - clamp(epaisseur / largeur_intersection, 0.0, 1.0);

    float intensite = opacite_base + fresnel + hexagones + onde;

    ALBEDO = couleur * intensite + couleur_intersection * intersection;
    ALPHA = clamp(intensite + intersection, 0.0, 1.0);
}
