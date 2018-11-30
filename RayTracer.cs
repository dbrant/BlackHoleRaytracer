using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Helpers;
using BlackHoleRaytracer.Mappings;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BlackHoleRaytracer
{
    public class RayTracer
    {

        public static bool DrawCheckeredHorizon = true;
        public static bool DrawCheckeredDisk = true;


        private KerrBlackHoleEquation equation;
        private int sizex;
        private int sizey;

        private int[] diskBitmap;
        private int diskBitmapWidth;
        private int[] skyBitmap;
        private int skyBitmapWidth;

        private double cameraTilt;
        private double cameraYaw;

        private bool trace;

        private SphericalMapping backgroundMap;
        private DiscMapping discMap;

        private static double PiOver2 = Math.PI / 2;
        
        public List<Tuple<double,double,double>> RayPoints { get; private set; }


        public RayTracer(KerrBlackHoleEquation equation, int sizex, int sizey,
            int[] diskBitmap, int[] skyBitmap, Bitmap diskImage, Bitmap skyImage,
            double cameraTilt, double cameraYaw, bool trace = false)
        {
            this.equation = equation;
            this.sizex = sizex;
            this.sizey = sizey;
            this.diskBitmap = diskBitmap;
            this.skyBitmap = skyBitmap;
            this.cameraTilt = cameraTilt;
            this.trace = trace;
            this.cameraYaw = cameraYaw;

            lock (skyImage)
            {
                backgroundMap = new SphericalMapping(skyImage.Width, skyImage.Height);
                skyBitmapWidth = skyImage.Width;
            }

            lock (diskImage)
            {
                discMap = new DiscMapping(equation.Rmstable, equation.Rdisk, diskImage.Width, diskImage.Height);
                diskBitmapWidth = diskImage.Width;
            }

            if (trace)
            {
                RayPoints = new List<Tuple<double, double, double>>();
            }
        }

        /// <summary>
        /// Shoot the ray through pixel (x1, y1).
        /// Returns color of the pixel.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        public unsafe Color Calculate(double x1, double y1)
        {
            Color? pixel = null;
            Color hitPixel;

            double htry = 0.5, escal = 1e11, hdid = 0.0, hnext = 0.0;

            double range = 0.0025 * equation.Rdisk / (sizex - 1);

            double yaw = cameraYaw * sizex;
            
            double* y = stackalloc double[equation.N];
            double* dydx = stackalloc double[equation.N];
            double* yscal = stackalloc double[equation.N];
            double* yPrev = stackalloc double[equation.N];

            int side;

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

                // Remember what side of the theta-plane we're currently on, so that we can detect
                // whether we've crossed the plane after stepping.
                side = y[1] > PiOver2 ? 1 : y[1] < PiOver2 ? -1 : 0;


                // Preserve the current Y, in case we need to converge on an intersection.
                MemHelper.memcpy((IntPtr)yPrev, (IntPtr)y, equation.N * sizeof(double));


                // Take the actual next step for the ray...
                hnext = RungeKuttaEngine.RKIntegrate(equation, y, dydx, htry, escal, yscal, out hdid);

                
                // Did we cross the theta (horizontal) plane?
                if ((y[1] - PiOver2) * side <= 0)
                {
                    // Restore Y to its previous values, and perform the binary intersection search.
                    MemHelper.memcpy((IntPtr)y, (IntPtr)yPrev, equation.N * sizeof(double));

                    IntersectionSearchDisk(y, dydx, hdid);
                    
                    // Is the ray within the accretion disk?
                    if ((y[0] <= equation.Rdisk) && (y[0] >= equation.Rmstable))
                    {
                        Color col;
                        if (DrawCheckeredDisk)
                        {
                            var m1 = DoubleMod(y[2], 1.04719);
                            bool foo = (m1 < 0.52359);
                            col = foo ? Color.BlueViolet : Color.MediumBlue;
                        }
                        else
                        {
                            int xPos, yPos;
                            // do mapping of texture image
                            discMap.Map(y[0], y[1], y[2], out xPos, out yPos);
                            col = Color.FromArgb(diskBitmap[yPos * diskBitmapWidth + xPos]);
                        }
                        
                        if (pixel != null)
                        {
                            pixel = ColorHelper.AddColor(col, pixel.Value);
                        }
                        else
                        {
                            pixel = col;
                        }
                        // don't break yet, just remember the color to 'tint' the texture later 
                    }
                }
                

                // Has the ray fallen past the horizon?
                if (y[0] < equation.Rhor)
                {
                    if (DrawCheckeredHorizon)
                    {
                        var m1 = DoubleMod(y[2], 1.04719); // Pi / 3
                        var m2 = DoubleMod(y[1], 1.04719); // Pi / 3
                        bool foo = (m1 < 0.52359) ^ (m2 < 0.52359); // Pi / 6
                        if (foo)
                        {
                            hitPixel = Color.Black;
                        }
                        else
                        {
                            hitPixel = Color.Green;
                        }
                    } else
                    {
                        hitPixel = Color.Black;
                    }
                    
                    // tint the color
                    if (pixel != null)
                    {
                        hitPixel = ColorHelper.AddColor(hitPixel, pixel.Value);
                    }
                    break;
                }

                // Has the ray escaped to infinity?
                if (y[0] > equation.R0)
                {
                    // Restore Y to its previous values, and perform the binary intersection search.
                    MemHelper.memcpy((IntPtr)y, (IntPtr)yPrev, equation.N * sizeof(double));

                    IntersectionSearchSky(y, dydx, hdid);

                    int xPos, yPos;
                    backgroundMap.Map(y[0], y[1], y[2], out xPos, out yPos);

                    hitPixel = Color.FromArgb(skyBitmap[yPos * skyBitmapWidth + xPos]);
                    if (pixel != null)
                    {
                        hitPixel = ColorHelper.AddColor(hitPixel, pixel.Value);
                    }
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
                    hitPixel = Color.Fuchsia;
                    break;
                }
            }
            
            return hitPixel;
        }

        private double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }

        /// <summary>
        /// Use Runge-Kutta steps to find intersection with horizontal plane of the scene.
        /// This is necessary to stop integrating when the ray hits the accretion disc.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        /// <param name="hupper"></param>
        private unsafe void IntersectionSearchDisk(double* y, double* dydx, double hupper)
        {
            unsafe
            {
                double hlower = 0.0;

                int side;
                if (y[1] > PiOver2)
                {
                    side = 1;
                }
                else if (y[1] < PiOver2)
                {
                    side = -1;
                }
                else
                {
                    // unlikely, but needs to handle a situation when ray hits the plane EXACTLY
                    return;
                }

                equation.Function(y, dydx);

                while ((y[0] > equation.Rhor) && (y[0] < equation.R0) && (side != 0))
                {
                    double* yout = stackalloc double[equation.N];
                    double* yerr = stackalloc double[equation.N];

                    double hdiff = hupper - hlower;

                    if (Math.Abs(hdiff) < 1e-7)
                    {
                        RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hupper, yout, yerr);

                        MemHelper.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));

                        return;
                    }

                    double hmid = (hupper + hlower) / 2;

                    RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hmid, yout, yerr);

                    if (side * (yout[1] - PiOver2) > 0)
                    {
                        hlower = hmid;
                    }
                    else
                    {
                        hupper = hmid;
                    }
                }
            }
        }

        private unsafe void IntersectionSearchSky(double* y, double* dydx, double hupper)
        {
            unsafe
            {
                double hlower = 0.0;
                equation.Function(y, dydx);

                while ((y[0] > equation.Rhor) && (y[0] < equation.R0 * 2))
                {
                    double* yout = stackalloc double[equation.N];
                    double* yerr = stackalloc double[equation.N];

                    double hdiff = hupper - hlower;

                    if (Math.Abs(hdiff) < 1e-7)
                    {
                        RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hupper, yout, yerr);

                        MemHelper.memcpy((IntPtr)y, (IntPtr)yout, equation.N * sizeof(double));
                        return;
                    }

                    double hmid = (hupper + hlower) / 2;

                    RungeKuttaEngine.RKIntegrateStep(equation, y, dydx, hmid, yout, yerr);

                    if (yout[0] < equation.R0)
                    {
                        hlower = hmid;
                    }
                    else
                    {
                        hupper = hmid;
                    }
                }
            }
        }
    }
}
