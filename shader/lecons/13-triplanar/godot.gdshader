shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D normales : hint_normal, filter_linear_mipmap, repeat_enable;
uniform float echelle : hint_range(0.01, 4.0) = 0.25;
uniform float nettete : hint_range(1.0, 16.0) = 4.0;
uniform float force_normales : hint_range(0.0, 3.0) = 1.0;
uniform float rugosite : hint_range(0.0, 1.0) = 0.85;

varying vec3 position_monde;
varying vec3 normale_monde;

void vertex() {
    position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    normale_monde = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
    vec3 normale = normalize(normale_monde);
    vec3 poids = pow(abs(normale), vec3(nettete));
    poids /= (poids.x + poids.y + poids.z);

    vec3 p = position_monde * echelle;

    vec3 couleur_x = texture(texture_base, p.zy).rgb;
    vec3 couleur_y = texture(texture_base, p.xz).rgb;
    vec3 couleur_z = texture(texture_base, p.xy).rgb;

    vec3 normale_x = texture(normales, p.zy).xyz * 2.0 - 1.0;
    vec3 normale_y = texture(normales, p.xz).xyz * 2.0 - 1.0;
    vec3 normale_z = texture(normales, p.xy).xyz * 2.0 - 1.0;

    normale_x.xy *= force_normales;
    normale_y.xy *= force_normales;
    normale_z.xy *= force_normales;

    normale_x = vec3(normale_x.xy + normale.zy, abs(normale_x.z) * normale.x);
    normale_y = vec3(normale_y.xy + normale.xz, abs(normale_y.z) * normale.y);
    normale_z = vec3(normale_z.xy + normale.xy, abs(normale_z.z) * normale.z);

    vec3 assemblee = normalize(
        normale_x.zyx * poids.x +
        normale_y.xzy * poids.y +
        normale_z.xyz * poids.z);

    ALBEDO = couleur_x * poids.x + couleur_y * poids.y + couleur_z * poids.z;
    NORMAL = (VIEW_MATRIX * vec4(assemblee, 0.0)).xyz;
    ROUGHNESS = rugosite;
}
