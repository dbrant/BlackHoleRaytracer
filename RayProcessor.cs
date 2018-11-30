using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using BlackHoleRaytracer.Equation;
using System.IO;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    class RayProcessor
    {
        int sizex; 
        int sizey; 

        int frame;
        Scene sceneDescription;
        

        private int[] outputBitmap;

        private static Bitmap skyImage;
        private static int[] skyBitmap;

        private static Bitmap diskImage;
        private static int[] diskBitmap;
        
        public string OutputPath { get; private set; }
        

        public RayProcessor(int sizex, int sizey, Scene scene, int frame, string outputPath)
        {
            this.sizex = sizex; 
            this.sizey = sizey; 
            this.frame = frame;
            sceneDescription = scene;
            this.OutputPath = outputPath;
        }
        
        public void Process()
        {

            // Create main bitmap for writing pixels
            int bufferLength = sizex * sizey;
            outputBitmap = new int[bufferLength];


            // Load textures for sky and accretion disk

            if (skyBitmap == null)
            {
                //Bitmap skyImage = new Bitmap("bgedit.jpg");
                skyImage = new Bitmap("sky_16k.jpg");
                skyBitmap = new int[skyImage.Width * skyImage.Height];
                BitmapData bmpBits = skyImage.LockBits(new Rectangle(0, 0, skyImage.Width, skyImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bmpBits.Scan0, skyBitmap, 0, skyBitmap.Length);
                skyImage.UnlockBits(bmpBits);
            }

            if (diskBitmap == null)
            {
                diskImage = new Bitmap("adisk.jpg");
                diskBitmap = new int[diskImage.Width * diskImage.Height];
                BitmapData diskBits = diskImage.LockBits(new Rectangle(0, 0, diskImage.Width, diskImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(diskBits.Scan0, diskBitmap, 0, diskBitmap.Length);
                diskImage.UnlockBits(diskBits);
            }



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
                            new KerrBlackHoleEquation(sceneDescription.CameraDistance, sceneDescription.CameraInclination, sceneDescription.CameraAngle, 20.0, sceneDescription.CameraAperture),
                            sizex, sizey, diskBitmap, skyBitmap, diskImage, skyImage,
                            sceneDescription.CameraTilt, sceneDescription.CameraYaw),
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
            resultBmp.Save(Path.Combine(OutputPath, String.Format("render_{0:00000}.png", frame)), ImageFormat.Png);
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
