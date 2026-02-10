using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class ReflectiveSphere(double centerX, double centerY, double centerZ, float radius) : IHitable
    {
        protected double centerX = centerX;
        protected double centerY = centerY;
        protected double centerZ = centerZ;
        protected float radius = radius;
        protected float radiusSqr = radius * radius;
        protected Vector3 center = new((float)centerX, (float)centerY, (float)centerZ);

        public bool Hit(ref Vector3 point, double sqrNorm, ref Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            float distanceSqr = (point.X - center.X) * (point.X - center.X)
                + (point.Y - center.Y) * (point.Y - center.Y)
                + (point.Z - center.Z) * (point.Z - center.Z);
            if (distanceSqr < radiusSqr)
            {
                point = IntersectionSearch(prevPoint, velocity, equation);
                velocity = Vector3.Reflect(velocity, Vector3.Normalize(point - center));

                color = Util.AddColor(Color.FromArgb(8, 8, 8), color);
                stop = false;
                return true;
            }
            return false;
        }

        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            double tempX = 0, tempY = 0, tempZ = 0;
            Util.ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

            double distance = Math.Sqrt((tempX - centerX) * (tempX - centerX)
                + (tempY - centerY) * (tempY - centerY)
                + (tempZ - centerZ) * (tempZ - centerZ));
            if (distance < radius)
            {
                // Restore Y to its previous values, and perform the binary intersection search.
                Util.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));

                IntersectionSearch(y, dydx, hdid, equation);

                // transform impact coordinates to cartesian coordinates relative to center of sphere.
                Util.ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

                var impact = new Vector3((float)tempX, (float)tempY, (float)tempZ);
                var impactFromCenter = Vector3.Normalize(impact - center);

                // and now transform to spherical coordinates relative to center of sphere.
                double tempR = 0, tempTheta = 0, tempPhi = 0;
                Util.ToSpherical(impactFromCenter.X, impactFromCenter.Y, impactFromCenter.Z, ref tempR, ref tempTheta, ref tempPhi);

                color = Color.White; //GetColor(tempR, tempTheta, tempPhi);

                stop = true;
                return true;
            }
            return false;
        }

        protected unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
        {
            double hlower = 0.0;
            double tempX = 0, tempY = 0, tempZ = 0;
            equation.Function(y, dydx);

            double* yout = stackalloc double[equation.N];
            double* yerr = stackalloc double[equation.N];

            while ((y[0] > equation.Rhor) && (y[0] < equation.R0))
            {
                double hdiff = hupper - hlower;

                if (Math.Abs(hdiff) < 1e-7)
                {
                    RungeKutta.IntegrateStep(equation, y, dydx, hupper, yout, yerr);

                    Util.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));
                    return;
                }

                double hmid = (hupper + hlower) / 2;

                RungeKutta.IntegrateStep(equation, y, dydx, hmid, yout, yerr);

                Util.ToCartesian(yout[0], yout[1], yout[2], ref tempX, ref tempY, ref tempZ);
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

        protected Vector3 IntersectionSearch(Vector3 prevPoint, Vector3 velocity, SchwarzschildBlackHoleEquation equation)
        {
            float stepLow = 0, stepHigh = equation.StepSize;
            Vector3 newPoint = prevPoint;
            Vector3 tempVelocity;
            while (true)
            {
                float stepMid = (stepLow + stepHigh) / 2;
                newPoint = prevPoint;
                tempVelocity = velocity;
                equation.Function(ref newPoint, ref tempVelocity, stepMid);

                float distanceSqr = (newPoint - center).LengthSquared();
                if (Math.Abs(stepHigh - stepLow) < 0.00001)
                {
                    break;
                }
                if (distanceSqr < radiusSqr)
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
