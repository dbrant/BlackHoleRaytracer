using BlackHoleRaytracer.Equation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

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

            int numThreads = Environment.ProcessorCount - 3;
            DateTime startTime = DateTime.Now;

            Log("Launching {0} threads...", numThreads);

            var lineLists = new List<List<int>>();
            var paramList = new List<ThreadParams>();
            

            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new ThreadParams()
                {
                    JobId = i,
                    LinesList = lineList,
                    Equation = new SchwarzschildBlackHoleEquation(scene.SchwarzschildEquation),
                    Thread = new Thread(new ParameterizedThreadStart(RayTraceThread)),
                });

            }


            for (int j = 0; j < height; j++)
            {
                lineLists[j % numThreads].Add(j);
            }

            foreach (var param in paramList)
            {
                param.Thread.Start(param);
            }
            foreach (var param in paramList)
            {
                param.Thread.Join();
            }


            GCHandle gcHandle = GCHandle.Alloc(outputBitmap, GCHandleType.Pinned);
            Bitmap resultBmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject());
            resultBmp.Save(outputFileName, ImageFormat.Png);
            if (resultBmp != null) { resultBmp.Dispose(); resultBmp = null; }
            if (gcHandle.IsAllocated) { gcHandle.Free(); }


            Log("Finished in {0} seconds.", (DateTime.Now - startTime).TotalSeconds);
        }


        public void RayTraceThread(object threadParams)
        {
            var param = (ThreadParams)threadParams;
            Log("Starting thread {0}...", param.JobId);

            float tanFov = (float)Math.Tan((Math.PI / 180.0) * scene.Fov);
            
            var front = Vector3.Normalize(scene.CameraLookAt - scene.CameraPosition);
            var left = Vector3.Normalize(Vector3.Cross(scene.UpVector, front));
            var nUp = Vector3.Cross(front, left);
            
            var viewMatrix = new Matrix4x4(left.X, left.Y, left.Z, 0,
                nUp.X, nUp.Y, nUp.Z, 0,
                front.X, front.Y, front.Z, 0,
                0, 0, 0, 0);

            
            bool debug = false;
            Color color;
            int x, yOffset;
            Vector3 point, prevPoint;
            double sqrNorm, prevSqrNorm;
            bool stop = false;

            try
            {
                foreach (int y in param.LinesList)
                {
                    yOffset = y * width;
                    for (x = 0; x < width; x++)
                    {
                        color = Color.Transparent;

                        var view = new Vector3(((float)x / width - 0.5f) * tanFov,
                            ((-(float)y / height + 0.5f) * height / width) * tanFov,
                            1f);
                        view = Vector3.Transform(view, viewMatrix);
                        
                        var velocity = Vector3.Normalize(view);

                        point = scene.CameraPosition;
                        sqrNorm = point.LengthSquared();

                        param.Equation.SetInitialConditions(ref point, ref velocity);

                        for (int iter = 0; iter < NumIterations; iter++)
                        {
                            prevPoint = point;
                            prevSqrNorm = sqrNorm;

                            sqrNorm = param.Equation.Function(ref point, ref velocity);

                            // Check if the ray hits anything
                            foreach (var hitable in scene.hitables)
                            {
                                stop = false;
                                if (hitable.Hit(ref point, sqrNorm, ref prevPoint, prevSqrNorm, ref velocity, param.Equation, ref color, ref stop, debug))
                                {
                                    if (stop)
                                    {
                                        // The ray has found its stopping point (or rather its starting point).
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
                    //Log("Thread {0}: Line {1} rendered.", param.JobId, y);
                }
            }
            catch (Exception e)
            {
                Log("Thread {0} error: {1}", param.JobId, e.Message);
            }
            Log("Thread {0} finished.", param.JobId);
        }

        private void Log(string message, object arg0)
        {
            if (debug)
            {
                Console.WriteLine(message, arg0);
            }
        }

        private void Log(string message, object arg0, object arg1)
        {
            if (debug)
            {
                Console.WriteLine(message, arg0, arg1);
            }
        }
    }

    class ThreadParams
    {
        public int JobId { get; set; }
        public List<int> LinesList { get; set; }
        public SchwarzschildBlackHoleEquation Equation { get; set; }
        public Thread Thread { get; set; }
    }
}
