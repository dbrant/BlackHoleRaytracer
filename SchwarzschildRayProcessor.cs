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


            int numThreads = 1;// Environment.ProcessorCount;
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





            float tanFov = 1.5f;
            int numIterations = 250;
            float stepSize = 0.16f;

            double[] Y = new double[6];
            double[] F = new double[6];
            double[] K1 = new double[6];
            double[] K2 = new double[6];
            double[] K3 = new double[6];
            double[] K4 = new double[6];



            var lookAt = new Vector3(0, 0, 0);
            var up = new Vector3(0.2f, 1, 0);
            var cameraPos = new Vector3(0, 1, -20);


            var front = Vector3.Normalize(lookAt - cameraPos);
            //FRONTVEC = FRONTVEC / np.linalg.norm(FRONTVEC)

            var left = Vector3.Normalize(Vector3.Cross(up, front));
            //LEFTVEC = LEFTVEC / np.linalg.norm(LEFTVEC)

            var nUp = Vector3.Cross(front, left);


            var viewMatrix = new Matrix4x4(left.X, left.Y, left.Z, 0,
                nUp.X, nUp.Y, nUp.Z, 0,
                front.X, front.Y, front.Z, 0,
                0, 0, 0, 0);
            
            

            Color pixel = Color.Black;
            int x, yOffset;
            float y = 0, yIncrement = (float)1 / (float)width;
            double tempR = 0, tempTheta = 0, tempPhi = 0;

            try
            {
                foreach (int yCoord in param.LinesList)
                {
                    yOffset = (height - yCoord - 1) * width;
                    for (x = 0; x < width; x++)
                    {
                        var view = new Vector3(((float)x / width - 0.5f) * tanFov,
                            (((float)y / height + 0.5f) * height / width) * tanFov,
                            1f);
                        view = MatrixMul(viewMatrix, view);



                        var normView = Vector3.Normalize(view);

                        var velocity = new Vector3(normView.X, normView.Y, normView.Z);

                        var point = cameraPos;

                        
                        var h2 = SqrNorm(Vector3.Cross(point, velocity));

                        for (int iter = 0; iter < numIterations; iter++)
                        {











                            var oldPoint = point;





                            /*
                            var rkstep = stepSize;

                            Y[0] = point.X;
                            Y[1] = point.Y;
                            Y[2] = point.Z;
                            Y[3] = velocity.X;
                            Y[4] = velocity.Y;
                            Y[5] = velocity.Z;
                            RK4f(Y, K1, h2);
                            RK4fAdd(0.5 * rkstep, Y, K1, F);
                            RK4f(F, K2, h2);
                            RK4fAdd(0.5 * rkstep, Y, K2, F);
                            RK4f(F, K3, h2);
                            RK4fAdd(rkstep, Y, K3, F);
                            RK4f(F, K4, h2);

                            F[0] = rkstep / 6 * (K1[0] + 2 * K2[0] + 2 * K3[0] + K4[0]);
                            F[1] = rkstep / 6 * (K1[1] + 2 * K2[1] + 2 * K3[1] + K4[1]);
                            F[2] = rkstep / 6 * (K1[2] + 2 * K2[2] + 2 * K3[2] + K4[2]);
                            F[3] = rkstep / 6 * (K1[3] + 2 * K2[3] + 2 * K3[3] + K4[3]);
                            F[4] = rkstep / 6 * (K1[4] + 2 * K2[4] + 2 * K3[4] + K4[4]);
                            F[5] = rkstep / 6 * (K1[5] + 2 * K2[5] + 2 * K3[5] + K4[5]);

                            velocity.X += (float)F[3];
                            velocity.Y += (float)F[4];
                            velocity.Z += (float)F[5];

                            point.X += (float)F[0];
                            point.Y += (float)F[1];
                            point.Z += (float)F[2];
                            */
                            



                            point += velocity * stepSize;
                            // this is the magical - 3/2 r^(-5) potential...
                            var accel = -1.5f * h2 * point / (float)Math.Pow(SqrNorm(point), 2.5);
                            velocity += accel * stepSize;










                            var pointSqr = SqrNorm(point);




                            Util.ToSpherical(point.X, point.Y, point.Z, ref tempR, ref tempTheta, ref tempPhi);



                            if (pointSqr < 1)
                            {
                                /*
                                var m1 = Util.DoubleMod(tempPhi, 1.04719); // Pi / 3
                                var m2 = Util.DoubleMod(tempTheta, 1.04719); // Pi / 3
                                bool foo = (m1 < 0.52359) ^ (m2 < 0.52359); // Pi / 6
                                if (foo)
                                {
                                    pixel = Color.Black;
                                }
                                else
                                {
                                    pixel = Color.Red;
                                }
                                */
                                pixel = Color.Red;

                                break;
                            }
                            else if (tempR > 30)
                            {

                                /*
                                var m1 = Util.DoubleMod(tempPhi, 1.04719); // Pi / 3
                                var m2 = Util.DoubleMod(tempTheta, 1.04719); // Pi / 3
                                bool foo = (m1 < 0.52359) ^ (m2 < 0.52359); // Pi / 6
                                if (foo)
                                {
                                    pixel = Color.Black;
                                }
                                else
                                {
                                    pixel = Color.Green;
                                }
                                */
                                


                                break;
                            }
                            



                        }


                        




                        outputBitmap[yOffset + x] = pixel.ToArgb();

                    }
                    Console.WriteLine("Thread {0}: Line {1} rendered.", param.JobId, y);


                    y += yIncrement;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Thread {0} error: {1}", param.JobId, e.Message);
            }
            Console.WriteLine("Thread {0} finished.", param.JobId);
        }


        private float SqrNorm(Vector3 v)
        {
            return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        }

        private Vector3 MatrixMul(Matrix4x4 m, Vector3 v)
        {
            return new Vector3(m.M11 * v.X + m.M21 * v.Y + m.M31 * v.Z,
                m.M12 * v.X + m.M22 * v.Y + m.M32 * v.Z,
                m.M13 * v.X + m.M23 * v.Y + m.M33 * v.Z);
        }

        private void RK4f(double[] y, double[] f, double h2)
        {
            f[0] = y[3];
            f[1] = y[4];
            f[2] = y[5];
            var d = Math.Pow(y[0] * y[0] + y[1] * y[1] + y[2] * y[2], 2.5);
            f[3] = -1.5 * h2 * y[0] / d;
            f[4] = -1.5 * h2 * y[1] / d;
            f[5] = -1.5 * h2 * y[2] / d;
        }

        private void RK4fAdd(double step, double[] y, double[] k, double[] o)
        {
            o[0] = y[0] + step * k[0];
            o[1] = y[1] + step * k[1];
            o[2] = y[2] + step * k[2];
            o[3] = y[3] + step * k[3];
            o[4] = y[4] + step * k[4];
            o[5] = y[5] + step * k[5];
        }

    }

    class ThreadParams
    {
        public int JobId { get; set; }
        public List<int> LinesList { get; set; }
        public Thread Thread { get; set; }
    }
}
