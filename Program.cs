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

            new RayProcessor(500, 500,
                        new Scene(30, 75, 65),
                        fileName)
                        .Process();

            n++;
        }
    }
}
