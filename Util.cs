using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    public static class Util
    {

        static double minx = 0;
        static double miny = 0;
        static double minz = 0;
        static double maxx = 0;
        static double maxy = 0;
        static double maxz = 0;


        static double PI_2 = 1.57079632679;

        static int acosEntries = 1000000;
        static double[] acosCache;

        static int atanEntries = 1000000;
        static double[] atanCache;

        static int pow25Entries = 100000;
        static double[] pow25Cache;

        static double epsilon = 0.00001;
        static double nepsilon = -0.00001;


        public static void Precalculate()
        {
            acosCache = new double[acosEntries + 4];
            double step = 2.0 / (double)acosEntries;
            double x = -1.0;
            for (int i = 0; i < acosEntries; i++)
            {
                acosCache[i] = Math.Acos(x);
                x += step;
            }


            atanCache = new double[atanEntries + 4];
            step = 2.0 / (double)atanEntries;
            x = -1.0;
            for (int i = 0; i < atanEntries; i++)
            {
                atanCache[i] = Math.Atan(x);
                x += step;
            }

            pow25Cache = new double[pow25Entries];
            step = 1000.0 / (double)pow25Entries;
            x = 0.00000001;
            for (int i = 0; i < pow25Entries; i++)
            {
                pow25Cache[i] = Math.Pow(x, 2.5);
                x += step;
            }


        }


        public static double Acos(double x)
        {
            int i = (int)(((x + 1.0) / 2.0) * acosEntries);
            return acosCache[i];
        }

        public static double Atan(double x)
        {
            int i = (int)(((x + 1.0) / 2.0) * atanEntries);
            return atanCache[i];
        }

        public static double Pow25(double x)
        {
            int i = (int)(x * pow25Entries / 1000.0);
            return i < pow25Entries ? pow25Cache[i] : Math.Pow(x, 2.5);
        }

        public static double Atan2(double y, double x)
        {
            if (x != 0.0)
            {
                if (Math.Abs(x) > Math.Abs(y))
                {
                    var z = y / x;
                    if (x > 0.0)
                    {
                        // atan2(y,x) = atan(y/x) if x > 0
                        return Atan(z);
                    }
                    else if (y >= 0.0)
                    {
                        // atan2(y,x) = atan(y/x) + PI if x < 0, y >= 0
                        return Atan(z) + Math.PI;
                    }
                    else
                    {
                        // atan2(y,x) = atan(y/x) - PI if x < 0, y < 0
                        return Atan(z) - Math.PI;
                    }
                }
                else // Use property atan(y/x) = PI/2 - atan(x/y) if |y/x| > 1.
                {
                    var z = x / y;
                    if (y > 0.0)
                    {
                        // atan2(y,x) = PI/2 - atan(x/y) if |y/x| > 1, y > 0
                        return -Atan(z) + PI_2;
                    }
                    else
                    {
                        // atan2(y,x) = -PI/2 - atan(x/y) if |y/x| > 1, y < 0
                        return -Atan(z) - PI_2;
                    }
                }
            }
            else
            {
                if (y > 0.0f) // x = 0, y > 0
                {
                    return PI_2;
                }
                else if (y < 0.0) // x = 0, y < 0
                {
                    return -PI_2;
                }
            }
            return 0.0; // x,y
        }





        public static void ToCartesian(double r, double theta, double phi, ref double x, ref double y, ref double z)
        {
            x = r * Math.Cos(phi) * Math.Sin(theta);
            y = r * Math.Sin(phi) * Math.Sin(theta);
            z = r * Math.Cos(theta);
        }

        public static void ToSpherical(double x, double y, double z, ref double r, ref double theta, ref double phi)
        {
            r = Math.Sqrt(x * x + y * y + z * z);
            phi = Atan2(y, x);
            theta = Acos(z / r);
        }

        public static double DoubleMod(double n, double m)
        {
            double x = Math.Floor(n / m);
            return n - (m * x);
        }
        
        public static int[] getNativeTextureBitmap(Bitmap texture)
        {
            int[] textureBitmap = new int[texture.Width * texture.Height];
            BitmapData bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData.Scan0, textureBitmap, 0, textureBitmap.Length);
            texture.UnlockBits(bmpData);
            return textureBitmap;
        }

        public static Color AddColor(Color hitColor, Color tintColor)
        {
            if (tintColor == Color.Transparent)
            {
                return hitColor;
            }
            float brightness = tintColor.GetBrightness();
            return Color.FromArgb(
                    Cap((int)((1 - brightness) * hitColor.R) + CapMin(tintColor.R, 0) * 255 / 205, 255),
                    Cap((int)((1 - brightness) * hitColor.G) + CapMin(tintColor.G, 0) * 255 / 205, 255),
                    Cap((int)((1 - brightness) * hitColor.B) + CapMin(tintColor.B, 0) * 255 / 205, 255)
                );
        }
        
        private static int Cap(int x, int max)
        {
            return x > max ? max : x;
        }

        private static int CapMin(int x, int min)
        {
            return x < min ? min : x;
        }

        [DllImport("msvcrt.dll", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);
    }
}
