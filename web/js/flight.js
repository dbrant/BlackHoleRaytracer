// Keyboard flight controls: W/S accelerate forward/backward, the arrow keys steer, and
// Space stops.

const DEG = Math.PI / 180;
const ACCELERATION = 4; // distance units per second squared
const MAX_SPEED = 20; // distance units per second
const TURN_RATE = 60 * DEG; // radians per second

// KeyboardEvent.code values, which name physical keys, so W/S are in the same place on
// non-QWERTY layouts.
const FLIGHT_KEYS = new Set(['KeyW', 'KeyS', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Space']);

/**
 * Whether the focused element uses this key itself (e.g. the arrow keys adjust a slider,
 * and Space toggles a checkbox), in which case it's left alone.
 */
function isUsedByElement(element, code) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }
    const type = element instanceof HTMLInputElement ? element.type : null;
    if (element instanceof HTMLSelectElement) {
        return code.startsWith('Arrow') || code === 'Space';
    }
    if (type === 'range') {
        return code.startsWith('Arrow');
    }
    if (type === 'checkbox' || element instanceof HTMLButtonElement) {
        return code === 'Space';
    }
    return element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element.isContentEditable;
}

export class FlightControls {
    constructor(camera) {
        this.camera = camera;
        this.pressed = new Set();
    }

    attach(target = window) {
        target.addEventListener('keydown', (e) => {
            if (!FLIGHT_KEYS.has(e.code) || e.ctrlKey || e.altKey || e.metaKey || isUsedByElement(e.target, e.code)) {
                return;
            }
            // Don't let the arrow keys and Space scroll the page.
            e.preventDefault();
            if (e.code === 'Space') {
                this.camera.speed = 0;
            } else {
                this.pressed.add(e.code);
            }
        });
        target.addEventListener('keyup', (e) => this.pressed.delete(e.code));
        // Keys released while the window isn't focused never send keyup.
        target.addEventListener('blur', () => this.pressed.clear());
    }

    /** Applies held keys and moves the camera for dt seconds. Returns true if the camera moved. */
    update(dt) {
        const held = (code) => (this.pressed.has(code) ? 1 : 0);
        const camera = this.camera;

        const turnRight = held('ArrowRight') - held('ArrowLeft');
        const turnUp = held('ArrowUp') - held('ArrowDown');
        const turned = turnRight !== 0 || turnUp !== 0;
        if (turned) {
            camera.turn(turnRight * TURN_RATE * dt, turnUp * TURN_RATE * dt);
        }

        const thrust = held('KeyW') - held('KeyS');
        if (thrust !== 0) {
            camera.speed = Math.min(MAX_SPEED, Math.max(-MAX_SPEED, camera.speed + thrust * ACCELERATION * dt));
        }

        const moved = camera.fly(dt);
        return turned || moved;
    }
}
