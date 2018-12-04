using System;
using System.Drawing;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class Disk : IHitable
    {
        double radiusInner;
        double radiusOuter;
        
        public Disk(double radiusInner, double radiusOuter)
        {
            this.radiusInner = radiusInner;
            this.radiusOuter = radiusOuter;
        }

        protected virtual Color GetColor(int side, double r, double theta, double phi)
        {
            return Color.White;
        }

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
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
                Util.memcpy((IntPtr)yCurrent, (IntPtr)y, equation.N * sizeof(double));

                //  Overwrite Y with its previous values, and perform the binary intersection search.
                Util.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                // Is the ray within the accretion disk?
                if ((y[0] >= radiusInner) && (y[0] <= radiusOuter))
                {
                    color = GetColor(side, y[0], y[1], y[2]);

                    stop = false;
                    success = true;
                }

                // ...and reset Y to its original current values.
                Util.memcpy((IntPtr)y, (IntPtr)yCurrent, equation.N * sizeof(double));
            }
            return success;
        }

        /// <summary>
        /// Use Runge-Kutta steps to find intersection with horizontal plane of the scene.
        /// This is necessary to stop integrating when the ray hits the accretion disc.
        /// </summary>
        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
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
                    RungeKutta.IntegrateStep(equation, y, dydx, hupper, yout, yerr);

                    Util.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));

                    return;
                }

                double hmid = (hupper + hlower) / 2;

                RungeKutta.IntegrateStep(equation, y, dydx, hmid, yout, yerr);

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
