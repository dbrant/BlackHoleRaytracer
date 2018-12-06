using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class ReflectiveSphere : IHitable
    {
        protected double centerX;
        protected double centerY;
        protected double centerZ;
        protected double radius;
        protected Vector3 center;

        public ReflectiveSphere(double centerX, double centerY, double centerZ, double radius)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.centerZ = centerZ;
            this.radius = radius;
            center = new Vector3((float)centerX, (float)centerY, (float)centerZ);
        }

        public bool Hit(Vector3 point, double sqrNorm, Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, double r, double theta, double phi, ref Color color, ref bool stop, bool debug)
        {
            double distance = Math.Sqrt((point.X - centerX) * (point.X - centerX)
                + (point.Y - centerY) * (point.Y - centerY)
                + (point.Z - centerZ) * (point.Z - centerZ));
            if (distance < radius)
            {
                var colpoint = IntersectionSearch(prevPoint, velocity, equation);
                var impactFromCenter = Vector3.Normalize(colpoint - center);

                var normal = Vector3.Normalize(impactFromCenter);
                velocity = Vector3.Reflect(velocity, normal);
                equation.SetInitialConditions(ref colpoint, ref velocity);
                
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
            float stepLow = 0, stepHigh = equation.stepSize;
            Vector3 newPoint = prevPoint;
            Vector3 tempVelocity;
            while (true)
            {
                float stepMid = (stepLow + stepHigh) / 2;
                newPoint = prevPoint;
                tempVelocity = velocity;
                equation.Function(ref newPoint, ref tempVelocity, stepMid);

                double distance = Math.Sqrt((newPoint.X - centerX) * (newPoint.X - centerX)
                    + (newPoint.Y - centerY) * (newPoint.Y - centerY)
                    + (newPoint.Z - centerZ) * (newPoint.Z - centerZ));
                if (Math.Abs(distance - radius) < 0.0001)
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
        /*
        public unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            double tempX = 0, tempY = 0, tempZ = 0;
            Util.ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

            double distance = Math.Sqrt((tempX - centerX) * (tempX - centerX)
                + (tempY - centerY) * (tempY - centerY)
                + (tempZ - centerZ) * (tempZ - centerZ));
            if (distance < radius)
            {
                // remember the current values of Y, so that we can restore them after the intersection search.
                double* yCurrent = stackalloc double[equation.N];
                Util.memcpy((IntPtr)yCurrent, (IntPtr)y, equation.N * sizeof(double));

                // Restore Y to its previous values, and perform the binary intersection search.
                Util.memcpy((IntPtr)y, (IntPtr)prevY, equation.N * sizeof(double));
                
                IntersectionSearch(y, dydx, hdid, equation);
                
                // transform impact coordinates to cartesian coordinates relative to center of sphere.
                Util.ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

                var impact = new Vector3((float)tempX, (float)tempY, (float)tempZ);
                var normal = Vector3.Normalize(center - impact);


                //ToCartesian(prevY[0], prevY[1], prevY[2], ref tempX, ref tempY, ref tempZ);
                //var V = new Vector3((float)tempX, (float)tempY, (float)tempZ) - impactFromCenter;
                //var reflected = (V - (2 * (Vector3.Dot(V, normal)) * normal)) + impactFromCenter;

                //ToSpherical(reflected.X, reflected.Y, reflected.Z, ref y[0], ref y[1], ref y[2]);


                //Console.WriteLine(">>> " + y[0] + ", " + y[1] + ", " + y[2] + ", " + y[3] + ", " + y[4] + ", " + y[5]);


                // transform impact momentum vector to cartesian coordinates relative to center of sphere.
                Util.ToCartesian(prevY[4], prevY[5], prevY[3], ref tempX, ref tempY, ref tempZ);
                //ToCartesian(y[3], y[4], y[5], ref tempX, ref tempY, ref tempZ);

                Vector3 impactMomentum = new Vector3((float)tempX, (float)tempY, (float)tempZ);



                if (debug)
                    Console.WriteLine(">>> " + tempX + ", " + tempY + ", " + tempZ);




                //normal = Vector3.Normalize(new Vector3(-1, 0, 0));



                //var reflected = (impactMomentum - (2 * (Vector3.Dot(impactMomentum, normal)) * normal));
                var reflected = Vector3.Reflect(impactMomentum, normal);



                if (debug)
                    Console.WriteLine(">>> " + reflected.X + ", " + reflected.Y + ", " + reflected.Z);




                // ...and reset Y to its original current values.
                Util.memcpy((IntPtr)y, (IntPtr)yCurrent, equation.N * sizeof(double));



                y[3] = -y[3];
                y[4] = -y[4];
                y[5] = -y[5];
                //dydx[3] = -dydx[3];
                //dydx[4] = -dydx[4];
                //dydx[5] = -dydx[5];

                //Util.ToSpherical(reflected.X, reflected.Y, reflected.Z, ref y[4], ref y[5], ref y[3]);




                ///equation.Function(y, dydx);





                color = Color.Transparent;
                stop = false;
                return true;
            }
            return false;
        }

        protected unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
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
        */

    }
}
