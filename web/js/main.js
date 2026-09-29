import { Camera } from './camera.js';
import { FlightControls } from './flight.js';
import { Renderer } from './renderer.js';
import { buildScene, createLayout, CAMERA_MAX_DISTANCE, CAMERA_MIN_DISTANCE, HORIZON_RADIUS } from './scene.js';
import { normalize, scale } from './vec3.js';

// How long after the last camera movement to render at full resolution again.
const SETTLE_DELAY_MS = 150;
const AUTO_ROTATE_SPEED = 0.15; // radians per second

// The viewport grows to show higher resolutions at one CSS pixel per rendered pixel, but
// never shrinks below this; lower resolutions are scaled up to fill it.
const MIN_VIEWPORT_SIZE = 512;

const canvas = document.getElementById('view');
const statusElement = document.getElementById('status');
const statsElement = document.getElementById('stats');
const controls = document.getElementById('controls');

// Start slightly above the disk plane, and at a moderate distance from the black hole,
// so that the accretion disk fills most of the view.
const START_DISTANCE = 15;
const camera = new Camera({
    position: scale(normalize([0, 5, -35]), START_DISTANCE),
    minDistance: CAMERA_MIN_DISTANCE,
    maxDistance: CAMERA_MAX_DISTANCE,
});
const flight = new FlightControls(camera);

const settings = {};

function readSettings() {
    const value = (id) => document.getElementById(id);
    Object.assign(settings, {
        resolution: Number(value('resolution').value),
        fov: Number(value('fov').value),
        curvature: -Number(value('curvature').value),
        maxIterations: Number(value('max-iterations').value),
        checkeredDisk: value('checkered-disk').checked,
        accretionDisk: value('accretion-disk').checked,
        planets: value('planets').checked,
        mirrors: value('mirrors').checked,
        checkeredSpheres: value('checkered-spheres').checked,
        glassSpheres: value('glass-spheres').checked,
        stars: value('stars').checked,
        checkeredHorizon: value('checkered-horizon').checked,
        autoRotate: value('auto-rotate').checked,
        lowResWhileMoving: value('low-res-while-moving').checked,
    });
    document.documentElement.style.setProperty('--viewport-size',
        `${Math.max(MIN_VIEWPORT_SIZE, settings.resolution)}px`);

    // Show the current value next to each slider.
    for (const input of controls.querySelectorAll('input[type=range]')) {
        const output = controls.querySelector(`output[for="${input.id}"]`);
        if (output) {
            output.textContent = input.value;
        }
    }
}

function showError(error) {
    console.error(error);
    statusElement.textContent = error.message;
    statusElement.classList.add('error');
    statusElement.hidden = false;
}

async function start() {
    readSettings();

    let renderer;
    try {
        renderer = await Renderer.create(canvas);
    } catch (error) {
        showError(error);
        return;
    }
    statusElement.hidden = true;

    let needsRender = true;
    let lastMoveTime = -Infinity;
    let showingLowRes = false;
    let lastFrameTime = performance.now();
    const renderTimes = [];

    const cameraMoved = () => {
        lastMoveTime = performance.now();
        needsRender = true;
    };

    // Positions of the randomly placed objects. These only change when asked, not whenever
    // the scene is rebuilt, so objects don't jump around (or trigger shader recompiles).
    let layout = createLayout();

    renderer.setScene(buildScene(settings, layout));
    camera.attach(canvas, cameraMoved);
    flight.attach();

    controls.addEventListener('input', () => {
        readSettings();
        renderer.setScene(buildScene(settings, layout));
        needsRender = true;
    });
    document.getElementById('randomize').addEventListener('click', () => {
        layout = createLayout();
        renderer.setScene(buildScene(settings, layout));
        needsRender = true;
    });
    document.getElementById('reset-view').addEventListener('click', () => {
        camera.reset();
        cameraMoved();
    });

    const frame = (now) => {
        const dt = Math.min(0.1, (now - lastFrameTime) / 1000);
        lastFrameTime = now;

        if (settings.autoRotate) {
            camera.orbit(-AUTO_ROTATE_SPEED * dt, 0);
            cameraMoved();
        }
        if (flight.update(dt)) {
            cameraMoved();
        }

        // Render at reduced resolution while the camera moves, then once more at full
        // resolution when it settles.
        const moving = now - lastMoveTime < SETTLE_DELAY_MS;
        const lowRes = moving && settings.lowResWhileMoving;
        if (needsRender || (showingLowRes && !lowRes)) {
            const size = lowRes ? Math.round(settings.resolution / 2) : settings.resolution;
            renderer.render(size, size, camera.basis, settings);
            showingLowRes = lowRes;
            needsRender = false;
            renderTimes.push(now);
        }

        updateStats(renderer, renderTimes, now);
        requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
}

function updateStats(renderer, renderTimes, now) {
    // Frames rendered in the last second.
    while (renderTimes.length > 0 && now - renderTimes[0] > 1000) {
        renderTimes.shift();
    }
    const gpuMs = renderer.pollGpuTime();
    const parts = [`${canvas.width}×${canvas.height}`];
    if (gpuMs !== null) {
        parts.push(`GPU ${gpuMs.toFixed(1)} ms`);
    }
    // Frames are only rendered when something changes, so only show a frame rate while animating.
    if (renderTimes.length > 1) {
        parts.push(`${renderTimes.length} fps`);
    }
    const inside = camera.distance < HORIZON_RADIUS ? ' (inside horizon)' : '';
    const observer = `distance ${camera.distance.toFixed(2)}${inside} · speed ${camera.speed.toFixed(1)}`;
    statsElement.textContent = `${parts.join(' · ')}
${observer}`;
}

start();
