import { add, cross, length, normalize, rotate, scale, sub } from './vec3.js';

const DEG = Math.PI / 180;
const MAX_PITCH = 85 * DEG;
const WORLD_UP = [0, 1, 0];

const clamp = (x, min, max) => Math.min(max, Math.max(min, x));

/**
 * The observer: a position, a view direction (yaw and pitch, so the horizon stays level),
 * and a speed of travel along the view direction.
 */
export class Camera {
    constructor({ position, minDistance, maxDistance }) {
        this.initialPosition = position;
        // The camera must stay clear of the singularity, and inside the sky sphere.
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
        this.reset();
    }

    /** Returns to the initial position, looking at the black hole, and stops. */
    reset() {
        this.position = [...this.initialPosition];
        const front = normalize(sub([0, 0, 0], this.position));
        this.yaw = Math.atan2(front[0], front[2]);
        this.pitch = clamp(Math.asin(front[1]), -MAX_PITCH, MAX_PITCH);
        this.speed = 0;
    }

    /** Distance from the black hole. */
    get distance() {
        return length(this.position);
    }

    get front() {
        return [
            Math.cos(this.pitch) * Math.sin(this.yaw),
            Math.sin(this.pitch),
            Math.cos(this.pitch) * Math.cos(this.yaw),
        ];
    }

    /**
     * Camera basis, computed the same way as in the C# tracers
     * (which call the right-hand vector "left").
     */
    get basis() {
        const front = this.front;
        const left = normalize(cross(WORLD_UP, front));
        const up = cross(front, left);
        return { position: this.position, front, left, up };
    }

    /** Turns the view, and with it the direction of travel. Positive values turn right and up. */
    turn(deltaYaw, deltaPitch) {
        this.yaw += deltaYaw;
        this.pitch = clamp(this.pitch + deltaPitch, -MAX_PITCH, MAX_PITCH);
    }

    /**
     * Swings the camera around the black hole: horizontally around the vertical axis, and
     * vertically around the camera's horizontal axis. The view turns along with it, so a
     * camera looking at the black hole keeps looking at it.
     */
    orbit(horizontalAngle, verticalAngle) {
        this.position = rotate(this.position, WORLD_UP, horizontalAngle);
        this.yaw += horizontalAngle;

        // Rotating around the camera's horizontal axis changes the pitch by exactly
        // -verticalAngle; limit the angle to keep the pitch in range.
        const angle = clamp(verticalAngle, this.pitch - MAX_PITCH, this.pitch + MAX_PITCH);
        const { left } = this.basis;
        this.position = rotate(this.position, left, angle);
        this.pitch -= angle;
    }

    /** Moves toward (factor < 1) or away from (factor > 1) the black hole. */
    zoom(factor) {
        const distance = this.distance;
        const newDistance = clamp(distance * factor, this.minDistance, this.maxDistance);
        this.position = scale(this.position, newDistance / distance);
    }

    /**
     * Travels at the current speed for dt seconds. Returns true if the camera moved.
     * Running into the singularity or the sky sphere stops the camera there.
     */
    fly(dt) {
        if (this.speed === 0) {
            return false;
        }
        this.position = add(this.position, scale(this.front, this.speed * dt));
        const distance = this.distance;
        if (distance < this.minDistance || distance > this.maxDistance) {
            this.position = scale(this.position, clamp(distance, this.minDistance, this.maxDistance) / distance);
            this.speed = 0;
        }
        return true;
    }

    /**
     * Wires up mouse, touch, and wheel input on the given element: drag (or one-finger drag)
     * to orbit, and scroll (or pinch) to zoom. onChange is called whenever the camera moves.
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
                this.orbit(dx * speed, dy * speed);
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
