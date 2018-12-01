using System;
using System.IO;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {
            int n = 1;

            string fileName = Path.Combine(".", String.Format("image{0}.png", n));

            new RayProcessor(1000, 1000,
                        new Scene(30, 75, 80),
                        fileName)
                        .Process();

            n++;
        }
    }
}
