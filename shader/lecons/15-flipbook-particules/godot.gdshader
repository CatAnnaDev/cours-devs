shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;

uniform sampler2D feuille : source_color, filter_linear_mipmap;
uniform vec2 grille = vec2(8.0, 8.0);
uniform float images_par_seconde : hint_range(0.0, 120.0) = 30.0;
uniform bool melanger_images = true;
uniform vec3 teinte : source_color = vec3(1.0, 1.0, 1.0);
uniform float intensite : hint_range(0.0, 10.0) = 2.0;
uniform float decalage_aleatoire : hint_range(0.0, 1.0) = 0.0;

vec2 uv_image(vec2 uv, float indice) {
    float total = grille.x * grille.y;
    float i = mod(floor(indice), total);
    vec2 cellule = vec2(mod(i, grille.x), floor(i / grille.x));
    return (clamp(uv, 0.001, 0.999) + cellule) / grille;
}

void fragment() {
    float depart = decalage_aleatoire * grille.x * grille.y * COLOR.r;
    float curseur = TIME * images_par_seconde + depart;

    vec4 image = texture(feuille, uv_image(UV, curseur));

    if (melanger_images) {
        vec4 suivante = texture(feuille, uv_image(UV, curseur + 1.0));
        image = mix(image, suivante, fract(curseur));
    }

    ALBEDO = image.rgb * teinte * intensite;
    ALPHA = image.a;
}
