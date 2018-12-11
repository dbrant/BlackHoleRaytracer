using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    public static class Util
    {
        public static void ToCartesian(double r, double theta, double phi, ref double x, ref double y, ref double z)
        {
            x = r * Math.Cos(phi) * Math.Sin(theta);
            y = r * Math.Sin(phi) * Math.Sin(theta);
            z = r * Math.Cos(theta);
        }

        public static void ToSpherical(double x, double y, double z, ref double r, ref double theta, ref double phi)
        {
            r = Math.Sqrt(x * x + y * y + z * z);
            phi = Math.Atan2(y, x);
            theta = Math.Acos(z / r);
        }

        public static void ToSpherical(Vector3 v, ref double r, ref double theta, ref double phi)
        {
            r = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            phi = Math.Atan2(v.Y, v.X);
            theta = Math.Acos(v.Z / r);
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
