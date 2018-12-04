using System.Drawing;

namespace BlackHoleRaytracer.Hitable
{
    public class CheckeredSphere : Sphere
    {
        private Color color1;
        private Color color2;
        
        public CheckeredSphere(double centerX, double centerY, double centerZ, double radius, Color color1, Color color2)
            : base(centerX, centerY, centerZ, radius)
        {
            this.color1 = color1;
            this.color2 = color2;
        }

        protected override Color GetColor(double r, double theta, double phi)
        {
            var m1 = DoubleMod(phi, 1.04719); // Pi / 3
            var m2 = DoubleMod(theta, 1.04719); // Pi / 3
            // Pi / 6
            return (m1 < 0.52359) ^ (m2 < 0.52359) ? color1 : color2;
        }
    }
}
