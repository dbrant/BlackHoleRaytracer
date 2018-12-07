﻿using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {

            // Set up some default parameters, which can be overridden by command line args.
            double r = 30; // distance from center
            double theta = 87; // vertical angle
            double phi = 0; // horizontal angle
            double angularMomentum = 0;
            string fileName = "image.png";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-r") && i < args.Length - 1)
                {
                    r = Double.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-theta") && i < args.Length - 1)
                {
                    theta = Double.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-phi") && i < args.Length - 1)
                {
                    phi = Double.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-a") && i < args.Length - 1)
                {
                    angularMomentum = Double.Parse(args[i + 1]);
                }
                else if (args[i].Equals("-o") && i < args.Length - 1)
                {
                    fileName = args[i + 1];
                }
            }

            var equation = new KerrBlackHoleEquation(r, theta, phi, angularMomentum);



            var hitables = new List<IHitable>
            {
                //new CheckeredDisk(2.6, 14.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(2.6, 14.0, new Bitmap("adisk.jpg")),
                //new CheckeredDisk(equation.Rmstable, 20.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(equation.Rmstable, 20.0, new Bitmap("adisk.jpg")),
                new Horizon(null, false),
                new Sky(new Bitmap("skymap_8k.jpg"), 30).SetTextureOffset(Math.PI / 2),
                //new CheckeredSphere(2, 2, -14, 1, Color.RoyalBlue, Color.DarkBlue),
                new TexturedSphere(2, 2, -14, 1, new Bitmap("earthmap1k.jpg")),
                new ReflectiveSphere(-1, 2, -14, 1),
                //new ReflectiveSphere(12, 0, 3, 1),
                //new TexturedSphere(24, 0, 2, 1, new Bitmap("earthmap1k.jpg")),
                //new TexturedSphere(16, 0, 4, 1, new Bitmap("gstar.jpg")),
                //new TexturedSphere(-10, -10, -10, 1, new Bitmap("gstar.jpg")),
                //new CheckeredSphere(-10, -10, -10, 1, Color.RoyalBlue, Color.DarkBlue)
            };

            var starTexture = new Bitmap("gstar.jpg");
            var starBitmap = Util.getNativeTextureBitmap(starTexture);
            var random = new Random();
            for (int i = 0; i < 250; i++)
            {
                double tempR = 4.0 + random.NextDouble() * 10.0;
                double tempTheta = random.NextDouble() * Math.PI * 2;
                double tempX = 0, tempY = 0, tempZ = 0;
                Util.ToCartesian(tempR, tempTheta, 0, ref tempX, ref tempY, ref tempZ);
                hitables.Add(new TexturedSphere(tempX, tempY, tempZ, 0.05 + random.NextDouble() * 0.2, starBitmap, starTexture.Width, starTexture.Height));
            }





            var scene = new Scene(r, theta, phi, equation, hitables);

            //new RayProcessor(640, 480, scene, fileName).Process();
            new SchwarzschildRayProcessor(300, 200, scene, fileName).Process();

            //Console.ReadKey();
        }
    }
}
