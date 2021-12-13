using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class Sky : IHitable
    {
        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;
        private double textureOffset = 0;
        private double radius;
        private double radiusSqr;

        public Sky(Bitmap texture, double radius)
        {
            this.radius = radius;
            radiusSqr = radius * radius;
            if (texture != null)
            {
                textureMap = new SphericalMapping(texture.Width, texture.Height);
                textureWidth = texture.Width;
                textureBitmap = Util.getNativeTextureBitmap(texture);
            }
        }

        public Sky SetTextureOffset(double offset)
        {
            textureOffset = offset;
            return this;
        }

        public bool Hit(ref Vector3 point, double sqrNorm, ref Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, ref Color color, ref bool stop, bool debug)
        {
            // Has the ray escaped to infinity?
            if (sqrNorm > radiusSqr)
            {
                int xPos, yPos;
                double tempR = 0.0, tempTheta = 0.0, tempPhi = 0.0;
                Util.ToSpherical(point.X, point.Y, point.Z, ref tempR, ref tempTheta, ref tempPhi);
                textureMap.Map(tempR, tempTheta, tempPhi, out xPos, out yPos);
                
                color = Util.AddColor(Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]), color);
                stop = true;
                return true;
            }
            return false;
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
                textureMap.Map(y[0], y[1], y[2] + textureOffset, out xPos, out yPos);

                color = Util.AddColor(Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]), color);
                stop = true;
                return true;
            }
            return false;
        }

        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper, KerrBlackHoleEquation equation)
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

                double distance = newPoint.LengthSquared();
                if (Math.Abs(stepHigh - stepLow) < 0.00001)
                {
                    break;
                }
                if (distance > radius)
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
