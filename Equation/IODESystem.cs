
namespace BlackHoleRaytracer.Equation
{
    /// <summary>
    /// Interface that defines a system of Ordinary Differential Equations in a way 
    /// that makes them integrable using the Runge-Kutta algorithm.
    /// </summary>
    public interface IODESystem
    {
        /// <summary>
        /// Calculate the value of derivatives (dydx) from a set of variable values (y array)
        /// </summary>
        unsafe void Function(double* y, double* dydx);

        /// <summary>
        /// Number of equations in the ODE system
        /// </summary>
        int N { get; }

    }
}
