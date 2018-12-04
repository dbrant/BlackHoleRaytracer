using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class ReflectiveSphere : Sphere
    {
        public ReflectiveSphere(double centerX, double centerY, double centerZ, double radius)
            : base(centerX, centerY, centerZ, radius, null, false)
        { }

        public override unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool trace)
        {
            double tempX = 0, tempY = 0, tempZ = 0;
            ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);
            
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


                //Console.WriteLine(">>> " + y[0] + ", " + y[1] + ", " + y[2] + ", " + y[3] + ", " + y[4] + ", " + y[5]);


                IntersectionSearch(y, dydx, hdid, equation);


                //Console.WriteLine(">>> " + y[0] + ", " + y[1] + ", " + y[2] + ", " + y[3] + ", " + y[4] + ", " + y[5]);

                //Console.WriteLine(">>> " + dydx[0] + ", " + dydx[1] + ", " + dydx[2] + ", " + dydx[3] + ", " + dydx[4] + ", " + dydx[5]);


                // transform impact coordinates to cartesian coordinates relative to center of sphere.
                ToCartesian(y[0], y[1], y[2], ref tempX, ref tempY, ref tempZ);

                var impact = new Vector3((float)tempX, (float)tempY, (float)tempZ);
                var center = new Vector3((float)centerX, (float)centerY, (float)centerZ);
                var normal = Vector3.Normalize(impact - center);

                
                //ToCartesian(prevY[0], prevY[1], prevY[2], ref tempX, ref tempY, ref tempZ);
                //var V = new Vector3((float)tempX, (float)tempY, (float)tempZ) - impactFromCenter;
                //var reflected = (V - (2 * (Vector3.Dot(V, normal)) * normal)) + impactFromCenter;

                //ToSpherical(reflected.X, reflected.Y, reflected.Z, ref y[0], ref y[1], ref y[2]);


                //Console.WriteLine(">>> " + y[0] + ", " + y[1] + ", " + y[2] + ", " + y[3] + ", " + y[4] + ", " + y[5]);


                // transform impact momentum vector to cartesian coordinates relative to center of sphere.
                ToCartesian(prevY[3], prevY[4], prevY[5], ref tempX, ref tempY, ref tempZ);
                //ToCartesian(y[3], y[4], y[5], ref tempX, ref tempY, ref tempZ);

                Vector3 impactMomentum = new Vector3((float)tempX, (float)tempY, (float)tempZ);



                if (trace)
                Console.WriteLine(">>> " + tempX + ", " + tempY + ", " + tempZ);




                normal = Vector3.Normalize(new Vector3(1, 1, -1));

                var reflected = (impactMomentum - (2 * (Vector3.Dot(impactMomentum, normal)) * normal));


                if (trace)
                Console.WriteLine(">>> " + reflected.X + ", " + reflected.Y + ", " + reflected.Z);




                // ...and reset Y to its original current values.
                Util.memcpy((IntPtr)y, (IntPtr)yCurrent, equation.N * sizeof(double));



                y[3] = -1; // y[3];
                y[4] = -1; // y[4];
                y[5] = 1; // y[5];
                //dydx[3] = -dydx[3];
                //dydx[4] = -dydx[4];
                //dydx[5] = -dydx[5];

                //ToSpherical(reflected.X, reflected.Y, reflected.Z, ref y[3], ref y[4], ref y[5]);



                ///equation.Function(y, dydx);





                color = Color.Transparent;
                stop = false;
                return true;
            }
            return false;
        }

    }
}
