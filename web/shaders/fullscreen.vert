#version 300 es

// Draws a single triangle that covers the whole viewport, without any vertex buffers.
void main() {
    vec2 position = vec2(float((gl_VertexID << 1) & 2), float(gl_VertexID & 2)) * 2.0 - 1.0;
    gl_Position = vec4(position, 0.0, 1.0);
}
