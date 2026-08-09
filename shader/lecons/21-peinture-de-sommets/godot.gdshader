shader_type spatial;

uniform sampler2D couche_rouge : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D couche_verte : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D couche_bleue : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D hauteurs : filter_linear_mipmap, repeat_enable;
uniform vec3 carrelage = vec3(4.0, 4.0, 4.0);
uniform bool melange_par_hauteur = true;
uniform float durete : hint_range(0.001, 1.0) = 0.15;
uniform float force_occlusion : hint_range(0.0, 1.0) = 1.0;
uniform float rugosite : hint_range(0.0, 1.0) = 0.85;

vec3 poids_lineaires(vec3 peinture) {
    float somme = peinture.r + peinture.g + peinture.b;
    return somme > 0.0001 ? peinture / somme : vec3(1.0, 0.0, 0.0);
}

vec3 poids_par_hauteur(vec3 peinture, vec3 hauteur) {
    vec3 combines = peinture * (hauteur + 0.0001);
    float maximum = max(combines.r, max(combines.g, combines.b));
    vec3 retenus = max(combines - (maximum - durete), 0.0);
    float somme = retenus.r + retenus.g + retenus.b;
    return somme > 0.0001 ? retenus / somme : poids_lineaires(peinture);
}

void fragment() {
    vec3 rouge = texture(couche_rouge, UV * carrelage.r).rgb;
    vec3 verte = texture(couche_verte, UV * carrelage.g).rgb;
    vec3 bleue = texture(couche_bleue, UV * carrelage.b).rgb;

    vec3 hauteur = vec3(
        texture(hauteurs, UV * carrelage.r).r,
        texture(hauteurs, UV * carrelage.g).g,
        texture(hauteurs, UV * carrelage.b).b);

    vec3 poids = melange_par_hauteur
        ? poids_par_hauteur(COLOR.rgb, hauteur)
        : poids_lineaires(COLOR.rgb);

    vec3 melange = rouge * poids.r + verte * poids.g + bleue * poids.b;

    ALBEDO = melange;
    AO = mix(1.0, COLOR.a, force_occlusion);
    AO_LIGHT_AFFECT = 1.0;
    ROUGHNESS = rugosite;
}
