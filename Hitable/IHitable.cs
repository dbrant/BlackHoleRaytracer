using System.Drawing;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public interface IHitable
    {
        unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop);
    }
}
