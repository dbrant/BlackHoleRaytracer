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
        private double radius = 1.0;

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

        public bool Hit(Vector3 point, double sqrNorm, Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, double r, double theta, double phi, ref Color color, ref bool stop, bool debug)
        {
            // Has the ray fallen past the horizon?
            if (prevSqrNorm > 1 && sqrNorm < 1)
            {
                var colpoint = IntersectionSearch(prevPoint, velocity, equation);

                double tempR = 0, tempTheta = 0, tempPhi = 0;
                Util.ToSpherical(colpoint.X, colpoint.Y, colpoint.Z, ref tempR, ref tempTheta, ref tempPhi);
                
                if (checkered)
                {
                    var m1 = Util.DoubleMod(tempTheta, 1.04719); // Pi / 3
                    var m2 = Util.DoubleMod(tempPhi, 1.04719); // Pi / 3
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


        protected Vector3 IntersectionSearch(Vector3 prevPoint, Vector3 velocity, SchwarzschildBlackHoleEquation equation)
        {
            float stepLow = 0, stepHigh = equation.stepSize;
            Vector3 newPoint = prevPoint;
            Vector3 tempVelocity;
            while (true)
            {
                float stepMid = (stepLow + stepHigh) / 2;
                newPoint = prevPoint;
                tempVelocity = velocity;
                equation.Function(ref newPoint, ref tempVelocity, stepMid);

                double distance = Util.SqrNorm(newPoint);
                if (Math.Abs(stepHigh - stepLow) < 0.00001)
                {
                    break;
                }
                if (distance < radius)
                {
                    stepHigh = stepMid;
                }
                else
                {
                    stepLow = stepMid;
                }
            }
            return newPoint;
        }
    }
}
