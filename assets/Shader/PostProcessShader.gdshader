shader_type spatial;
render_mode unshaded, depth_draw_opaque, depth_prepass_alpha, blend_mix;

uniform sampler2D depth_texture : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D normal_texture : hint_normal_roughness_texture, filter_linear_mipmap;
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform sampler2D hatch1 : filter_nearest, repeat_enable;
uniform sampler2D hatch2 : filter_nearest, repeat_enable;
uniform sampler2D hatch3 : filter_nearest, repeat_enable;

uniform vec3 edge_color : source_color = vec3(0.0, 0.0, 0.0);
uniform float edge_threshold : hint_range(0.001, 0.2, 0.001) = 0.01;

uniform float noise_frequency : hint_range(0.1, 20.0, 0.1) = 10.0;
uniform float noise_offset_intensity : hint_range(0.0, 0.2, 0.001) = 0.002;

vec2 hash(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)),
             dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

vec2 fade(vec2 t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float grad(vec2 hash, vec2 dir) {
    vec2 grad_dir = vec2(hash.x * 2.0 - 1.0, hash.y * 2.0 - 1.0);
    return dot(grad_dir, dir);
}

float perlin(vec2 pos) {
    vec2 p = floor(pos);
    vec2 f = fract(pos);
    f = fade(f);

    float tl = grad(hash(p), f);
    float tr = grad(hash(p + vec2(1.0, 0.0)), f - vec2(1.0, 0.0));
    float bl = grad(hash(p + vec2(0.0, 1.0)), f - vec2(0.0, 1.0));
    float br = grad(hash(p + vec2(1.0, 1.0)), f - vec2(1.0, 1.0));

    float top = mix(tl, tr, f.x);
    float bottom = mix(bl, br, f.x);
    return mix(top, bottom, f.y);
}

void fragment() {
    vec2 screen_uv = SCREEN_UV;
    vec3 screen_color = texture(screen_texture, screen_uv).rgb;

    // Perlin noise offset
    vec2 noise = vec2(
        perlin(screen_uv * noise_frequency),
        perlin((screen_uv + vec2(0.5)) * noise_frequency)
    );
    vec2 uv = clamp(screen_uv + noise * noise_offset_intensity, 0.0, 1.0);

    // Sobel edge detection
    float dx = 1.0 / VIEWPORT_SIZE.x;
    float dy = 1.0 / VIEWPORT_SIZE.y;

    mat3 Gx = mat3(
        vec3(-1, -2, -1),
        vec3( 0,  0,  0),
        vec3( 1,  2,  1)
    );

    mat3 Gy = mat3(
        vec3(-1, 0, 1),
        vec3(-2, 0, 2),
        vec3(-1, 0, 1)
    );

    float depthSobelX = 0.0;
    float depthSobelY = 0.0;
    float normalSobelX = 0.0;
    float normalSobelY = 0.0;

    for (int i = -1; i <= 1; i++) {
        for (int j = -1; j <= 1; j++) {
            vec2 offset = vec2(float(i) * dx, float(j) * dy);
            float depth = texture(depth_texture, uv + offset).r;
            vec3 normal = normalize(texture(normal_texture, uv + offset).rgb * 2.0 - 1.0);

            depthSobelX += Gx[i+1][j+1] * depth;
            depthSobelY += Gy[i+1][j+1] * depth;
            normalSobelX += Gx[i+1][j+1] * normal.x;
            normalSobelY += Gy[i+1][j+1] * normal.y;
        }
    }

    float edge_magnitude = length(vec2(depthSobelX, depthSobelY)) + length(vec2(normalSobelX, normalSobelY));

    // Hatching based on luminance
    float luminance = dot(screen_color, vec3(0.299, 0.587, 0.114));
    vec3 normal_sample = normalize(texture(normal_texture, uv).rgb * 2.0 - 1.0);
    vec2 distorted_uv = clamp(uv + normal_sample.xy * 2.0, 0.0, 1.0);

    vec4 hatch_color;
    if (luminance < 0.6) {
        hatch_color = texture(hatch3, distorted_uv * 20.0);
    } else if (luminance < 0.7) {
        hatch_color = texture(hatch2, distorted_uv * 20.0);
    } else if (luminance < 0.8) {
        hatch_color = texture(hatch1, distorted_uv * 20.0);
    } else {
        hatch_color = vec4(screen_color, 1.0);
    }

    // Final output
    if (edge_magnitude > edge_threshold) {
        ALBEDO = edge_color;
    } else {
        ALBEDO = hatch_color.rgb;
    }

    if (edge_magnitude > edge_threshold) {
    ALBEDO = edge_color;
} else {
    ALBEDO = hatch_color.rgb;
}

// Blend with scene
float alpha_mask = clamp((1.0 - edge_magnitude * 5.0) * (luminance * 1.5 - 0.2), 0.0, 1.0);
ALPHA = alpha_mask;
DEPTH = texture(depth_texture, screen_uv).r;
}
