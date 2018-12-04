using BlackHoleRaytracer.Equation;
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

            var scene = new Scene(r, theta, phi, equation, new List<IHitable>
            {
                new CheckeredDisk(equation.Rmstable, 20.0, Color.BlueViolet, Color.MediumBlue, Color.ForestGreen, Color.LightSeaGreen),
                //new TexturedDisk(equation.Rmstable, 20.0, new Bitmap("adisk.jpg")),
                new Horizon(null, false),
                new Sky(new Bitmap("skymap_8k.jpg")),
                //new ReflectiveSphere(12, 0, 3, 1),
                new TexturedSphere(16, 0, 4, 1, new Bitmap("gstar.jpg")),
                new CheckeredSphere(-10, -10, -10, 1, Color.RoyalBlue, Color.DarkBlue)
            });

            new RayProcessor(1000, 1000, scene, fileName).Process();
        }
    }
}
