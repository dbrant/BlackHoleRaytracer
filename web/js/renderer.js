import { Kind, TEXTURE_URLS, TextureSlot } from './scene.js';

const SAMPLER_UNIFORMS = {
    [TextureSlot.SKY]: 'uSkyTexture',
    [TextureSlot.EARTH]: 'uEarthTexture',
    [TextureSlot.MARS]: 'uMarsTexture',
    [TextureSlot.DISK]: 'uDiskTexture',
    [TextureSlot.STAR]: 'uStarTexture',
};

// How many compiled scene variants to keep. Each object toggle combination (and each
// randomized layout) is a separate program.
const MAX_CACHED_PROGRAMS = 16;

const UNIFORMS = [
    'uResolution', 'uCameraPosition', 'uCameraLeft', 'uCameraUp', 'uCameraFront',
    'uTanFov', 'uPotentialCoefficient', 'uMaxIterations', 'uSkyLoaded', ...Object.values(SAMPLER_UNIFORMS),
];

// Shown by textures that haven't loaded yet (other than the sky, which shows a grid instead).
const PLACEHOLDER_COLOR = [40, 40, 40, 255];

async function fetchText(url) {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`Failed to load ${url}: ${response.status}`);
    }
    return response.text();
}

function loadImage(url) {
    return new Promise((resolve, reject) => {
        const image = new Image();
        image.onload = () => resolve(image);
        image.onerror = () => reject(new Error(`Failed to load ${url}`));
        image.src = url;
    });
}

// GLSL literals.
const float = (x) => (Number.isInteger(x) ? x.toFixed(1) : String(x));
const vec3 = ([x, y, z]) => `vec3(${float(x)}, ${float(y)}, ${float(z)})`;

/** Generates the GLSL call that tests the ray against one hitable (other than spheres). */
function hitableCall(h) {
    switch (h.kind) {
        case Kind.SKY:
            return `hitSky(${float(h.radiusSqr)}, ${h.texture})`;
        case Kind.HORIZON:
            return `hitHorizon(${h.checkered})`;
        case Kind.DISK:
            return `hitDisk(${float(h.innerRadius)}, ${float(h.outerRadius)}, ` +
                `${vec3(h.color1)}, ${vec3(h.color2)}, ${vec3(h.color3)}, ${vec3(h.color4)}, ${h.texture})`;
        default:
            throw new Error(`Unknown hitable kind ${h.kind}`);
    }
}

// The value of hitKind in the generated sphere tests, for each kind of sphere.
const SPHERE_HIT_KINDS = {
    [Kind.SPHERE]: 1,
    [Kind.REFLECTIVE_SPHERE]: 2,
    [Kind.GLASS_SPHERE]: 3,
};

const isSphere = (h) => h.kind in SPHERE_HIT_KINDS;

/**
 * Generates the sphere tests in testHitables(). The spheres in the scene never overlap, so
 * the ray can be inside at most one of them at a time. Rather than calling hitSphere() for
 * every sphere (which the shader compiler inlines, intersection search and all, at every
 * call site), find the sphere with cheap tests, then run the hit code once.
 */
function generateSphereTests(spheres) {
    // Beyond the outermost sphere, none of them can be hit.
    const extent = Math.max(...spheres.map((h) => Math.hypot(...h.center) + Math.sqrt(h.radiusSqr)));
    const lines = [
        `    if (sqrNorm < ${float(extent * extent)}) {`,
        '        int hitKind = 0; // 0: none; otherwise, see SPHERE_HIT_KINDS',
        '        vec3 center, color1, color2;',
        '        float radiusSqr, textureOffset;',
        '        int texture;',
        '        vec3 d;',
    ];
    for (const h of spheres) {
        const center = vec3(h.center);
        const kind = SPHERE_HIT_KINDS[h.kind];
        lines.push(
            `        d = point - ${center};`,
            `        if (dot(d, d) < ${float(h.radiusSqr)}) {`,
            `            hitKind = ${kind}; center = ${center}; radiusSqr = ${float(h.radiusSqr)};`,
            `            color1 = ${vec3(h.color1)}; color2 = ${vec3(h.color2)};`,
            `            texture = ${h.texture}; textureOffset = ${float(h.textureOffset)};`,
            '        }',
        );
    }
    lines.push(
        '        if (hitKind == 1) {',
        '            hitSphere(center, radiusSqr, color1, color2, texture, textureOffset);',
        '        } else if (hitKind == 2) {',
        '            hitReflectiveSphere(center, radiusSqr);',
        '        } else if (hitKind == 3) {',
        '            hitGlassSphere(center, radiusSqr);',
        '        }',
        '    }',
    );
    return lines;
}

/**
 * Generates testHitables() for raytrace.frag: tests each hitable in order, stopping as
 * soon as one of them ends the ray (as in the C# tracer). Spheres are tested last, and
 * must not overlap each other (see generateSphereTests).
 */
function generateTestHitables(hitables) {
    const lines = [];
    for (const h of hitables.filter((h) => !isSphere(h))) {
        lines.push(`    ${hitableCall(h)};`, '    if (stop) return;');
    }
    const spheres = hitables.filter(isSphere);
    if (spheres.length > 0) {
        lines.push(...generateSphereTests(spheres));
    }
    return `\nvoid testHitables() {\n${lines.join('\n')}\n}\n`;
}

function compileShader(gl, type, source, name) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        throw new Error(`Error compiling ${name}:\n${gl.getShaderInfoLog(shader)}`);
    }
    return shader;
}

/**
 * Renders the scene with the raytracing fragment shader, drawing one full-screen triangle.
 */
export class Renderer {
    static async create(canvas) {
        const gl = canvas.getContext('webgl2', { antialias: false, depth: false, powerPreference: 'high-performance' });
        if (!gl) {
            throw new Error('WebGL 2 is not available in this browser.');
        }
        // Textures aren't loaded here, but when a scene first uses them (see setScene).
        const [vertexSource, fragmentSource] = await Promise.all([
            fetchText('shaders/fullscreen.vert'),
            fetchText('shaders/raytrace.frag'),
        ]);
        return new Renderer(gl, vertexSource, fragmentSource);
    }

    constructor(gl, vertexSource, fragmentSource) {
        this.gl = gl;
        this.vertexShader = compileShader(gl, gl.VERTEX_SHADER, vertexSource, 'fullscreen.vert');
        this.fragmentSource = fragmentSource;
        // Compiled programs, keyed by their generated testHitables() source.
        this.programs = new Map();
        this.program = null;
        // Every texture starts out as a placeholder, and is loaded when first needed.
        this.textures = new Map(Object.keys(TEXTURE_URLS).map((slot) => [
            Number(slot), { texture: this.createTexture(), requested: false, loaded: false },
        ]));
        // Called when a texture finishes loading, so the view can be rendered again.
        this.onTextureLoaded = () => {};
        this.vertexArray = gl.createVertexArray();

        // GPU timer, where supported. Frame times without it would only measure how fast
        // commands are queued, not how long the GPU takes to run them.
        this.timerExt = gl.getExtension('EXT_disjoint_timer_query_webgl2');
        this.pendingQueries = [];
        this.lastGpuTimeMs = null;
    }

    createProgram(testHitablesSource) {
        const gl = this.gl;
        const fragmentShader = compileShader(gl, gl.FRAGMENT_SHADER, this.fragmentSource + testHitablesSource, 'raytrace.frag');
        const program = gl.createProgram();
        gl.attachShader(program, this.vertexShader);
        gl.attachShader(program, fragmentShader);
        gl.linkProgram(program);
        gl.deleteShader(fragmentShader);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
            throw new Error(`Error linking shaders:\n${gl.getProgramInfoLog(program)}`);
        }
        const uniforms = {};
        for (const name of UNIFORMS) {
            uniforms[name] = gl.getUniformLocation(program, name);
        }
        return { program, uniforms };
    }

    /** Creates a texture holding a single placeholder pixel, until the image is loaded. */
    createTexture() {
        const gl = this.gl;
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE,
            new Uint8Array(PLACEHOLDER_COLOR));
        gl.generateMipmap(gl.TEXTURE_2D);
        // Textures wrap around horizontally (longitude), and clamp at the poles.
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        const anisotropic = gl.getExtension('EXT_texture_filter_anisotropic');
        if (anisotropic) {
            gl.texParameterf(gl.TEXTURE_2D, anisotropic.TEXTURE_MAX_ANISOTROPY_EXT,
                Math.min(8, gl.getParameter(anisotropic.MAX_TEXTURE_MAX_ANISOTROPY_EXT)));
        }
        return texture;
    }

    /** Starts loading the image for a texture slot, if it hasn't been already. */
    requestTexture(slot) {
        const entry = this.textures.get(slot);
        if (!entry || entry.requested) {
            return;
        }
        entry.requested = true;
        loadImage(TEXTURE_URLS[slot]).then((image) => {
            const gl = this.gl;
            gl.bindTexture(gl.TEXTURE_2D, entry.texture);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, image);
            gl.generateMipmap(gl.TEXTURE_2D);
            entry.loaded = true;
            this.onTextureLoaded(slot);
        }, (error) => {
            // Keep showing the placeholder, and try again the next time the scene changes.
            console.error(error);
            entry.requested = false;
        });
    }

    /**
     * Sets the hitables to render, and starts loading any textures they use. The first time
     * a given scene is used, this compiles a shader specialized for it, which can take a moment.
     */
    setScene(hitables) {
        for (const h of hitables) {
            this.requestTexture(h.texture);
        }
        const source = generateTestHitables(hitables);
        let program = this.programs.get(source);
        if (program) {
            // Move to the end, so the least recently used program is evicted first.
            this.programs.delete(source);
        } else {
            program = this.createProgram(source);
            if (this.programs.size >= MAX_CACHED_PROGRAMS) {
                const [oldestSource, oldest] = this.programs.entries().next().value;
                this.gl.deleteProgram(oldest.program);
                this.programs.delete(oldestSource);
            }
        }
        this.programs.set(source, program);
        this.program = program;
    }

    /**
     * Renders one frame at the given resolution.
     * @param camera {position, front, left, up}
     * @param params {fov (degrees), curvature, maxIterations}
     */
    render(width, height, camera, params) {
        const gl = this.gl;
        const canvas = gl.canvas;
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }
        gl.viewport(0, 0, width, height);

        const { program, uniforms: u } = this.program;
        gl.useProgram(program);
        gl.uniform2f(u.uResolution, width, height);
        gl.uniform3fv(u.uCameraPosition, camera.position);
        gl.uniform3fv(u.uCameraLeft, camera.left);
        gl.uniform3fv(u.uCameraUp, camera.up);
        gl.uniform3fv(u.uCameraFront, camera.front);
        gl.uniform1f(u.uTanFov, Math.tan(params.fov * Math.PI / 180));
        gl.uniform1f(u.uPotentialCoefficient, params.curvature);
        gl.uniform1i(u.uMaxIterations, params.maxIterations);
        gl.uniform1i(u.uSkyLoaded, this.textures.get(TextureSlot.SKY).loaded ? 1 : 0);

        let unit = 0;
        for (const [slot, { texture }] of this.textures) {
            gl.activeTexture(gl.TEXTURE0 + unit);
            gl.bindTexture(gl.TEXTURE_2D, texture);
            gl.uniform1i(u[SAMPLER_UNIFORMS[slot]], unit);
            unit++;
        }

        const query = this.beginTimer();
        gl.bindVertexArray(this.vertexArray);
        gl.drawArrays(gl.TRIANGLES, 0, 3);
        this.endTimer(query);
    }

    beginTimer() {
        if (!this.timerExt) {
            return null;
        }
        const gl = this.gl;
        const query = gl.createQuery();
        gl.beginQuery(this.timerExt.TIME_ELAPSED_EXT, query);
        return query;
    }

    endTimer(query) {
        if (!query) {
            return;
        }
        this.gl.endQuery(this.timerExt.TIME_ELAPSED_EXT);
        this.pendingQueries.push(query);
    }

    /**
     * Returns the GPU time of the most recently completed frame in milliseconds,
     * or null if GPU timing is unavailable. Results arrive a frame or more late.
     */
    pollGpuTime() {
        const gl = this.gl;
        if (!this.timerExt) {
            return null;
        }
        if (gl.getParameter(this.timerExt.GPU_DISJOINT_EXT)) {
            // Timings are unreliable (e.g. the GPU changed clocks); discard them.
            this.pendingQueries.forEach((q) => gl.deleteQuery(q));
            this.pendingQueries = [];
            return this.lastGpuTimeMs;
        }
        while (this.pendingQueries.length > 0 && gl.getQueryParameter(this.pendingQueries[0], gl.QUERY_RESULT_AVAILABLE)) {
            const query = this.pendingQueries.shift();
            this.lastGpuTimeMs = gl.getQueryParameter(query, gl.QUERY_RESULT) / 1e6;
            gl.deleteQuery(query);
        }
        return this.lastGpuTimeMs;
    }
}
