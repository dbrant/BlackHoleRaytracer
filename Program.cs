using System;

namespace BlackHoleRaytracer
{
    class Program
    {
        static void Main(string[] args)
        {
            //var rayIllustrator = new RayIllustrationGenerator(new Scene() { ViewDistance = 30, ViewInclination = 90, CameraAperture = 3.5 });
            //rayIllustrator.Process();
            //return;


            int n = 1;
            //for (double d = 0.1; d < 359; d += 5.0) {

                //Console.WriteLine("Starting " + d);

                var rayTracer = new RayProcessor(
                            300,
                            300,
                            Scene.GetScene(65),
                            n,
                            ".");
                rayTracer.Process();

                n++;
            //}
        }
    }
}
