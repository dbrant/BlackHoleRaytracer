using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using BlackHoleRaytracer.Equation;
using System.Runtime.InteropServices;
using BlackHoleRaytracer.Hitable;

namespace BlackHoleRaytracer
{
    class RayProcessor
    {
        int sizex; 
        int sizey; 

        Scene scene;

        private int[] outputBitmap;
        private string outputFileName;
        

        public RayProcessor(int sizex, int sizey, Scene scene, string outputFileName)
        {
            this.sizex = sizex;
            this.sizey = sizey;
            this.scene = scene;
            this.outputFileName = outputFileName;
        }
        
        public void Process()
        {

            // Create main bitmap for writing pixels
            int bufferLength = sizex * sizey;
            outputBitmap = new int[bufferLength];
            

            int numThreads = Environment.ProcessorCount;
            DateTime startTime = DateTime.Now;

            Console.WriteLine("Launching {0} threads...", numThreads);

            List<List<int>> lineLists = new List<List<int>>();
            List<RayTracerThreadParams> paramList = new List<RayTracerThreadParams>();


            var equation = new KerrBlackHoleEquation(scene.CameraDistance, scene.CameraInclination, scene.CameraAngle);

            List<IHitable> hitables = new List<IHitable>();
            hitables.Add(new Disk(equation.Rmstable, 20.0, new Bitmap("adisk.jpg"), true));
            hitables.Add(new Horizon(true));
            hitables.Add(new Sky(new Bitmap("sky_16k.jpg")));
            hitables.Add(new Sphere(12, 7, 3, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true));
            hitables.Add(new Sphere(-10, -10, -10, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true));


            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new RayTracerThreadParams()
                {
                    JobId = i,
                    RayTracer = new RayTracer(
                            new KerrBlackHoleEquation(equation),
                            sizex, sizey, hitables,
                            scene.CameraTilt, scene.CameraYaw),
                    LinesList = lineList,
                    Thread = new Thread(new ParameterizedThreadStart(RayTraceThread)),
                });

            }
            

            for (int j = 0; j < sizey; j++)
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
            Bitmap resultBmp = new Bitmap(sizex, sizey, sizex * 4, PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject());
            resultBmp.Save(outputFileName, ImageFormat.Png);
            if (resultBmp != null) { resultBmp.Dispose(); resultBmp = null; }
            if (gcHandle.IsAllocated) { gcHandle.Free(); }


            Console.WriteLine("Finished in {0} seconds.", (DateTime.Now - startTime).TotalSeconds);
        }
        

        public void RayTraceThread(object threadParams)
        {
            var param = (RayTracerThreadParams)threadParams;
            Console.WriteLine("Starting thread {0}...", param.JobId);
            
            var random = new Random();
            int x, yOffset;
            int sample;
            int numSamples = 10;
            int r, g, b;

            try
            {
                foreach (int y in param.LinesList) {
                    yOffset = (sizey - y - 1) * sizex;
                    for (x = 0; x < sizex; x++)
                    {
                        

                        /*
                        r = g = b = 0;
                        for (sample = 0; sample < numSamples; sample++)
                        {
                            Color p = param.RayTracer.Calculate(x + random.NextDouble(), y + random.NextDouble());
                            r += p.R; g += p.G; b += p.B;
                        }
                        Color pixel = Color.FromArgb(r / numSamples, g / numSamples, b / numSamples);
                        */


                        Color pixel = param.RayTracer.Calculate(x, y);
                        
                        
                        outputBitmap[yOffset + x] = pixel.ToArgb();

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

    class RayTracerThreadParams
    {
        public int JobId { get; set; }
        public RayTracer RayTracer { get; set; }
        public List<int> LinesList { get; set; }
        public Thread Thread { get; set; }
    }
}
