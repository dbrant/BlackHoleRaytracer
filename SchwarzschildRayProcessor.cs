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

            for (int i = 0; i < numThreads; i++)
            {
                var lineList = new List<int>();
                lineLists.Add(lineList);
                paramList.Add(new ThreadParams()
                {
                    JobId = i,
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
            var param = (ThreadParams)threadParams;
            Console.WriteLine("Starting thread {0}...", param.JobId);




            var lookAt = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);
            var cameraPos = new Vector3(20, 0, 0);


            var front = Vector3.Normalize(lookAt - cameraPos);
            //FRONTVEC = FRONTVEC / np.linalg.norm(FRONTVEC)

            var left = Vector3.Normalize(Vector3.Cross(up, front));
            //LEFTVEC = LEFTVEC / np.linalg.norm(LEFTVEC)

            var nUp = Vector3.Cross(front, left);


            /*
viewMatrix = np.zeros((3, 3))

viewMatrix[:, 0] = LEFTVEC
viewMatrix[:, 1] = NUPVEC
viewMatrix[:, 2] = FRONTVEC
            */






















            Color pixel;
            int x, yOffset;

            try
            {
                foreach (int y in param.LinesList)
                {
                    yOffset = (height - y - 1) * width;
                    for (x = 0; x < width; x++)
                    {








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

    class ThreadParams
    {
        public int JobId { get; set; }
        public List<int> LinesList { get; set; }
        public Thread Thread { get; set; }
    }
}
