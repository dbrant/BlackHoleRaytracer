using System;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        private const float StepSizeOver30 = DefaultStepSize / 30f;

        private float h2;
        private float potH2; // PotentialCoefficient * h2, cached per ray
        public float StepSize { get; }

        /// <summary>
        /// Multiplier for the potential, ranging from 0 for no curvature, to -1.5 for full curvature.
        /// </summary>
        public float PotentialCoefficient { get; }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Function(ref Vector3 point, ref Vector3 velocity)
        {
            // Compute lsq first, then derive length from it (one sqrt instead of two).
            float lsq = point.X * point.X + point.Y * point.Y + point.Z * point.Z;
            float step = MathF.Sqrt(lsq) * StepSizeOver30;

            point += velocity * step;

            // Recompute lsq after advancing the point.
            lsq = point.X * point.X + point.Y * point.Y + point.Z * point.Z;

            // x^2.5 = x * x * sqrt(x), avoiding the Pow25 lookup table.
            float sqrtLsq = MathF.Sqrt(lsq);
            float f1 = potH2 * step / (lsq * lsq * sqrtLsq);
            velocity += f1 * point;

            return lsq;
        }

        public float Function(ref Vector3 point, ref Vector3 velocity, float step)
        {
            point += velocity * step;

            float lsq = point.X * point.X + point.Y * point.Y + point.Z * point.Z;
            float sqrtLsq = MathF.Sqrt(lsq);
            float f1 = potH2 * step / (lsq * lsq * sqrtLsq);
            velocity += f1 * point;

            return lsq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInitialConditions(ref Vector3 point, ref Vector3 velocity)
        {
            h2 = Vector3.Cross(point, velocity).LengthSquared();
            potH2 = PotentialCoefficient * h2;
        }

    }
}
