using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BlackHoleRaytracer
{
    public class RayTracer
    {
        private KerrBlackHoleEquation equation;
        private int sizex;
        private int sizey;
        
        private double cameraTilt;
        private double cameraYaw;
        
        private List<IHitable> hitables;

        private bool trace;
        public List<Tuple<double,double,double>> RayPoints { get; private set; }


        public RayTracer(KerrBlackHoleEquation equation, int sizex, int sizey, List<IHitable> hitables,
            double cameraTilt, double cameraYaw, bool trace = false)
        {
            this.equation = equation;
            this.sizex = sizex;
            this.sizey = sizey;
            this.cameraTilt = cameraTilt;
            this.trace = trace;
            this.cameraYaw = cameraYaw;
            this.hitables = hitables;
            
            if (trace)
            {
                RayPoints = new List<Tuple<double, double, double>>();
            }
        }

        /// <summary>
        /// Shoot the ray through pixel (x1, y1).
        /// </summary>
        /// <returns>Color of the pixel at the requested coordinates.</returns>
        public unsafe Color Calculate(double x1, double y1)
        {
            Color? color = null;
            Color tempColor = Color.Black;

            double htry = 0.5, escal = 1e11, hdid = 0.0, hnext = 0.0;

            double range = 0.04 / (sizex - 1);

            double yaw = cameraYaw * sizex;
            
            double* y = stackalloc double[equation.N];
            double* dydx = stackalloc double[equation.N];
            double* yscal = stackalloc double[equation.N];
            double* yPrev = stackalloc double[equation.N];
            
            double tiltSin = Math.Sin((cameraTilt / 180) * Math.PI);
            double tiltCos = Math.Cos((cameraTilt / 180) * Math.PI);

            double xRot = x1 - (sizex + 1) / 2 - yaw;
            double yRot = y1 - (sizey + 1) / 2;
            

            equation.SetInitialConditions(y, dydx,
                (int)(xRot * tiltCos - yRot * tiltSin) * range,
                (int)(yRot * tiltCos + xRot * tiltSin) * range
            );
            

            // if tracing on, store the initial point
            if (trace)
            {
                RayPoints.Clear();
                RayPoints.Add(new Tuple<double, double, double>(y[0], y[1], y[2]));
            }

            bool stop = false;
            int rCount = 0;

            while (true)
            {
                equation.Function(y, dydx);

                // y[0] - radial position
                // y[1] - theta (vertical) angular position
                // y[2] - phi (horizontal) angular position

                for (int i = 0; i < equation.N; i++)
                {
                    yscal[i] = Math.Abs(y[i]) + Math.Abs(dydx[i] * htry) + 1.0e-3;
                }

                // Preserve the current Y, in case we need to converge on an intersection.
                Util.memcpy((IntPtr)yPrev, (IntPtr)y, equation.N * sizeof(double));
                
                // Take the actual next step for the ray...
                hnext = RungeKutta.Integrate(equation, y, dydx, htry, escal, yscal, out hdid);

                // Check if the ray hits anything
                foreach (var hitable in hitables)
                {
                    stop = false;
                    if (hitable.Hit(y, yPrev, dydx, hdid, equation, ref tempColor, ref stop))
                    {
                        if (color != null)
                        {
                            color = Util.AddColor(tempColor, color.Value);
                        }
                        else
                        {
                            color = tempColor;
                        }
                        if (stop)
                        {
                            // The ray has found its stopping point (or rather its starting point).
                            break;
                        }
                    }
                }
                if (stop)
                {
                    break;
                }
                

                // if tracing on, store the calculated point
                if (trace)
                {
                    RayPoints.Add(new Tuple<double, double, double>(y[0], y[1], y[2]));
                }

                htry = hnext;

                if (rCount++ > 1000000) // failsafe...
                {
                    Console.WriteLine("Error - solution not converging!");
                    color = Color.Fuchsia;
                    break;
                }
            }

            return (Color)color;
        }
        
    }
}
