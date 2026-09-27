using System.Numerics;
using System.Runtime.CompilerServices;
using ILGPU.Algorithms;

namespace BlackHoleRaytracer.Gpu
{
    /// <summary>
    /// Minimal 3-component vector for use inside GPU kernels. System.Numerics.Vector3
    /// is implemented with CPU SIMD intrinsics, which ILGPU cannot compile.
    /// </summary>
    public struct Float3(float x, float y, float z)
    {
        public float X = x;
        public float Y = y;
        public float Z = z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

        public static Float3 operator +(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Float3 operator -(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Float3 operator *(Float3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

        public static Float3 operator /(Float3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);

        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Float3 Cross(Float3 a, Float3 b)
        {
            return new(a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public static Float3 Normalize(Float3 a) => a / XMath.Sqrt(a.LengthSquared());

        public static Float3 Reflect(Float3 v, Float3 normal) => v - normal * (2f * Dot(v, normal));

        public static implicit operator Float3(Vector3 v) => new(v.X, v.Y, v.Z);
    }
}
