# BlackHoleRaytracer
Raytracing a black hole (and surrounding stuff) in C#.

## Web version

The `web` folder contains an interactive, real-time version of the Schwarzschild raytracer that runs in the browser, using a WebGL 2 fragment shader ported from the C# code. Drag to orbit the black hole, scroll to zoom, and adjust the curvature and objects in the scene.

It needs to be served over HTTP (browsers don't allow loading shaders and textures from `file://` pages). For example:

```
cd web
python -m http.server 8000
```

Then open http://localhost:8000.
