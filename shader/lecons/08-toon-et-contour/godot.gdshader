shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform vec3 couleur_ombre : source_color = vec3(0.45, 0.42, 0.60);
uniform float niveaux : hint_range(1.0, 8.0, 1.0) = 3.0;
uniform float douceur : hint_range(0.0, 0.5) = 0.03;
uniform float seuil_speculaire : hint_range(0.0, 1.0) = 0.35;
uniform float force_speculaire : hint_range(0.0, 4.0) = 1.0;
uniform vec3 couleur_lisere : source_color = vec3(1.0, 0.97, 0.90);
uniform float puissance_lisere : hint_range(0.5, 16.0) = 6.0;
uniform float force_lisere : hint_range(0.0, 2.0) = 0.4;

float marches(float valeur, float nombre, float largeur) {
    float echelle = valeur * nombre;
    float palier = floor(echelle);
    float reste = fract(echelle);
    return (palier + smoothstep(0.5 - largeur, 0.5 + largeur, reste)) / nombre;
}

void fragment() {
    ALBEDO = texture(texture_base, UV).rgb;
}

void light() {
    vec3 normale = normalize(NORMAL);
    vec3 vers_lumiere = normalize(LIGHT);
    vec3 vers_camera = normalize(VIEW);

    float eclairement = dot(normale, vers_lumiere) * 0.5 + 0.5;
    float marche = marches(eclairement, niveaux, douceur);
    vec3 teinte = mix(couleur_ombre, vec3(1.0), marche);

    vec3 demi = normalize(vers_lumiere + vers_camera);
    float brillance = pow(clamp(dot(normale, demi), 0.0, 1.0), 32.0);
    float tache = step(seuil_speculaire, brillance) * force_speculaire;

    float face = clamp(dot(normale, vers_camera), 0.0, 1.0);
    float lisere = pow(1.0 - face, puissance_lisere) * force_lisere * marche;

    DIFFUSE_LIGHT += ALBEDO * teinte * ATTENUATION * LIGHT_COLOR / PI;
    SPECULAR_LIGHT += (vec3(tache) + couleur_lisere * lisere) * ATTENUATION * LIGHT_COLOR / PI;
}
