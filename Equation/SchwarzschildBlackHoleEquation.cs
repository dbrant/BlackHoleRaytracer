﻿using System;
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

        public float Function(ref Vector3 point, ref Vector3 velocity)
        {
            return Function(ref point, ref velocity, (point.Length() / 30f) * StepSize);
        }

        public float Function(ref Vector3 point, ref Vector3 velocity, float step)
        {
            //point += velocity * step;
            point.X += velocity.X * step;
            point.Y += velocity.Y * step;
            point.Z += velocity.Z * step;

            // this is the magical - 3/2 r^(-5) potential...
            //var accel = PotentialCoefficient * h2 * point / (float)Util.Pow25(point.LengthSquared());
            //velocity += accel * step;
            float lsq = point.LengthSquared();
            float f1 = PotentialCoefficient * h2 * step / (float)Util.Pow25(lsq);
            velocity.X += f1 * point.X;
            velocity.Y += f1 * point.Y;
            velocity.Z += f1 * point.Z;

            return lsq;
        }

        public unsafe void SetInitialConditions(ref Vector3 point, ref Vector3 velocity)
        {
            h2 = Vector3.Cross(point, velocity).LengthSquared();
        }

    }
}
