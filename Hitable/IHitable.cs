using System.Drawing;
using BlackHoleRaytracer.Equation;

namespace BlackHoleRaytracer.Hitable
{
    public abstract class IHitable
    {
        public abstract unsafe bool Hit(double* y, double* prevY, double* dydx, double hdid, KerrBlackHoleEquation equation, ref Color color, ref bool stop, bool trace);
    }
}
