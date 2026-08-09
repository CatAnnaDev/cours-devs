shader_type canvas_item;

uniform vec3 couleur_contour : source_color = vec3(1.0, 0.95, 0.35);
uniform float epaisseur : hint_range(0.0, 8.0) = 1.5;
uniform float seuil_alpha : hint_range(0.0, 1.0) = 0.1;
uniform bool contour_seul = false;

void fragment() {
    vec4 sprite = texture(TEXTURE, UV);
    vec2 pas = TEXTURE_PIXEL_SIZE * epaisseur;
    vec2 diagonale = pas * 0.70710678;

    float voisins = 0.0;
    voisins = max(voisins, texture(TEXTURE, UV + vec2(pas.x, 0.0)).a);
    voisins = max(voisins, texture(TEXTURE, UV - vec2(pas.x, 0.0)).a);
    voisins = max(voisins, texture(TEXTURE, UV + vec2(0.0, pas.y)).a);
    voisins = max(voisins, texture(TEXTURE, UV - vec2(0.0, pas.y)).a);
    voisins = max(voisins, texture(TEXTURE, UV + diagonale).a);
    voisins = max(voisins, texture(TEXTURE, UV - diagonale).a);
    voisins = max(voisins, texture(TEXTURE, UV + vec2(diagonale.x, -diagonale.y)).a);
    voisins = max(voisins, texture(TEXTURE, UV + vec2(-diagonale.x, diagonale.y)).a);

    float dedans = step(seuil_alpha, sprite.a);
    float contour = step(seuil_alpha, voisins) * (1.0 - dedans);

    vec3 couleur = mix(sprite.rgb, couleur_contour, contour);
    float alpha = max(sprite.a, contour);

    if (contour_seul) {
        couleur = couleur_contour;
        alpha = contour;
    }

    COLOR = vec4(couleur, alpha) * COLOR;
}
