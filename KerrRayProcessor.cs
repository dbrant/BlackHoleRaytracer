using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using BlackHoleRaytracer.Equation;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    class KerrRayProcessor
    {
        private int width;
        private int height;

        private Scene scene;

        private int[] outputBitmap;
        private string outputFileName;
        

        public KerrRayProcessor(int width, int height, Scene scene, string outputFileName)
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

            int numThreads = 4; // Environment.ProcessorCount;
            DateTime startTime = DateTime.Now;

            Console.WriteLine("Launching {0} threads...", numThreads);

            var lineLists = new List<List<int>>();
            var paramList = new List<KerrThreadParams>();
            
            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new KerrThreadParams()
                {
                    JobId = i,
                    RayTracer = new KerrRayTracer(
                            new KerrBlackHoleEquation(scene.KerrEquation),
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
            var param = (KerrThreadParams)threadParams;
            Console.WriteLine("Starting thread {0}...", param.JobId);
            
            var random = new Random();
            int x, yOffset;

            try
            {
                foreach (int y in param.LinesList) {
                    yOffset = (height - y - 1) * width;
                    for (x = 0; x < width; x++)
                    {
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

    class KerrThreadParams
    {
        public int JobId { get; set; }
        public KerrRayTracer RayTracer { get; set; }
        public List<int> LinesList { get; set; }
        public Thread Thread { get; set; }
    }
}
