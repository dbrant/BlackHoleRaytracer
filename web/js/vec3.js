// Minimal 3-vector helpers, on plain [x, y, z] arrays.

export const add = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];

export const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];

export const scale = (a, s) => [a[0] * s, a[1] * s, a[2] * s];

export const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

export const cross = (a, b) => [
    a[1] * b[2] - a[2] * b[1],
    a[2] * b[0] - a[0] * b[2],
    a[0] * b[1] - a[1] * b[0],
];

export const length = (a) => Math.hypot(a[0], a[1], a[2]);

export function normalize(a) {
    const len = length(a);
    return [a[0] / len, a[1] / len, a[2] / len];
}

/** Rotates v by angle (radians) around the given unit axis (Rodrigues' rotation formula). */
export function rotate(v, axis, angle) {
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return add(add(scale(v, cos), scale(cross(axis, v), sin)), scale(axis, dot(axis, v) * (1 - cos)));
}
