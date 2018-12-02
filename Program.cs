using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {
            int n = 1;

            double r = 30; // distance from center
            double theta = 80; // vertical angle
            double phi = 75; // horizontal angle

            string fileName = Path.Combine(".", String.Format("image{0}.png", n));

            var equation = new KerrBlackHoleEquation(r, theta, phi);

            var scene = new Scene(r, theta, phi, equation, new List<IHitable>
            {
                new Disk(equation.Rmstable, 20.0, new Bitmap("adisk.jpg"), true),
                new Horizon(null, false),
                new Sky(new Bitmap("sky_16k.jpg")),
                //new Sphere(12, 7, 3, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true),
                //new Sphere(-10, -10, -10, 1, /*new Bitmap("earthmap1k.jpg")*/ null, true)
            });

            new RayProcessor(1000, 1000, scene, fileName).Process();

            n++;
        }
    }
}
