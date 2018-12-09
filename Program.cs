using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {

            // Set up some default parameters, which can be overridden by command line args.
            var cameraPos = new Vector3(0, 5, -20);
            var lookAt = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);
            float fov = 56f;
            float curvatureCoeff = -1.5f;
            float angularMomentum = 0;
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
            

            var hitables = new List<IHitable>
            {
                //new CheckeredDisk(2.6, 14.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(2.6, 14.0, new Bitmap("adisk_dark.jpg")),
                //new CheckeredDisk(equation.Rmstable, 20.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(2, 20.0, new Bitmap("adisk.jpg")),
                new Horizon(null, false),
                new Sky(new Bitmap("skymap_8k.jpg"), 30).SetTextureOffset(Math.PI / 2),
                //new CheckeredSphere(2, 2, -14, 1, Color.RoyalBlue, Color.DarkBlue),
                //new TexturedSphere(2, 2, -14, 1, new Bitmap("earthmap1k.jpg")).SetTextureOffset(Math.PI),
                //new ReflectiveSphere(-1, 2, -14, 1),
                //new ReflectiveSphere(12, 0, 3, 1),
                //new TexturedSphere(24, 0, 2, 1, new Bitmap("earthmap1k.jpg")),
                //new TexturedSphere(16, 0, 4, 1, new Bitmap("gstar.jpg")),
                //new TexturedSphere(-10, -10, -10, 1, new Bitmap("gstar.jpg")),
                //new CheckeredSphere(-10, -10, -10, 1, Color.RoyalBlue, Color.DarkBlue)
            };

            var starTexture = new Bitmap("gstar.jpg");
            var starBitmap = Util.getNativeTextureBitmap(starTexture);
            var random = new Random();
            for (int i = 0; i < 20; i++)
            {
                double tempR = 4.0 + random.NextDouble() * 10.0;
                double tempTheta = random.NextDouble() * Math.PI * 2;
                double tempX = 0, tempY = 0, tempZ = 0;
                Util.ToCartesian(tempR, tempTheta, 0, ref tempX, ref tempY, ref tempZ);
                hitables.Add(new TexturedSphere(tempX, tempY, tempZ, 0.05f + (float)random.NextDouble() * 0.2f, starBitmap, starTexture.Width, starTexture.Height)
                    .SetTextureOffset(random.NextDouble() * Math.PI * 2));
            }
            

            var scene = new Scene(cameraPos, lookAt, up, fov, hitables, curvatureCoeff, angularMomentum);

            //new KerrRayProcessor(400, 200, scene, fileName).Process();
            new SchwarzschildRayProcessor(600, 400, scene, fileName).Process();
        }
    }
}
