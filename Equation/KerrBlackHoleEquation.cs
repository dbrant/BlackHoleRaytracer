using System;

namespace BlackHoleRaytracer.Equation
{
    /// <summary>
    /// Encapsulation of the Kerr model, borrowed from:
    /// http://locklessinc.com/articles/raytracing/
    /// 
    /// Note: this class is not thread-safe - setting initial conditions
    /// will overwrite certain private variables.
    /// </summary>
    public class KerrBlackHoleEquation : IODESystem
    {
        // Motion constants
        private double L; // angular momentum in phi direction
        private double kappa; // Carter's constant's element
        private double angularMomentum;
        private double a2; // a-squared

        // Initial conditions:
        // Ray starting location in Boyer-Lindquist coordinates
        public double R0 { get; private set; }
        private double theta0;
        private double phi0;
        
        /// <summary>
        /// Radius of event horizon.
        /// </summary>
        public double Rhor { get; private set; }
        
        /// <summary>
        /// Radius of innermost stable orbit.
        /// </summary>
        public double Rmstable { get; private set; }
        
        public KerrBlackHoleEquation(double rDistance, double thetaDegrees, double phiDegrees, double angularMomentum)
        {
            R0 = rDistance;
            theta0 = (Math.PI / 180.0) * thetaDegrees;
            phi0 = (Math.PI / 180.0) * phiDegrees;
            this.angularMomentum = angularMomentum;
            Init();
        }

        public KerrBlackHoleEquation(KerrBlackHoleEquation other)
        {
            R0 = other.R0;
            theta0 = other.theta0;
            phi0 = other.phi0;
            angularMomentum = other.angularMomentum;
            Init();
        }

        private void Init()
        {
            a2 = angularMomentum * angularMomentum;
            Rhor = 1.0 + Math.Sqrt(1.0 - a2) + 1e-5;
            Rmstable = InnermostStableOrbit();
        }

        /// <summary>
        /// Number of equations in the set
        /// </summary>
        public int N { get { return 6; } }

        /// <summary>
        /// Perform the actual function (geodesic) on the vector Y, and output the results to dYdX.
        /// 
        /// y[0] = r
        /// y[1] = theta
        /// y[2] = phi
        /// y[3] = momentum in r direction
        /// y[4] = momentum in theta direction
        /// y[5] = momentum in phi direction
        /// </summary>
        public unsafe void Function(double* y, double* dydx)
        {
            double r, theta, pr, ptheta;

            r = y[0];
            theta = y[1];
            pr = y[4];
            ptheta = y[5];

            double r2 = r * r;
            double twor = 2.0 * r;

            double sintheta, costheta;
            sintheta = Math.Sin(theta);
            costheta = Math.Cos(theta);
            double cos2 = costheta * costheta;
            double sigma = r2 + a2 * cos2;
            double delta = r2 - twor + a2;
            double sd = sigma * delta;
            double siginv = 1.0 / sigma;
            double bot = 1.0 / sd;
            
            if (sintheta < 1e-8) { sintheta = 1e-8; }
            double sin2 = sintheta * sintheta;

            dydx[0] = -pr * delta * siginv;
            dydx[1] = -ptheta * siginv;
            dydx[2] = -(twor * angularMomentum + (sigma - twor) * L / sin2) * bot;
            //dydx[3] = -(1.0 + (twor * (r2 + a2) - twor * angularMomentum * L) * bot);
            dydx[4] = -(((r - 1.0) * (-kappa) + twor * (r2 + a2) - 2.0 * angularMomentum * L) * bot - 2.0 * pr * pr * (r - 1.0) * siginv);
            dydx[5] = -sintheta * costheta * (L * L / (sin2 * sin2) - a2) * siginv;
        }

        /// <summary>
        /// Set initial conditions for a starting point of the ray.
        /// </summary>
        /// <param name="y0">Vector of coefficients to be initialized.</param>
        /// <param name="ydot0">Vector of coefficients that will receive the initial call to the geodesic.</param>
        /// <param name="x">x-coordinate of the ray</param>
        /// <param name="y">y-coordinate of the ray</param>
        public unsafe void SetInitialConditions(double* y0, double* ydot0, double x, double y)
        {
            y0[0] = R0;
            y0[1] = theta0;
            y0[2] = phi0;

            double sintheta, costheta;
            sintheta = Math.Sin(theta0);
            costheta = Math.Cos(theta0);
            double cos2 = costheta * costheta;
            double sin2 = sintheta * sintheta;

            double rdot0 = Math.Cos(y) * Math.Cos(x);
            double thetadot0 = Math.Sin(y);

            double r2 = R0 * R0;
            double sigma = r2 + a2 * cos2;
            double delta = r2 - 2.0 * R0 + a2;
            double s1 = sigma - 2.0 * R0;

            y0[4] = rdot0 * sigma / delta;
            y0[5] = thetadot0 * sigma;

            double sinx = Math.Sin(x);
            if (sinx < 1e-8 && sinx > -1e-8) { sinx = 1e-8; }

            double phidot0 = Math.Cos(y) * sinx / Math.Sin(theta0);
            double energy2 = s1 * (rdot0 * rdot0 / delta + thetadot0 * thetadot0) + delta * sin2 * phidot0 * phidot0;
            double energy = Math.Sqrt(energy2);

            // rescale
            y0[4] = y0[4] / energy;
            y0[5] = y0[5] / energy;

            // Angular Momentum with E = 1
            L = ((sigma * delta * phidot0 - 2.0 * angularMomentum * R0 * energy) * sin2 / s1) / energy;

            kappa = y0[5] * y0[5] + a2 * sin2 + L * L / sin2;

            // Hack - make sure everything is normalized correctly by a call to geodesic
            Function(y0, ydot0);
        }
        
        private double InnermostStableOrbit()
        {
            double z1 = 1 + Math.Pow(1 - a2, 1.0 / 3.0) * (Math.Pow(1 + angularMomentum, 1.0 / 3.0) + Math.Pow(1 - angularMomentum, 1.0 / 3.0));
            double z2 = Math.Sqrt(3 * a2 + z1 * z1);
            return 3 + z2 - Math.Sqrt((3 - z1) * (3 + z1 + 2 * z2));
        }
    }
}
