using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BlackHoleRaytracer
{
    public class SchwarzschildRayProcessor
    {
        private int width;
        private int height;

        private Scene scene;

        private int[] outputBitmap;
        private string outputFileName;

        private const int NumIterations = 10000;

        private bool debug;


        public SchwarzschildRayProcessor(int width, int height, Scene scene, string outputFileName)
            : this(width, height, scene, outputFileName, true)
        { }

        public SchwarzschildRayProcessor(int width, int height, Scene scene, string outputFileName, bool debug)
        {
            this.width = width;
            this.height = height;
            this.scene = scene;
            this.outputFileName = outputFileName;
            this.debug = debug;
        }

        public void Process()
        {
            // Create main bitmap for writing pixels
            int bufferLength = width * height;
            outputBitmap = new int[bufferLength];

            int numThreads = Math.Max(1, Environment.ProcessorCount - 3);
            DateTime startTime = DateTime.Now;

            Log("Launching {0} threads...", numThreads);

            // Pre-compute loop-invariant camera and projection values
            float tanFov = (float)Math.Tan((Math.PI / 180.0) * scene.Fov);
            float invWidth = 1f / width;
            float invHeight = 1f / height;
            float aspectRatio = (float)height / width;

            var front = Vector3.Normalize(scene.CameraLookAt - scene.CameraPosition);
            var left = Vector3.Normalize(Vector3.Cross(scene.UpVector, front));
            var nUp = Vector3.Cross(front, left);

            var viewMatrix = new Matrix4x4(left.X, left.Y, left.Z, 0,
                nUp.X, nUp.Y, nUp.Z, 0,
                front.X, front.Y, front.Z, 0,
                0, 0, 0, 0);

            // Cache hitables as an array for index-based iteration (avoids enumerator allocations)
            IHitable[] hitables = [.. scene.hitables];
            int hitableCount = hitables.Length;

            Vector3 cameraPosition = scene.CameraPosition;
            double cameraPosSqrNorm = cameraPosition.LengthSquared();

            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = numThreads }, () =>
            {
                // Each thread gets its own equation instance (not thread-safe)
                return new SchwarzschildBlackHoleEquation(scene.SchwarzschildEquation);
            },
            (y, state, equation) =>
            {
                int yOffset = y * width;
                float yViewComponent = (-(float)y * invHeight + 0.5f) * aspectRatio * tanFov;

                for (int x = 0; x < width; x++)
                {
                    Color color = Color.Transparent;

                    var view = new Vector3((x * invWidth - 0.5f) * tanFov, yViewComponent, 1f);
                    view = Vector3.Transform(view, viewMatrix);

                    var velocity = Vector3.Normalize(view);

                    Vector3 point = cameraPosition;
                    double sqrNorm = cameraPosSqrNorm;

                    equation.SetInitialConditions(ref point, ref velocity);

                    bool stop = false;
                    for (int iter = 0; iter < NumIterations; iter++)
                    {
                        Vector3 prevPoint = point;
                        double prevSqrNorm = sqrNorm;

                        sqrNorm = equation.Function(ref point, ref velocity);

                        // Check if the ray hits anything
                        for (int h = 0; h < hitableCount; h++)
                        {
                            if (hitables[h].Hit(ref point, sqrNorm, ref prevPoint, prevSqrNorm, ref velocity, equation, ref color, ref stop, false))
                            {
                                if (stop)
                                {
                                    break;
                                }
                            }
                        }
                        if (stop)
                        {
                            break;
                        }
                    }

                    outputBitmap[yOffset + x] = color.ToArgb();
                }
                //Log("Line {0} rendered.", y);

                return equation;
            },
            _ => { });


            GCHandle gcHandle = GCHandle.Alloc(outputBitmap, GCHandleType.Pinned);
            Bitmap resultBmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject());
            resultBmp.Save(outputFileName, ImageFormat.Png);
            if (resultBmp != null) { resultBmp.Dispose(); resultBmp = null; }
            if (gcHandle.IsAllocated) { gcHandle.Free(); }


            Log("Finished in {0} seconds.", (DateTime.Now - startTime).TotalSeconds);
        }

        private void Log(string message, object arg0 = null, object arg1 = null)
        {
            if (debug)
            {
                Console.WriteLine(message, arg0, arg1);
            }
        }
    }
}
