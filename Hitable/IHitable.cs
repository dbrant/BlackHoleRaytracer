using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Gpu;

namespace BlackHoleRaytracer.Hitable
{
    public interface IHitable
    {
        unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug);

        bool Hit(ref Vector3 point, double sqrNorm, ref Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, ref Color color, ref bool stop, bool debug);

        /// <summary>
        /// Convert this hitable into its flattened form for the GPU raytracer.
        /// </summary>
        GpuHitable ToGpu(GpuSceneBuilder builder);
    }
}
