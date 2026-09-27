import { cross, normalize, sub } from './vec3.js';

const DEG = Math.PI / 180;
const MAX_ELEVATION = 85 * DEG;

/**
 * A camera that orbits the black hole (at the origin), controlled by dragging and scrolling.
 */
export class OrbitCamera {
    constructor({ distance, azimuth, elevation, minDistance, maxDistance }) {
        this.initial = { distance, azimuth, elevation };
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
        this.reset();
    }

    reset() {
        Object.assign(this, this.initial);
    }

    /** Creates a camera at the given position, looking at the origin. */
    static fromPosition([x, y, z], limits) {
        const distance = Math.hypot(x, y, z);
        return new OrbitCamera({
            distance,
            azimuth: Math.atan2(x, -z),
            elevation: Math.asin(y / distance),
            ...limits,
        });
    }

    get position() {
        const horizontal = this.distance * Math.cos(this.elevation);
        return [
            horizontal * Math.sin(this.azimuth),
            this.distance * Math.sin(this.elevation),
            -horizontal * Math.cos(this.azimuth),
        ];
    }

    /**
     * Camera basis, computed the same way as in the C# tracers
     * (which call the right-hand vector "left").
     */
    get basis() {
        const position = this.position;
        const front = normalize(sub([0, 0, 0], position));
        const left = normalize(cross([0, 1, 0], front));
        const up = cross(front, left);
        return { position, front, left, up };
    }

    rotate(deltaAzimuth, deltaElevation) {
        this.azimuth += deltaAzimuth;
        this.elevation = Math.min(MAX_ELEVATION, Math.max(-MAX_ELEVATION, this.elevation + deltaElevation));
    }

    zoom(factor) {
        this.distance = Math.min(this.maxDistance, Math.max(this.minDistance, this.distance * factor));
    }

    /**
     * Wires up mouse, touch, and wheel input on the given element.
     * onChange is called whenever the camera moves.
     */
    attach(element, onChange) {
        const pointers = new Map();
        let pinchDistance = 0;

        const pinchSpan = () => {
            const [a, b] = [...pointers.values()];
            return Math.hypot(a.x - b.x, a.y - b.y);
        };

        element.addEventListener('pointerdown', (e) => {
            element.setPointerCapture(e.pointerId);
            pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
            if (pointers.size === 2) {
                pinchDistance = pinchSpan();
            }
        });

        element.addEventListener('pointermove', (e) => {
            const last = pointers.get(e.pointerId);
            if (!last) {
                return;
            }
            const dx = e.clientX - last.x;
            const dy = e.clientY - last.y;
            pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

            if (pointers.size === 1) {
                // Grab-and-drag: the scene turns with the pointer (like three.js OrbitControls).
                const speed = 0.3 * DEG;
                this.rotate(-dx * speed, dy * speed);
                onChange();
            } else if (pointers.size === 2) {
                const span = pinchSpan();
                if (pinchDistance > 0 && span > 0) {
                    this.zoom(pinchDistance / span);
                    onChange();
                }
                pinchDistance = span;
            }
        });

        const release = (e) => {
            pointers.delete(e.pointerId);
            pinchDistance = pointers.size === 2 ? pinchSpan() : 0;
        };
        element.addEventListener('pointerup', release);
        element.addEventListener('pointercancel', release);

        element.addEventListener('wheel', (e) => {
            e.preventDefault();
            this.zoom(Math.exp(e.deltaY * 0.001));
            onChange();
        }, { passive: false });
    }
}
