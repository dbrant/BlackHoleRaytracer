﻿using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public class Sphere : IHitable
    {
        protected double centerX;
        protected double centerY;
        protected double centerZ;
        protected double radius;
        protected Vector3 center;
        
        public Sphere(double centerX, double centerY, double centerZ, double radius)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.centerZ = centerZ;
            this.radius = radius;
            center = new Vector3((float)centerX, (float)centerY, (float)centerZ);
        }

        protected virtual Color GetColor(double r, double theta, double phi)
        {
            return Color.White;
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

                color = GetColor(tempR, tempTheta, tempPhi);
                
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
    }
}
