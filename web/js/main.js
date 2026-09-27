import { OrbitCamera } from './camera.js';
import { Renderer } from './renderer.js';
import { buildScene, CAMERA_MAX_DISTANCE, CAMERA_MIN_DISTANCE } from './scene.js';

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

// Same starting position as the C# Program.cs.
const camera = OrbitCamera.fromPosition([0, 5, -35], {
    minDistance: CAMERA_MIN_DISTANCE,
    maxDistance: CAMERA_MAX_DISTANCE,
});

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
        checkeredSphere: value('checkered-sphere').checked,
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

    renderer.setScene(buildScene(settings));
    camera.attach(canvas, cameraMoved);

    controls.addEventListener('input', () => {
        readSettings();
        renderer.setScene(buildScene(settings));
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
            camera.rotate(AUTO_ROTATE_SPEED * dt, 0);
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
    statsElement.textContent = parts.join(' · ');
}

start();
