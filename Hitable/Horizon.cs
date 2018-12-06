using System;
using System.Drawing;
using System.Numerics;
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

        public bool Hit(Vector3 point, Vector3 prevPoint, double pointSqrNorm, double r, double theta, double phi, ref Color color, ref bool stop, bool debug)
        {
            // Has the ray fallen past the horizon?
            if (pointSqrNorm < 1)
            {
                if (checkered)
                {
                    var m1 = Util.DoubleMod(phi, 1.04719); // Pi / 3
                    var m2 = Util.DoubleMod(theta, 1.04719); // Pi / 3
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
                    textureMap.Map(r, theta, -phi, out xPos, out yPos);

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

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            // Has the ray fallen past the horizon?
            if (y[0] < equation.Rhor)
            {
                if (checkered)
                {
                    var m1 = Util.DoubleMod(y[2], 1.04719); // Pi / 3
                    var m2 = Util.DoubleMod(y[1], 1.04719); // Pi / 3
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
    }
}
