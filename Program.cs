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
            double phi = 45; // horizontal angle
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
                new Disk(equation.Rmstable, 20.0, new Bitmap("adisk.jpg"), false),
                new Horizon(null, false),
                new Sky(new Bitmap("sky_16k.jpg")),
                //new ReflectiveSphere(0, 6, 3, 1),
                //new Sphere(0, 6, 6, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true),
                //new Sphere(-10, -10, -10, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true)
            });

            new RayProcessor(1000, 1000, scene, fileName).Process();
        }
    }
}
