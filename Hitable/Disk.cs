using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class Disk : IHitable
    {
        private double radiusInner;
        private double radiusOuter;
        private double radiusInnerSqr;
        private double radiusOuterSqr;
        
        public Disk(double radiusInner, double radiusOuter)
        {
            this.radiusInner = radiusInner;
            this.radiusOuter = radiusOuter;
            radiusInnerSqr = radiusInner * radiusInner;
            radiusOuterSqr = radiusOuter * radiusOuter;
        }

        protected virtual Color GetColor(int side, double r, double theta, double phi)
        {
            return Color.White;
        }

        public bool Hit(Vector3 point, double sqrNorm, Vector3 prevPoint, double prevSqrNorm, Vector3 velocity, SchwarzschildBlackHoleEquation equation, double r, double theta, double phi, ref Color color, ref bool stop, bool debug)
        {
            // Remember what side of the plane we're currently on, so that we can detect
            // whether we've crossed the plane after stepping.
            int side = prevPoint.Y > 0 ? -1 : prevPoint.Y < 0 ? 1 : 0;

            // Did we cross the horizontal plane?
            bool success = false;
            if (point.Y * side >= 0)
            {
                var colpoint = IntersectionSearch(side, prevPoint, velocity, equation);
                var colpointsqr = Util.SqrNorm(colpoint);

                if ((colpointsqr >= radiusInnerSqr) && (colpointsqr <= radiusOuterSqr))
                {
                    double tempR = 0, tempTheta = 0, tempPhi = 0;
                    Util.ToSpherical(colpoint.X, colpoint.Y, colpoint.Z, ref tempR, ref tempTheta, ref tempPhi);
                    
                    color = GetColor(side, tempR, tempTheta, tempPhi);

                    stop = false;
                    success = true;
                }
            }
            return success;
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

        protected Vector3 IntersectionSearch(int side, Vector3 prevPoint, Vector3 velocity, SchwarzschildBlackHoleEquation equation)
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
                
                if (Math.Abs(newPoint.Y) < 0.001)
                {
                    break;
                }
                if (side * newPoint.Y > 0)
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
