using BlackHoleRaytracer.Hitable;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer.Gpu
{
    /// <summary>
    /// GPU implementation of SchwarzschildRayProcessor, using ILGPU (CUDA, OpenCL, or
    /// CPU fallback). The kernel mirrors the CPU tracer and the Schwarzschild Hit()
    /// logic of each hitable.
    ///
    /// Create one instance and reuse it across frames: initializing the device and
    /// compiling the kernel takes a moment, and textures stay resident on the GPU as
    /// long as the same hitables list is rendered.
    /// </summary>
    public sealed class GpuSchwarzschildRayProcessor : IDisposable
    {
        private const int NumIterations = 10000;

        // Render the image in horizontal bands, so that no single kernel launch runs long
        // enough to trigger the Windows GPU watchdog (TDR) on a display GPU.
        private const int PixelsPerLaunch = 1 << 20;

        private readonly Context context;
        private readonly Accelerator accelerator;
        private readonly Action<Index2D, ArrayView<int>, ArrayView<GpuHitable>, ArrayView<int>, RenderParams> kernel;
        private readonly bool debug;

        private List<IHitable> cachedHitables;
        private MemoryBuffer1D<GpuHitable, Stride1D.Dense> hitableBuffer;
        private MemoryBuffer1D<int, Stride1D.Dense> textureBuffer;
        private MemoryBuffer1D<int, Stride1D.Dense> outputBuffer;

        public GpuSchwarzschildRayProcessor(bool debug = true, bool preferCpu = false)
        {
            this.debug = debug;
            context = Context.Create(builder => builder.Default().EnableAlgorithms());
            accelerator = context.GetPreferredDevice(preferCpu).CreateAccelerator(context);
            Log("Using device: {0}", accelerator.Name);

            var stopwatch = Stopwatch.StartNew();
            kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<int>, ArrayView<GpuHitable>, ArrayView<int>, RenderParams>(RenderKernel);
            Log("Kernel compiled in {0} seconds.", stopwatch.Elapsed.TotalSeconds);
        }

        public void Process(int width, int height, Scene scene, string outputFileName)
        {
            var stopwatch = Stopwatch.StartNew();

            UploadScene(scene.hitables);

            int bufferLength = width * height;
            if (outputBuffer == null || outputBuffer.Length != bufferLength)
            {
                outputBuffer?.Dispose();
                outputBuffer = accelerator.Allocate1D<int>(bufferLength);
            }

            var front = Vector3.Normalize(scene.CameraLookAt - scene.CameraPosition);
            var left = Vector3.Normalize(Vector3.Cross(scene.UpVector, front));
            var nUp = Vector3.Cross(front, left);

            var equation = scene.SchwarzschildEquation;
            var parameters = new RenderParams
            {
                Width = width,
                TanFov = (float)Math.Tan((Math.PI / 180.0) * scene.Fov),
                InvWidth = 1f / width,
                InvHeight = 1f / height,
                AspectRatio = (float)height / width,
                Left = left,
                Up = nUp,
                Front = front,
                CameraPosition = scene.CameraPosition,
                CameraSqrNorm = scene.CameraPosition.LengthSquared(),
                PotentialCoefficient = equation.PotentialCoefficient,
                StepSize = equation.StepSize,
                StepSizeOver30 = equation.StepSize / 30f
            };

            int rowsPerLaunch = Math.Max(1, PixelsPerLaunch / width);
            for (int row = 0; row < height; row += rowsPerLaunch)
            {
                parameters.RowOffset = row;
                kernel(new Index2D(width, Math.Min(rowsPerLaunch, height - row)),
                    outputBuffer.View, hitableBuffer.View, textureBuffer.View, parameters);
            }
            accelerator.Synchronize();
            double renderSeconds = stopwatch.Elapsed.TotalSeconds;

            int[] outputBitmap = outputBuffer.GetAsArray1D();

            GCHandle gcHandle = GCHandle.Alloc(outputBitmap, GCHandleType.Pinned);
            try
            {
                using var resultBmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject());
                resultBmp.Save(outputFileName, ImageFormat.Png);
            }
            finally
            {
                gcHandle.Free();
            }

            Log("Finished in {0} seconds (render: {1} seconds).", stopwatch.Elapsed.TotalSeconds, renderSeconds);
        }

        private void UploadScene(List<IHitable> hitables)
        {
            // Textures can be large (the 8k sky is 128MB), so only re-upload when the list changes.
            if (ReferenceEquals(hitables, cachedHitables) && hitableBuffer.Length == hitables.Count)
            {
                return;
            }
            var (gpuHitables, textures) = GpuSceneBuilder.Build(hitables);

            hitableBuffer?.Dispose();
            textureBuffer?.Dispose();
            // Never allocate a zero-length buffer, even for an empty scene.
            hitableBuffer = accelerator.Allocate1D<GpuHitable>(Math.Max(1, gpuHitables.Length));
            hitableBuffer.View.SubView(0, gpuHitables.Length).CopyFromCPU(gpuHitables);
            textureBuffer = accelerator.Allocate1D(textures);
            cachedHitables = hitables;
        }

        public void Dispose()
        {
            outputBuffer?.Dispose();
            hitableBuffer?.Dispose();
            textureBuffer?.Dispose();
            accelerator.Dispose();
            context.Dispose();
        }

        private void Log(string message, object arg0 = null, object arg1 = null)
        {
            if (debug)
            {
                Console.WriteLine(message, arg0, arg1);
            }
        }

        #region Kernel

        // Color.Transparent.ToArgb(), which is what the CPU tracer outputs when a ray hits nothing.
        private const int TransparentArgb = 0x00FFFFFF;
        private const int BlackArgb = unchecked((int)0xFF000000);
        private const int GreenArgb = unchecked((int)0xFF008000);
        private const int ReflectionArgb = unchecked((int)0xFF080808);

        private const double TwoPi = 2 * Math.PI;

        public struct RenderParams
        {
            public int Width;
            public int RowOffset;
            public float TanFov;
            public float InvWidth;
            public float InvHeight;
            public float AspectRatio;
            public Float3 Left;
            public Float3 Up;
            public Float3 Front;
            public Float3 CameraPosition;
            public float CameraSqrNorm;
            public float PotentialCoefficient;
            public float StepSize;
            public float StepSizeOver30;
        }

        private static void RenderKernel(Index2D index, ArrayView<int> output, ArrayView<GpuHitable> hitables, ArrayView<int> textures, RenderParams p)
        {
            int x = index.X;
            int y = index.Y + p.RowOffset;
            int hitableCount = hitables.IntLength;

            float yViewComponent = (-(float)y * p.InvHeight + 0.5f) * p.AspectRatio * p.TanFov;
            float xViewComponent = (x * p.InvWidth - 0.5f) * p.TanFov;
            var view = p.Left * xViewComponent + p.Up * yViewComponent + p.Front;

            var velocity = Float3.Normalize(view);
            var point = p.CameraPosition;
            float sqrNorm = p.CameraSqrNorm;
            float potH2 = p.PotentialCoefficient * Float3.Cross(point, velocity).LengthSquared();

            int color = TransparentArgb;
            bool hasColor = false;
            bool stop = false;

            for (int iter = 0; iter < NumIterations && !stop; iter++)
            {
                var prevPoint = point;
                float prevSqrNorm = sqrNorm;

                sqrNorm = Step(ref point, ref velocity, potH2, XMath.Sqrt(point.LengthSquared()) * p.StepSizeOver30);

                for (int h = 0; h < hitableCount && !stop; h++)
                {
                    ref GpuHitable hitable = ref hitables[h];
                    switch (hitable.Kind)
                    {
                        case GpuHitableKind.Sky:
                            // Has the ray escaped to infinity?
                            if (sqrNorm > hitable.OuterRadiusSqr)
                            {
                                ToSpherical(point.X, point.Y, point.Z, out double r, out double theta, out double phi);
                                int c = hitable.Texture.IsValid ? SampleSpherical(textures, hitable.Texture, theta, phi) : BlackArgb;
                                color = AddColor(c, color, hasColor);
                                hasColor = true;
                                stop = true;
                            }
                            break;

                        case GpuHitableKind.Horizon:
                            // Has the ray fallen past the horizon?
                            if (prevSqrNorm > 1 && sqrNorm < 1)
                            {
                                var colpoint = IntersectionSearch(ref hitable, 0, prevPoint, velocity, potH2, p.StepSize);
                                ToSpherical(colpoint.X, colpoint.Z, colpoint.Y, out double r, out double theta, out double phi);

                                int c = BlackArgb;
                                if (hitable.Checkered)
                                {
                                    if ((DoubleMod(theta, 1.04719) < 0.52359) ^ (DoubleMod(phi, 1.04719) < 0.52359))
                                    {
                                        c = GreenArgb;
                                    }
                                }
                                else if (hitable.Texture.IsValid)
                                {
                                    c = SampleSpherical(textures, hitable.Texture, theta, -phi);
                                }
                                color = AddColor(c, color, hasColor);
                                hasColor = true;
                                stop = true;
                            }
                            break;

                        case GpuHitableKind.Disk:
                            {
                                // Did we cross the horizontal plane?
                                // Note: side must be a float. ILGPU's CUDA backend miscompiles this
                                // expression as an int (yielding 1 instead of -1) when it is later
                                // converted to float.
                                float side = prevPoint.Y > 0 ? -1f : prevPoint.Y < 0 ? 1f : 0f;
                                if (point.Y * side >= 0)
                                {
                                    var colpoint = IntersectionSearch(ref hitable, side, prevPoint, velocity, potH2, p.StepSize);
                                    double colpointSqr = colpoint.LengthSquared();
                                    if (colpointSqr >= hitable.InnerRadius * hitable.InnerRadius
                                        && colpointSqr <= hitable.OuterRadius * hitable.OuterRadius)
                                    {
                                        ToSpherical(colpoint.X, colpoint.Y, colpoint.Z, out double r, out double theta, out double phi);
                                        // Note: the disk's texture angle is theta, not phi (matching Disk.Hit).
                                        color = AddColor(GetDiskColor(ref hitable, textures, side, r, theta + Math.PI / 12), color, hasColor);
                                        hasColor = true;
                                    }
                                }
                            }
                            break;

                        case GpuHitableKind.Sphere:
                            if ((point - hitable.Center).LengthSquared() < hitable.RadiusSqr)
                            {
                                var colpoint = IntersectionSearch(ref hitable, 0, prevPoint, velocity, potH2, p.StepSize);
                                var impactFromCenter = Float3.Normalize(hitable.Center - colpoint);
                                ToSpherical(impactFromCenter.X, impactFromCenter.Z, -impactFromCenter.Y, out double r, out double theta, out double phi);

                                int c;
                                if (hitable.Texture.IsValid)
                                {
                                    c = SampleSpherical(textures, hitable.Texture, theta, phi + hitable.TextureOffset);
                                }
                                else
                                {
                                    c = (DoubleMod(theta, 1.04719) < 0.52359) ^ (DoubleMod(phi, 1.04719) < 0.52359) ? hitable.Color1 : hitable.Color2;
                                }
                                color = AddColor(c, color, hasColor);
                                hasColor = true;
                                stop = true;
                            }
                            break;

                        case GpuHitableKind.ReflectiveSphere:
                            if ((point - hitable.Center).LengthSquared() < hitable.RadiusSqr)
                            {
                                point = IntersectionSearch(ref hitable, 0, prevPoint, velocity, potH2, p.StepSize);
                                velocity = Float3.Reflect(velocity, Float3.Normalize(point - hitable.Center));
                                color = AddColor(ReflectionArgb, color, hasColor);
                                hasColor = true;
                            }
                            break;
                    }
                }
            }

            output[y * p.Width + x] = color;
        }

        /// <summary>
        /// One step of the Schwarzschild geodesic equation (see SchwarzschildBlackHoleEquation).
        /// </summary>
        private static float Step(ref Float3 point, ref Float3 velocity, float potH2, float step)
        {
            point += velocity * step;

            float lsq = point.LengthSquared();
            float sqrtLsq = XMath.Sqrt(lsq);
            float f1 = potH2 * step / (lsq * lsq * sqrtLsq);
            velocity += point * f1;

            return lsq;
        }

        /// <summary>
        /// Binary search for the step size at which the ray, starting at prevPoint, enters the given hitable.
        /// </summary>
        private static Float3 IntersectionSearch(ref GpuHitable hitable, float side, Float3 prevPoint, Float3 velocity, float potH2, float stepSize)
        {
            float stepLow = 0, stepHigh = stepSize;
            Float3 newPoint;
            while (true)
            {
                float stepMid = (stepLow + stepHigh) / 2;
                newPoint = prevPoint;
                var tempVelocity = velocity;
                float lsq = Step(ref newPoint, ref tempVelocity, potH2, stepMid);

                if (XMath.Abs(stepHigh - stepLow) < 0.00001f)
                {
                    break;
                }

                bool inside = hitable.Kind switch
                {
                    GpuHitableKind.Disk => side * newPoint.Y > 0,
                    GpuHitableKind.Horizon => lsq < 1,
                    _ => (newPoint - hitable.Center).LengthSquared() < hitable.RadiusSqr
                };
                if (inside)
                {
                    stepHigh = stepMid;
                }
                else
                {
                    stepLow = stepMid;
                }
            }
            return newPoint;
        }

        private static int GetDiskColor(ref GpuHitable hitable, ArrayView<int> textures, float side, double r, double phi)
        {
            if (hitable.Texture.IsValid)
            {
                // Same as DiscMapping.
                var texture = hitable.Texture;
                int x = (int)(phi / TwoPi * texture.Width) % texture.Width;
                if (x < 0) { x += texture.Width; }
                int y = (int)((r - hitable.InnerRadius) / (hitable.OuterRadius - hitable.InnerRadius) * texture.Height);
                y = XMath.Clamp(y, 0, texture.Height - 1);
                return textures[texture.Offset + y * texture.Width + x];
            }
            bool check = DoubleMod(phi, 1.04719) < 0.52359;
            return side < 0 ? (check ? hitable.Color1 : hitable.Color2) : (check ? hitable.Color3 : hitable.Color4);
        }

        /// <summary>
        /// Same as SphericalMapping.Map, followed by a texture lookup.
        /// </summary>
        private static int SampleSpherical(ArrayView<int> textures, GpuTexture texture, double theta, double phi)
        {
            int x = (int)((phi / TwoPi) * texture.Width) % texture.Width;
            int y = (int)((theta / Math.PI) * texture.Height) % texture.Height;
            if (x < 0) { x += texture.Width; }
            if (y < 0) { y += texture.Height; }
            return textures[texture.Offset + y * texture.Width + x];
        }

        /// <summary>
        /// Same as Util.ToSpherical, but in single precision: consumer GPUs have very slow
        /// double precision, and double Atan2/Acos made rendering ~3x slower.
        /// </summary>
        private static void ToSpherical(float x, float y, float z, out double r, out double theta, out double phi)
        {
            float fr = XMath.Sqrt(x * x + y * y + z * z);
            r = fr;
            phi = XMath.Atan2(y, x);
            theta = XMath.Acos(z / fr);
        }

        private static double DoubleMod(double n, double m)
        {
            return n - (m * XMath.Floor(n / m));
        }

        /// <summary>
        /// Same as Util.AddColor, operating on ARGB values. When hasTint is false, the
        /// tint is Color.Transparent, and the hit color is returned unchanged.
        /// </summary>
        private static int AddColor(int hitColor, int tintColor, bool hasTint)
        {
            if (!hasTint)
            {
                return hitColor;
            }
            int tintR = (tintColor >> 16) & 0xFF, tintG = (tintColor >> 8) & 0xFF, tintB = tintColor & 0xFF;
            int max = XMath.Max(XMath.Max(tintR, tintG), tintB);
            int min = XMath.Min(XMath.Min(tintR, tintG), tintB);
            float darkness = 1 - (max + min) / (255 * 2f);

            int r = XMath.Min((int)(darkness * ((hitColor >> 16) & 0xFF)) + tintR * 255 / 205, 255);
            int g = XMath.Min((int)(darkness * ((hitColor >> 8) & 0xFF)) + tintG * 255 / 205, 255);
            int b = XMath.Min((int)(darkness * (hitColor & 0xFF)) + tintB * 255 / 205, 255);
            return BlackArgb | (r << 16) | (g << 8) | b;
        }

        #endregion
    }
}
