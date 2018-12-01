using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Helpers;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Disk : IHitable
    {
        double radiusInner;
        double radiusOuter;

        private bool checkered;
        private DiscMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public Disk(double radiusInner, double radiusOuter, Bitmap texture, bool checkered)
        {
            this.radiusInner = radiusInner;
            this.radiusOuter = radiusOuter;
            this.checkered = checkered;
            if (texture != null)
            {
                lock (texture)
                {
                    textureMap = new DiscMapping(radiusInner, radiusOuter, texture.Width, texture.Height);
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
            // Remember what side of the theta-plane we're currently on, so that we can detect
            // whether we've crossed the plane after stepping.
            int side = prevY[1] > Math.PI / 2 ? 1 : prevY[1] < Math.PI / 2 ? -1 : 0;

            // Did we cross the theta (horizontal) plane?
            bool success = false;
            if ((y[1] - Math.PI / 2) * side <= 0)
            {
                // remember the current values of Y, so that we can restore them after the intersection search.
                double* yCurrent = stackalloc double[equation.N];
                MemHelper.memcpy((IntPtr)yCurrent, (IntPtr)y, equation.N * sizeof(double));

                //  Overwrite Y with its previous values, and perform the binary intersection search.
                MemHelper.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                // Is the ray within the accretion disk?
                if ((y[0] >= radiusInner) && (y[0] <= radiusOuter))
                {
                    if (checkered)
                    {
                        var m1 = DoubleMod(y[2], 1.04719);
                        bool foo = (m1 < 0.52359);
                        color = side == -1 ? (foo ? Color.BlueViolet : Color.MediumBlue) : (foo ? Color.ForestGreen : Color.LightSeaGreen);
                    }
                    else
                    {
                        int xPos, yPos;
                        // do mapping of texture image
                        textureMap.Map(y[0], y[1], y[2], out xPos, out yPos);
                        color = Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
                    }
                    stop = false;
                    success = true;
                }

                // ...and reset Y to its original current values.
                MemHelper.memcpy((IntPtr)y, (IntPtr)yCurrent, equation.N * sizeof(double));
            }
            return success;
        }

        private static double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }

        /// <summary>
        /// Use Runge-Kutta steps to find intersection with horizontal plane of the scene.
        /// This is necessary to stop integrating when the ray hits the accretion disc.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        /// <param name="hupper"></param>
        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
        {
            unsafe
            {
                double hlower = 0.0;

                int side;
                if (y[1] > Math.PI / 2)
                {
                    side = 1;
                }
                else if (y[1] < Math.PI / 2)
                {
                    side = -1;
                }
                else
                {
                    // unlikely, but needs to handle a situation when ray hits the plane EXACTLY
                    return;
                }

                equation.Function(y, dydx);

                while ((y[0] > equation.Rhor) && (y[0] < equation.R0) && (side != 0))
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

                    if (side * (yout[1] - Math.PI / 2) > 0)
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
