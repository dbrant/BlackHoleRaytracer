using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Helpers;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Sphere : IHitable
    {
        private double centerX;
        private double centerY;
        private double centerZ;
        private double radius;
        
        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public Sphere(double centerX, double centerY, double centerZ, double radius, Bitmap texture)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.centerZ = centerZ;
            this.radius = radius;
            if (texture != null)
            {
                lock (texture)
                {
                    textureMap = new SphericalMapping(texture.Width, texture.Height);
                    textureWidth = texture.Width;
                    
                    textureBitmap = new int[texture.Width * texture.Height];
                    BitmapData diskBits = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(diskBits.Scan0, textureBitmap, 0, textureBitmap.Length);
                    texture.UnlockBits(diskBits);
                }
            }
        }

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop)
        {
            double tempX = 0, tempY = 0, tempZ = 0;
            ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

            double distance = Math.Sqrt((tempX - centerX) * (tempX - centerX)
                + (tempY - centerY) * (tempY - centerY)
                + (tempZ - centerZ) * (tempZ - centerZ));
            if (distance < radius)
            {
                // Restore Y to its previous values, and perform the binary intersection search.
                MemHelper.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                // transform impact coordinates to spherical coordinates relative to center of sphere
                ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);
                tempX = tempX - centerX;
                tempY = tempY - centerY;
                tempZ = tempZ - centerZ;
                double tempR = 0, tempTheta = 0, tempPhi = 0;
                ToSpherical(tempX, tempY, tempZ, ref tempR, ref tempTheta, ref tempPhi);

                int xPos, yPos;
                textureMap.Map(tempR, tempTheta, tempPhi, out xPos, out yPos);

                color = Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
                
                //color = Color.Pink;

                stop = true;
                return true;
            }
            return false;
        }
        
        private static void ToCartesian(double r, double theta, double phi, ref double x, ref double y, ref double z)
        {
            x = r * Math.Cos(phi) * Math.Sin(theta);
            y = r * Math.Sin(phi) * Math.Sin(theta);
            z = r * Math.Cos(theta);
        }

        private static void ToSpherical(double x, double y, double z, ref double r, ref double theta, ref double phi)
        {
            r = Math.Sqrt(x*x + y*y + z*z);
            theta = Math.Atan(y / x);
            phi = Math.Atan(Math.Sqrt(x*x + y*y) / z);
        }

        private static double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }

        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
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
                        RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hupper, yout, yerr);

                        MemHelper.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));
                        return;
                    }

                    double hmid = (hupper + hlower) / 2;

                    RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hmid, yout, yerr);

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
