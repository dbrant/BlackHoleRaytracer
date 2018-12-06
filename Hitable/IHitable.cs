using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public interface IHitable
    {
        unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool debug);

        bool Hit(Vector3 point, Vector3 prevPoint, double pointSqrNorm, double r, double theta, double phi, ref Color color, ref bool stop, bool debug);
    }
}
