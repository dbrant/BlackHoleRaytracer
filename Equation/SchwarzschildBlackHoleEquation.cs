using System;
using System.Numerics;

namespace BlackHoleRaytracer.Equation
{
    /// <summary>
    /// Encapsulation of the Schwarzschild model, borrowed from:
    /// https://github.com/rantonels/starless
    /// 
    /// Note: this class is not thread-safe - setting initial conditions
    /// will overwrite certain private variables.
    /// </summary>
    public class SchwarzschildBlackHoleEquation
    {
        private const float DefaultStepSize = 0.16f;

        private float h2;
        public float StepSize { get; }

        /// <summary>
        /// Multiplier for the potential, ranging from 0 for no curvature, to -1.5 for full curvature.
        /// </summary>
        public float PotentialCoefficient { get; }

        private double[] Y = new double[6];
        private double[] F = new double[6];
        private double[] K1 = new double[6];
        private double[] K2 = new double[6];
        private double[] K3 = new double[6];
        private double[] K4 = new double[6];

        public SchwarzschildBlackHoleEquation(float PotentialCoefficient)
        {
            StepSize = DefaultStepSize;
            this.PotentialCoefficient = PotentialCoefficient;
        }

        public SchwarzschildBlackHoleEquation(SchwarzschildBlackHoleEquation other)
        {
            StepSize = other.StepSize;
            PotentialCoefficient = other.PotentialCoefficient;
        }

        public void Function(ref Vector3 point, ref Vector3 velocity)
        {
            Function(ref point, ref velocity, (point.Length() / 30f) * StepSize);
        }

        public void Function(ref Vector3 point, ref Vector3 velocity, float step)
        {
            point += velocity * step;

            // this is the magical - 3/2 r^(-5) potential...
            var accel = PotentialCoefficient * h2 * point / (float)Math.Pow(Util.SqrNorm(point), 2.5);
            velocity += accel * step;

            /*
            //...if we decide to go the RK4 route:
            var rkstep = step;

            Y[0] = point.X;
            Y[1] = point.Y;
            Y[2] = point.Z;
            Y[3] = velocity.X;
            Y[4] = velocity.Y;
            Y[5] = velocity.Z;
            RK4f(Y, K1, h2);
            RK4fAdd(0.5 * rkstep, Y, K1, F);
            RK4f(F, K2, h2);
            RK4fAdd(0.5 * rkstep, Y, K2, F);
            RK4f(F, K3, h2);
            RK4fAdd(rkstep, Y, K3, F);
            RK4f(F, K4, h2);

            F[0] = rkstep / 6 * (K1[0] + 2 * K2[0] + 2 * K3[0] + K4[0]);
            F[1] = rkstep / 6 * (K1[1] + 2 * K2[1] + 2 * K3[1] + K4[1]);
            F[2] = rkstep / 6 * (K1[2] + 2 * K2[2] + 2 * K3[2] + K4[2]);
            F[3] = rkstep / 6 * (K1[3] + 2 * K2[3] + 2 * K3[3] + K4[3]);
            F[4] = rkstep / 6 * (K1[4] + 2 * K2[4] + 2 * K3[4] + K4[4]);
            F[5] = rkstep / 6 * (K1[5] + 2 * K2[5] + 2 * K3[5] + K4[5]);

            velocity.X += (float)F[3];
            velocity.Y += (float)F[4];
            velocity.Z += (float)F[5];

            point.X += (float)F[0];
            point.Y += (float)F[1];
            point.Z += (float)F[2];
            */

        }

        public unsafe void SetInitialConditions(ref Vector3 point, ref Vector3 velocity)
        {
            h2 = Util.SqrNorm(Vector3.Cross(point, velocity));
        }

        private void RK4f(double[] y, double[] f, double h2)
        {
            f[0] = y[3];
            f[1] = y[4];
            f[2] = y[5];
            var d = Math.Pow(y[0] * y[0] + y[1] * y[1] + y[2] * y[2], 2.5);
            f[3] = -1.5 * h2 * y[0] / d;
            f[4] = -1.5 * h2 * y[1] / d;
            f[5] = -1.5 * h2 * y[2] / d;
        }

        private void RK4fAdd(double step, double[] y, double[] k, double[] o)
        {
            o[0] = y[0] + step * k[0];
            o[1] = y[1] + step * k[1];
            o[2] = y[2] + step * k[2];
            o[3] = y[3] + step * k[3];
            o[4] = y[4] + step * k[4];
            o[5] = y[5] + step * k[5];
        }
    }
}
