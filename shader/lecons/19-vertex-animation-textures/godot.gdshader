shader_type spatial;

uniform sampler2D positions_cuites : filter_nearest, repeat_disable;
uniform sampler2D normales_cuites : filter_nearest, repeat_disable;
uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform int nombre_images = 60;
uniform float images_par_seconde : hint_range(0.0, 120.0) = 30.0;
uniform vec3 borne_min = vec3(-1.0, -1.0, -1.0);
uniform vec3 borne_max = vec3(1.0, 1.0, 1.0);
uniform bool interpoler = true;
uniform float decalage_instance : hint_range(0.0, 1.0) = 0.0;
uniform float rugosite : hint_range(0.0, 1.0) = 0.7;

vec3 lire_position(int sommet, int image) {
    vec3 brut = texelFetch(positions_cuites, ivec2(sommet, image), 0).xyz;
    return mix(borne_min, borne_max, brut);
}

vec3 lire_normale(int sommet, int image) {
    return texelFetch(normales_cuites, ivec2(sommet, image), 0).xyz * 2.0 - 1.0;
}

void vertex() {
    float total = float(nombre_images);
    float curseur = TIME * images_par_seconde + decalage_instance * total;

    int image_a = int(mod(floor(curseur), total));
    int image_b = int(mod(floor(curseur) + 1.0, total));
    float melange = fract(curseur) * float(interpoler);

    VERTEX = mix(lire_position(VERTEX_ID, image_a), lire_position(VERTEX_ID, image_b), melange);
    NORMAL = normalize(mix(lire_normale(VERTEX_ID, image_a), lire_normale(VERTEX_ID, image_b), melange));
}

void fragment() {
    ALBEDO = texture(texture_base, UV).rgb;
    ROUGHNESS = rugosite;
}
