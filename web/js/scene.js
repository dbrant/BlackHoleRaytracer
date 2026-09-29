// Scene description: based on the default scene in Program.cs, with randomly placed spheres
// and stars (as in the C# project's commented-out scenes).
// Each hitable is a plain object whose fields mirror the Hitable struct in raytrace.frag.

// Must match the constants in raytrace.frag.
export const Kind = {
    SKY: 0,
    HORIZON: 1,
    DISK: 2,
    SPHERE: 3,
    REFLECTIVE_SPHERE: 4,
    GLASS_SPHERE: 5,
};

export const TextureSlot = {
    NONE: 0,
    SKY: 1,
    EARTH: 2,
    MARS: 3,
    DISK: 4,
    STAR: 5,
    // Not a texture: the procedural grid shown in place of the sky.
    SKY_GRID: 6,
};

export const TEXTURE_URLS = {
    [TextureSlot.SKY]: 'textures/sky4k.webp',
    [TextureSlot.EARTH]: 'textures/earth1k.webp',
    [TextureSlot.MARS]: 'textures/mars1k.webp',
    [TextureSlot.DISK]: 'textures/disk.webp',
    [TextureSlot.STAR]: 'textures/sun.webp',
};

// System.Drawing named colors used by the C# scene, as RGB in [0, 1].
const rgb = (r, g, b) => [r / 255, g / 255, b / 255];
const Colors = {
    white: rgb(255, 255, 255),
    blueViolet: rgb(138, 43, 226),
    mediumBlue: rgb(0, 0, 205),
    forestGreen: rgb(34, 139, 34),
    lightSeaGreen: rgb(32, 178, 170),
    royalBlue: rgb(65, 105, 225),
    darkBlue: rgb(0, 0, 139),
};

const SKY_RADIUS = 60;

function hitable(fields) {
    return {
        kind: Kind.SKY,
        center: [0, 0, 0],
        radiusSqr: 0,
        innerRadius: 0,
        outerRadius: 0,
        color1: Colors.white,
        color2: Colors.white,
        color3: Colors.white,
        color4: Colors.white,
        texture: TextureSlot.NONE,
        textureOffset: 0,
        checkered: false,
        ...fields,
    };
}

const sphere = (center, radius, fields = {}) =>
    hitable({ kind: Kind.SPHERE, center, radiusSqr: radius * radius, ...fields });

const reflectiveSphere = (center, radius) =>
    hitable({ kind: Kind.REFLECTIVE_SPHERE, center, radiusSqr: radius * radius });

const glassSphere = (center, radius) =>
    hitable({ kind: Kind.GLASS_SPHERE, center, radiusSqr: radius * radius });

const EARTH = { center: [10, 2, -1], radius: 1 };
const MARS = { center: [-10, -2, 1], radius: 1 };

// Randomly placed objects: how many, how big, and the shell (around the black hole) that
// their centers are placed in. A shell's optional maxHeight also keeps the centers within
// that distance of the disk plane (y = 0).
const MIRROR_COUNT = 5;
const CHECKERED_SPHERE_COUNT = 5;
const GLASS_SPHERE_COUNT = 5;
const SPHERE_RADIUS = 1;
const SPHERE_SHELL = { inner: 5, outer: 12 };
const STAR_COUNT = 20;
const STAR_MIN_RADIUS = 0.05;
const STAR_MAX_RADIUS = 0.45;
const STAR_SHELL = { inner: 4, outer: 20, maxHeight: 5 };

// Minimum gap between randomly placed objects, so they don't overlap.
const PLACEMENT_GAP = 0.5;
const MAX_PLACEMENT_ATTEMPTS = 1000;

/** A uniformly random point in the spherical shell between the given radii. */
function randomPointInShell({ inner, outer }, random) {
    // Uniform direction...
    const z = random() * 2 - 1;
    const phi = random() * 2 * Math.PI;
    const horizontal = Math.sqrt(1 - z * z);
    // ...and a radius that's uniform by volume.
    const r = Math.cbrt(inner ** 3 + random() * (outer ** 3 - inner ** 3));
    return [r * horizontal * Math.cos(phi), r * z, r * horizontal * Math.sin(phi)];
}

/**
 * Randomly places the mirror spheres, checkered spheres, and stars, so that they don't
 * overlap each other or the planets. Returns lists of {center, radius} (plus a texture
 * offset for stars), used by buildScene().
 */
export function createLayout(random = Math.random) {
    const placed = [EARTH, MARS];

    const place = (shell, radius) => {
        for (let attempt = 0; attempt < MAX_PLACEMENT_ATTEMPTS; attempt++) {
            const center = randomPointInShell(shell, random);
            if (shell.maxHeight !== undefined && Math.abs(center[1]) > shell.maxHeight) {
                continue;
            }
            const clear = placed.every((other) =>
                Math.hypot(center[0] - other.center[0], center[1] - other.center[1], center[2] - other.center[2])
                    >= radius + other.radius + PLACEMENT_GAP);
            if (clear) {
                const object = { center, radius };
                placed.push(object);
                return object;
            }
        }
        throw new Error('Could not find room to place all objects.');
    };

    const spheres = (count) => Array.from({ length: count }, () => place(SPHERE_SHELL, SPHERE_RADIUS));
    return {
        mirrors: spheres(MIRROR_COUNT),
        checkeredSpheres: spheres(CHECKERED_SPHERE_COUNT),
        glassSpheres: spheres(GLASS_SPHERE_COUNT),
        stars: Array.from({ length: STAR_COUNT }, () => ({
            ...place(STAR_SHELL, STAR_MIN_RADIUS + random() * (STAR_MAX_RADIUS - STAR_MIN_RADIUS)),
            textureOffset: random() * 2 * Math.PI,
        })),
    };
}

/**
 * Builds the list of hitables, with randomly placed objects from the given layout
 * (see createLayout). Order matters, as in the C# tracer: hitables are tested in this
 * order on every step, and a reflective sphere changes the ray for the rest.
 */
export function buildScene(options, layout) {
    const hitables = [];

    if (options.checkeredDisk) {
        hitables.push(hitable({
            kind: Kind.DISK,
            innerRadius: 10,
            outerRadius: 20,
            color1: Colors.blueViolet,
            color2: Colors.mediumBlue,
            color3: Colors.forestGreen,
            color4: Colors.lightSeaGreen,
        }));
    }
    if (options.accretionDisk) {
        hitables.push(hitable({ kind: Kind.DISK, innerRadius: 2.6, outerRadius: 8, texture: TextureSlot.DISK }));
    }

    hitables.push(hitable({ kind: Kind.HORIZON, checkered: options.checkeredHorizon }));
    hitables.push(hitable({
        kind: Kind.SKY,
        radiusSqr: SKY_RADIUS * SKY_RADIUS,
        texture: options.sky ? TextureSlot.SKY : TextureSlot.SKY_GRID,
    }));

    if (options.planets) {
        hitables.push(sphere(EARTH.center, EARTH.radius, { texture: TextureSlot.EARTH, textureOffset: Math.PI }));
        hitables.push(sphere(MARS.center, MARS.radius, { texture: TextureSlot.MARS }));
    }
    if (options.mirrors) {
        for (const { center, radius } of layout.mirrors) {
            hitables.push(reflectiveSphere(center, radius));
        }
    }
    if (options.checkeredSpheres) {
        for (const { center, radius } of layout.checkeredSpheres) {
            hitables.push(sphere(center, radius, { color1: Colors.royalBlue, color2: Colors.darkBlue }));
        }
    }
    if (options.glassSpheres) {
        for (const { center, radius } of layout.glassSpheres) {
            hitables.push(glassSphere(center, radius));
        }
    }
    if (options.stars) {
        for (const { center, radius, textureOffset } of layout.stars) {
            hitables.push(sphere(center, radius, { texture: TextureSlot.STAR, textureOffset }));
        }
    }

    return hitables;
}

// The camera must stay inside the sky sphere. It may cross the horizon (radius 1), but must
// keep clear of the singularity at the center, where the ray equations break down.
export const HORIZON_RADIUS = 1;
export const CAMERA_MIN_DISTANCE = 0.05;
export const CAMERA_MAX_DISTANCE = SKY_RADIUS - 5;
