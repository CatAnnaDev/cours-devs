shader_type spatial;

uniform sampler2D texture_deformation : filter_linear, repeat_disable;
uniform vec4 zone = vec4(0.0, 0.0, 20.0, 20.0);
uniform float profondeur_max : hint_range(0.0, 2.0) = 0.25;
uniform vec3 couleur_surface : source_color = vec3(0.92, 0.94, 1.0);
uniform vec3 couleur_creux : source_color = vec3(0.58, 0.64, 0.80);
uniform vec3 couleur_bourrelet : source_color = vec3(1.0, 1.0, 1.0);
uniform float largeur_bourrelet : hint_range(0.001, 1.0) = 0.25;

varying vec3 position_monde;

vec2 uv_zone(vec3 monde) {
    return (monde.xz - zone.xy) / zone.zw + 0.5;
}

void vertex() {
    position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float enfoncement = textureLod(texture_deformation, uv_zone(position_monde), 0.0).r;
    VERTEX.y -= enfoncement * profondeur_max;
}

void fragment() {
    vec2 uv = uv_zone(position_monde);
    vec2 pas = 1.0 / vec2(textureSize(texture_deformation, 0));

    float centre = texture(texture_deformation, uv).r;
    float dx = texture(texture_deformation, uv + vec2(pas.x, 0.0)).r
             - texture(texture_deformation, uv - vec2(pas.x, 0.0)).r;
    float dz = texture(texture_deformation, uv + vec2(0.0, pas.y)).r
             - texture(texture_deformation, uv - vec2(0.0, pas.y)).r;

    float taille_texel_x = zone.z * pas.x;
    float taille_texel_z = zone.w * pas.y;
    vec3 normale_monde = normalize(vec3(
        dx * profondeur_max / (2.0 * taille_texel_x),
        1.0,
        dz * profondeur_max / (2.0 * taille_texel_z)));

    float pente = length(vec2(dx, dz));
    float bourrelet = smoothstep(0.0, largeur_bourrelet, pente) * (1.0 - centre);

    vec3 couleur = mix(couleur_surface, couleur_creux, centre);
    couleur = mix(couleur, couleur_bourrelet, bourrelet);

    NORMAL = (VIEW_MATRIX * vec4(normale_monde, 0.0)).xyz;
    ALBEDO = couleur;
    ROUGHNESS = mix(0.45, 0.85, centre);
}
