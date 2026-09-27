import { Kind, TEXTURE_URLS, TextureSlot } from './scene.js';

const SAMPLER_UNIFORMS = {
    [TextureSlot.SKY]: 'uSkyTexture',
    [TextureSlot.EARTH]: 'uEarthTexture',
    [TextureSlot.MARS]: 'uMarsTexture',
    [TextureSlot.DISK]: 'uDiskTexture',
};

const UNIFORMS = [
    'uResolution', 'uCameraPosition', 'uCameraLeft', 'uCameraUp', 'uCameraFront',
    'uTanFov', 'uPotentialCoefficient', 'uMaxIterations', ...Object.values(SAMPLER_UNIFORMS),
];

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

/** Generates the GLSL call that tests the ray against one hitable. */
function hitableCall(h) {
    switch (h.kind) {
        case Kind.SKY:
            return `hitSky(${float(h.radiusSqr)}, ${h.texture})`;
        case Kind.HORIZON:
            return `hitHorizon(${h.checkered})`;
        case Kind.DISK:
            return `hitDisk(${float(h.innerRadius)}, ${float(h.outerRadius)}, ` +
                `${vec3(h.color1)}, ${vec3(h.color2)}, ${vec3(h.color3)}, ${vec3(h.color4)}, ${h.texture})`;
        case Kind.SPHERE:
            return `hitSphere(${vec3(h.center)}, ${float(h.radiusSqr)}, ` +
                `${vec3(h.color1)}, ${vec3(h.color2)}, ${h.texture}, ${float(h.textureOffset)})`;
        case Kind.REFLECTIVE_SPHERE:
            return `hitReflectiveSphere(${vec3(h.center)}, ${float(h.radiusSqr)})`;
        default:
            throw new Error(`Unknown hitable kind ${h.kind}`);
    }
}

/**
 * Generates testHitables() for raytrace.frag: tests each hitable in order, stopping as
 * soon as one of them ends the ray (as in the C# tracer).
 */
function generateTestHitables(hitables) {
    const calls = hitables.map((h) => `    ${hitableCall(h)};\n    if (stop) return;`);
    return `\nvoid testHitables() {\n${calls.join('\n')}\n}\n`;
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
        const [vertexSource, fragmentSource, images] = await Promise.all([
            fetchText('shaders/fullscreen.vert'),
            fetchText('shaders/raytrace.frag'),
            Promise.all(Object.entries(TEXTURE_URLS).map(async ([slot, url]) => [slot, await loadImage(url)])),
        ]);
        return new Renderer(gl, vertexSource, fragmentSource, images);
    }

    constructor(gl, vertexSource, fragmentSource, images) {
        this.gl = gl;
        this.vertexShader = compileShader(gl, gl.VERTEX_SHADER, vertexSource, 'fullscreen.vert');
        this.fragmentSource = fragmentSource;
        // Compiled programs, keyed by their generated testHitables() source.
        this.programs = new Map();
        this.program = null;
        this.textures = images.map(([slot, image]) => ({ slot: Number(slot), texture: this.createTexture(image) }));
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

    createTexture(image) {
        const gl = this.gl;
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, image);
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

    /**
     * Sets the hitables to render. The first time a given scene is used, this compiles a
     * shader specialized for it, which can take a moment.
     */
    setScene(hitables) {
        const source = generateTestHitables(hitables);
        if (!this.programs.has(source)) {
            this.programs.set(source, this.createProgram(source));
        }
        this.program = this.programs.get(source);
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

        this.textures.forEach(({ slot, texture }, unit) => {
            gl.activeTexture(gl.TEXTURE0 + unit);
            gl.bindTexture(gl.TEXTURE_2D, texture);
            gl.uniform1i(u[SAMPLER_UNIFORMS[slot]], unit);
        });

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
