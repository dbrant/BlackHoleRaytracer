using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public interface IHitable
    {
        unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug);

        bool Hit(ref Vector3 point, double sqrNorm, Vector3 prevPoint, double prevSqrNorm, ref Vector3 velocity, SchwarzschildBlackHoleEquation equation, double r, double theta, double phi, ref Color color, ref bool stop, bool debug);
    }
}
