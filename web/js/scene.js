// Scene description: the same objects as the default scene in Program.cs.
// Each hitable is a plain object whose fields mirror the Hitable struct in raytrace.frag.

// Must match the constants in raytrace.frag.
export const Kind = {
    SKY: 0,
    HORIZON: 1,
    DISK: 2,
    SPHERE: 3,
    REFLECTIVE_SPHERE: 4,
};

export const TextureSlot = {
    NONE: 0,
    SKY: 1,
    EARTH: 2,
    MARS: 3,
    DISK: 4,
};

export const TEXTURE_URLS = {
    [TextureSlot.SKY]: 'textures/sky4k.jpg',
    [TextureSlot.EARTH]: 'textures/earth1k.jpg',
    [TextureSlot.MARS]: 'textures/mars1k.jpg',
    [TextureSlot.DISK]: 'textures/disk.jpg',
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

/**
 * Builds the list of hitables. Order matters, as in the C# tracer: hitables are tested
 * in this order on every step, and a reflective sphere changes the ray for the rest.
 */
export function buildScene(options) {
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
    hitables.push(hitable({ kind: Kind.SKY, radiusSqr: SKY_RADIUS * SKY_RADIUS, texture: TextureSlot.SKY }));

    if (options.planets) {
        hitables.push(sphere([10, 2, -1], 1, { texture: TextureSlot.EARTH, textureOffset: Math.PI }));
        hitables.push(sphere([-10, -2, 1], 1, { texture: TextureSlot.MARS }));
    }
    if (options.mirrors) {
        hitables.push(reflectiveSphere([-1, 2, -10], 1));
        hitables.push(reflectiveSphere([3, -3, -7], 1));
        hitables.push(reflectiveSphere([3, -5, 5], 1));
        hitables.push(reflectiveSphere([-3.7, 2, -7], 1));
    }
    if (options.checkeredSphere) {
        hitables.push(sphere([-10, -10, -10], 1, { color1: Colors.royalBlue, color2: Colors.darkBlue }));
    }

    return hitables;
}

// The camera must stay inside the sky sphere. It may cross the horizon (radius 1), but must
// keep clear of the singularity at the center, where the ray equations break down.
export const HORIZON_RADIUS = 1;
export const CAMERA_MIN_DISTANCE = 0.05;
export const CAMERA_MAX_DISTANCE = SKY_RADIUS - 5;
