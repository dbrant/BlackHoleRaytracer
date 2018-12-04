using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Sphere : IHitable
    {
        protected double centerX;
        protected double centerY;
        protected double centerZ;
        protected double radius;
        protected Vector3 center;

        private bool checkered;
        
        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public Sphere(double centerX, double centerY, double centerZ, double radius, Bitmap texture, bool checkered)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.centerZ = centerZ;
            this.radius = radius;
            this.checkered = checkered;
            center = new Vector3((float)centerX, (float)centerY, (float)centerZ);
            if (texture != null)
            {
                textureMap = new SphericalMapping(texture.Width, texture.Height);
                textureWidth = texture.Width;
                textureBitmap = Util.getNativeTextureBitmap(texture);
            }
        }

        public override unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            double tempX = 0, tempY = 0, tempZ = 0;
            ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

            double distance = Math.Sqrt((tempX - centerX) * (tempX - centerX)
                + (tempY - centerY) * (tempY - centerY)
                + (tempZ - centerZ) * (tempZ - centerZ));
            if (distance < radius)
            {
                // Restore Y to its previous values, and perform the binary intersection search.
                Util.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                // transform impact coordinates to cartesian coordinates relative to center of sphere.
                ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

                var impact = new Vector3((float)tempX, (float)tempY, (float)tempZ);
                var impactFromCenter = Vector3.Normalize(impact - center);
                
                // and now transform to spherical coordinates relative to center of sphere.
                double tempR = 0, tempTheta = 0, tempPhi = 0;
                ToSpherical(impactFromCenter.X, impactFromCenter.Y, impactFromCenter.Z, ref tempR, ref tempTheta, ref tempPhi);
                                
                if (checkered)
                {
                    var m1 = DoubleMod(tempPhi, 1.04719); // Pi / 3
                    var m2 = DoubleMod(tempTheta, 1.04719); // Pi / 3
                    bool foo = (m1 < 0.52359) ^ (m2 < 0.52359); // Pi / 6
                    if (foo)
                    {
                        color = Color.RoyalBlue;
                    }
                    else
                    {
                        color = Color.DarkBlue;
                    }
                }
                else
                {
                    // rotate by 180 degrees
                    tempPhi += Math.PI;

                    // and retransform into cartesian coordinates
                    ToCartesian(tempR, tempTheta, tempPhi, ref tempX, ref tempY, ref tempZ);
                    impactFromCenter = new Vector3((float)tempX, (float)tempY, (float)tempZ);

                    int xPos, yPos;
                    textureMap.MapCartesian(-impactFromCenter.X, impactFromCenter.Z, impactFromCenter.Y, out xPos, out yPos);

                    color = Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
                }

                stop = true;
                return true;
            }
            return false;
        }
        
        public static void ToCartesian(double r, double theta, double phi, ref double x, ref double y, ref double z)
        {
            x = r * Math.Cos(phi) * Math.Sin(theta);
            y = r * Math.Sin(phi) * Math.Sin(theta);
            z = r * Math.Cos(theta);
        }

        public static void ToSpherical(double x, double y, double z, ref double r, ref double theta, ref double phi)
        {
            r = Math.Sqrt(x*x + y*y + z*z);
            phi = Math.Atan(y / x);
            theta = Math.Acos(z / r);
        }

        protected static double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }

        protected unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
        {
            unsafe
            {
                double hlower = 0.0;
                double tempX = 0, tempY = 0, tempZ = 0;
                equation.Function(y, dydx);

                while ((y[0] > equation.Rhor) && (y[0] < equation.R0))
                {
                    double* yout = stackalloc double[equation.N];
                    double* yerr = stackalloc double[equation.N];

                    double hdiff = hupper - hlower;

                    if (Math.Abs(hdiff) < 1e-7)
                    {
                        RungeKutta.IntegrateStep(equation, y, dydx, hupper, yout, yerr);

                        Util.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));
                        return;
                    }

                    double hmid = (hupper + hlower) / 2;

                    RungeKutta.IntegrateStep(equation, y, dydx, hmid, yout, yerr);

                    ToCartesian(yout[0], yout[1], yout[2], ref tempX, ref tempY, ref tempZ);
                    double distance = Math.Sqrt((tempX - centerX) * (tempX - centerX)
                        + (tempY - centerY) * (tempY - centerY)
                        + (tempZ - centerZ) * (tempZ - centerZ));

                    if (distance > radius)
                    {
                        hlower = hmid;
                    }
                    else
                    {
                        hupper = hmid;
                    }
                }
            }
        }
    }
}
