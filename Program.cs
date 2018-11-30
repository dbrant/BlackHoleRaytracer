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

            new RayProcessor(300, 300,
                        Scene.GetScene(65),
                        fileName)
                        .Process();

            n++;
        }
    }
}
