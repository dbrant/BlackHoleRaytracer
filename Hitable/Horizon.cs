using System;
using System.Drawing;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Horizon : IHitable
    {
        private bool checkered;

        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public Horizon(Bitmap texture, bool checkered)
        {
            this.checkered = checkered;
            if (texture != null)
            {
                textureMap = new SphericalMapping(texture.Width, texture.Height);
                textureWidth = texture.Width;
                textureBitmap = Util.getNativeTextureBitmap(texture);
            }
        }

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop)
        {
            // Has the ray fallen past the horizon?
            if (y[0] < equation.Rhor)
            {
                if (checkered)
                {
                    var m1 = DoubleMod(y[2], 1.04719); // Pi / 3
                    var m2 = DoubleMod(y[1], 1.04719); // Pi / 3
                    bool foo = (m1 < 0.52359) ^ (m2 < 0.52359); // Pi / 6
                    if (foo)
                    {
                        color = Color.Black;
                    }
                    else
                    {
                        color = Color.Green;
                    }
                }
                else if (textureBitmap != null)
                {
                    int xPos, yPos;
                    textureMap.Map(y[0], y[1], -y[2], out xPos, out yPos);

                    color = Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
                }
                else
                {
                    color = Color.Black;
                }
                stop = true;
                return true;
            }
            return false;
        }

        private static double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }
    }
}
