using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using BlackHoleRaytracer.Equation;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    class RayProcessor
    {
        private int width;
        private int height;

        private Scene scene;

        private int[] outputBitmap;
        private string outputFileName;
        

        public RayProcessor(int width, int height, Scene scene, string outputFileName)
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

            List<List<int>> lineLists = new List<List<int>>();
            List<RayTracerThreadParams> paramList = new List<RayTracerThreadParams>();
            
            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new RayTracerThreadParams()
                {
                    JobId = i,
                    RayTracer = new RayTracer(
                            new KerrBlackHoleEquation(scene.equation),
                            width, height, scene.hitables,
                            scene.CameraTilt, scene.CameraYaw),
                    LinesList = lineList,
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
                    yOffset = (height - y - 1) * width;
                    for (x = 0; x < width; x++)
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
