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


        public SchwarzschildRayProcessor(int width, int height, Scene scene, string outputFileName)
        {
            this.width = width;
            this.height = height;
            this.scene = scene;
            this.outputFileName = outputFileName;
        }

        public void Process()
        {

            // Create main bitmap for writing pixels
            int bufferLength = width * height;
            outputBitmap = new int[bufferLength];
            
            int numThreads = Environment.ProcessorCount;
            DateTime startTime = DateTime.Now;

            Console.WriteLine("Launching {0} threads...", numThreads);

            var lineLists = new List<List<int>>();
            var paramList = new List<ThreadParams>();


            float stepSize = 0.16f;


            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new ThreadParams()
                {
                    JobId = i,
                    LinesList = lineList,
                    Equation = new SchwarzschildBlackHoleEquation(stepSize),
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


            Console.WriteLine("Finished in {0} seconds.", (DateTime.Now - startTime).TotalSeconds);
        }


        public void RayTraceThread(object threadParams)
        {
            var param = (ThreadParams)threadParams;
            Console.WriteLine("Starting thread {0}...", param.JobId);
            

            float tanFov = 1.5f;
            int numIterations = 2500;
            
            
            var lookAt = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);
            var cameraPos = new Vector3(0, 1, -20);


            var front = Vector3.Normalize(lookAt - cameraPos);
            var left = Vector3.Normalize(Vector3.Cross(up, front));
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
            double tempR = 0, tempTheta = 0, tempPhi = 0;
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
                        view = Util.MatrixMul(viewMatrix, view);
                        
                        var normView = Vector3.Normalize(view);

                        var velocity = new Vector3(normView.X, normView.Y, normView.Z);

                        point = cameraPos;
                        sqrNorm = Util.SqrNorm(point);
                        
                        param.Equation.SetInitialConditions(ref point, ref velocity);

                        for (int iter = 0; iter < numIterations; iter++)
                        {
                            prevPoint = point;
                            prevSqrNorm = sqrNorm;
                            
                            param.Equation.Function(ref point, ref velocity);
                            sqrNorm = Util.SqrNorm(point);
                            
                            Util.ToSpherical(point.X, point.Y, point.Z, ref tempR, ref tempTheta, ref tempPhi);
                            
                            // Check if the ray hits anything
                            foreach (var hitable in scene.hitables)
                            {
                                stop = false;
                                if (hitable.Hit(ref point, sqrNorm, prevPoint, prevSqrNorm, ref velocity, param.Equation, tempR, tempTheta, tempPhi, ref color, ref stop, debug))
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
                    Console.WriteLine("Thread {0}: Line {1} rendered.", param.JobId, y);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Thread {0} error: {1}", param.JobId, e.Message);
            }
            Console.WriteLine("Thread {0} finished.", param.JobId);
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
