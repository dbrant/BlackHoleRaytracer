using System;
using System.Drawing;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Sky : IHitable
    {
        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public Sky(Bitmap texture)
        {
            if (texture != null)
            {
                textureMap = new SphericalMapping(texture.Width, texture.Height);
                textureWidth = texture.Width;
                textureBitmap = Util.getNativeTextureBitmap(texture);
            }
        }

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            // Has the ray escaped to infinity?
            if (y[0] > equation.R0)
            {
                // Restore Y to its previous values, and perform the binary intersection search.
                Util.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                int xPos, yPos;
                textureMap.Map(y[0], y[1], y[2], out xPos, out yPos);

                color = Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
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

        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
        {
            unsafe
            {
                double hlower = 0.0;
                equation.Function(y, dydx);

                while ((y[0] > equation.Rhor) && (y[0] < equation.R0 * 2))
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

                    if (yout[0] < equation.R0)
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
