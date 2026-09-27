// Minimal 3-vector helpers, on plain [x, y, z] arrays.

export const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];

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
