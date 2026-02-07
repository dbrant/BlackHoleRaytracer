using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {

            // Set up some default parameters, which can be overridden by command line args.
            var cameraPos = new Vector3(0, 5, -18);
            var lookAt = new Vector3(0, 0, 0);
            var up = new Vector3(0f, 1, 0);
            float fov = 55f;
            float curvatureCoeff = -1.5f;
            float angularMomentum = 0f;
            string fileName = "image.png";


            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-camera") && i < args.Length - 3)
                {
                    cameraPos = new Vector3(float.Parse(args[i + 1]), float.Parse(args[i + 2]), float.Parse(args[i + 3]));
                }
                else if (args[i].Equals("-lookat") && i < args.Length - 3)
                {
                    lookAt = new Vector3(float.Parse(args[i + 1]), float.Parse(args[i + 2]), float.Parse(args[i + 3]));
                }
                else if (args[i].Equals("-up") && i < args.Length - 3)
                {
                    up = new Vector3(float.Parse(args[i + 1]), float.Parse(args[i + 2]), float.Parse(args[i + 3]));
                }
                else if (args[i].Equals("-fov") && i < args.Length - 1)
                {
                    fov = float.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-curvature") && i < args.Length - 1)
                {
                    curvatureCoeff = float.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-angularmomentum") && i < args.Length - 1)
                {
                    angularMomentum = float.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-o") && i < args.Length - 1)
                {
                    fileName = args[i + 1];
                }
            }



            Util.Precalculate();
            

            var hitables = new List<IHitable>
            {
                //new CheckeredDisk(3.0, 10.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.DarkGreen),
                //new TexturedDisk(2.6, 10.0, new Bitmap("adisk5.jpg")),
                //new CheckeredDisk(equation.Rmstable, 20.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(2, 20.0, new Bitmap("disk.jpg")),
                new Horizon(null, true),
                new Sky(new Bitmap("sky8k.jpg"), 60).SetTextureOffset(Math.PI / 2),

                //new CheckeredSphere(2, 2, -14, 1, Color.RoyalBlue, Color.DarkBlue),

                new TexturedSphere(2, 2, -10, 1, new Bitmap("earth1k.jpg")).SetTextureOffset(Math.PI),
                new TexturedSphere(-2, -2, -8, 1, new Bitmap("mars1k.jpg")),
                new ReflectiveSphere(-1, 2, -10, 1),
                new ReflectiveSphere(3, -3, -7, 1),
                new ReflectiveSphere(3, -5, 5, 1),
                new ReflectiveSphere(-3.7, 2, -7, 1),

                //new TexturedSphere(24, 0, 2, 1, new Bitmap("earthmap1k.jpg")),
                //new TexturedSphere(16, 0, 4, 1, new Bitmap("gstar.jpg")),
                //new TexturedSphere(-10, -10, -10, 1, new Bitmap("gstar.jpg")),
                new CheckeredSphere(-10, -10, -10, 1, Color.RoyalBlue, Color.DarkBlue)
            };

            var starTexture = new Bitmap("sun2k.jpg");
            var starBitmap = Util.getNativeTextureBitmap(starTexture);
            var random = new Random();
            double tempR = 0, tempTheta = 0, tempPhi = 0;
            double tempX = 0, tempY = 0, tempZ = 0;

            int numRandomSpheres = 20;
            for (int i = 0; i < numRandomSpheres; i++)
            {
                tempR = 5 + random.NextDouble() * 6.0;
                tempTheta = (Math.PI * 2 / numRandomSpheres) * i + (random.NextDouble() - 0.5) * (Math.PI / 32); // random.NextDouble() * Math.PI * 2;
                Util.ToCartesian(tempR, tempTheta, 0, ref tempX, ref tempY, ref tempZ);
                tempY += (random.NextDouble() - 0.5) * 4;
                hitables.Add(new TexturedSphere(tempX, tempY, tempZ, 0.05f + (float)random.NextDouble() * 0.4f, starBitmap, starTexture.Width, starTexture.Height)
                    .SetTextureOffset(random.NextDouble() * Math.PI * 2));
            }

            int numReflectiveSpheres = 0;
            for (int i = 0; i < numReflectiveSpheres; i++)
            {
                tempR = 4 + random.NextDouble() * 6.0;
                tempTheta = (Math.PI * 2 / numRandomSpheres) * i + (random.NextDouble() - 0.5) * (Math.PI / 32);
                Util.ToCartesian(tempR, tempTheta, 0, ref tempX, ref tempY, ref tempZ);
                tempY += (random.NextDouble() - 0.5) * 4;
                hitables.Add(new ReflectiveSphere(tempX, tempY, tempZ, 0.2f + (float)random.NextDouble() * 0.8f));
            }


            //var scene = new Scene(cameraPos, lookAt, up, fov, hitables, curvatureCoeff, angularMomentum);

            ////new KerrRayProcessor(400, 200, scene, fileName).Process();
            //new SchwarzschildRayProcessor(192, 108, scene, fileName).Process();



            int numFrames = 20;
            double angleIncrement = (Math.PI * 2) / 1000; // numFrames;
            var rotationMatrix = Matrix4x4.CreateRotationY((float)angleIncrement);
            tempR = 20; tempTheta = 0; tempPhi = 0;

            Directory.CreateDirectory("anim");

            for (int i = 0; i < numFrames; i++)
            {
                fileName = Path.Combine("anim", "frame" + i + ".png");

                Console.WriteLine("Rendering frame " + i);


                tempTheta += angleIncrement;
                //tempPhi = Math.Sin(tempTheta) * (Math.PI / 6);

                var rotation = Matrix4x4.CreateRotationY((float)tempTheta);
                var tempCamPos = cameraPos;
                tempCamPos = Vector3.Transform(tempCamPos, rotation);
                rotation = Matrix4x4.CreateRotationX((float)tempPhi);
                tempCamPos = Vector3.Transform(tempCamPos, rotation);


                tempCamPos.Z = cameraPos.Z + (float)Math.Cos(i / 600f) * 4f;


                //double curveFactor = 4;

                //double c = Math.Cos(tempTheta * 2) * Math.Sqrt((1 + curveFactor * curveFactor) / (1 + curveFactor * curveFactor * Math.Cos(tempTheta * 2) * Math.Cos(tempTheta * 2)));
                //c = (c + 1.0) / 2.0;


                var scene = new Scene(tempCamPos, lookAt, up, fov, hitables, (float)(curvatureCoeff), angularMomentum);

                //new KerrRayProcessor(1000, 600, scene, fileName).Process();
                //new SchwarzschildRayProcessor(1920, 1080, scene, fileName, true).Process();
                new SchwarzschildRayProcessor(3840, 2160, scene, fileName, false).Process();
                //new SchwarzschildRayProcessor(320, 200, scene, fileName, false).Process();
                //new SchwarzschildRayProcessor(128, 64, scene, fileName, false).Process();


                //curvatureMultiplier += angleIncrement;
            }

            /*
            int numFrames = 4000;
            double angleIncrement = (Math.PI * 2) / 2000; // numFrames;
            double curvatureMultiplier = 0;
            var rotationMatrix = Matrix4x4.CreateRotationY((float)angleIncrement);
            tempR = 20; tempTheta = 0; tempPhi = 0;

            Directory.CreateDirectory("anim");

            for (int i = 0; i < numFrames; i++)
            {
                fileName = Path.Combine("anim", "frame" + i + ".png");

                Console.WriteLine("Rendering frame " + i);


                tempTheta += angleIncrement;
                tempPhi = Math.Sin(tempTheta) * (Math.PI / 6);

                var rotation = Matrix4x4.CreateRotationY((float)tempTheta);
                var tempCamPos = cameraPos;
                tempCamPos = Vector3.Transform(tempCamPos, rotation);
                rotation = Matrix4x4.CreateRotationX((float)tempPhi);
                tempCamPos = Vector3.Transform(tempCamPos, rotation);
                
                var scene = new Scene(tempCamPos, lookAt, up, fov, hitables, curvatureCoeff, angularMomentum);

                //new KerrRayProcessor(400, 200, scene, fileName).Process();
                new SchwarzschildRayProcessor(1280, 720, scene, fileName, false).Process();
                //new SchwarzschildRayProcessor(460, 300, scene, fileName, false).Process();


                //curvatureMultiplier += angleIncrement;
            }
            */


            /*
            int numFrames = 16;
            double angleIncrement = (Math.PI * 2) / numFrames;
            double curvatureMultiplier = 0;
            var rotationMatrix = Matrix4x4.CreateRotationY((float)angleIncrement);

            Directory.CreateDirectory("anim");

            for (int i = 0; i < numFrames; i++)
            {
                fileName = Path.Combine("anim", "frame" + i + ".png");
                
                var scene = new Scene(cameraPos, lookAt, up, fov, hitables, curvatureCoeff * (float)(1.0 - Math.Cos(curvatureMultiplier) * Math.Cos(curvatureMultiplier)), angularMomentum);

                //new KerrRayProcessor(400, 200, scene, fileName).Process();
                new SchwarzschildRayProcessor(1200, 720, scene, fileName).Process();
                
                cameraPos = Vector3.Transform(cameraPos, rotationMatrix);
                //curvatureMultiplier += angleIncrement;
            }
            */

        }
    }
}
