using System;
using System.Drawing;
using System.Drawing.Imaging;
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
            phi = Math.Atan(y / x);
            theta = Math.Acos(z / r);
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
            float brightness = tintColor.GetBrightness();
            var result = Color.FromArgb(
                    Cap((int)((1 - brightness) * hitColor.R) + CapMin(tintColor.R - 20, 0) * 255 / 205, 255),
                    Cap((int)((1 - brightness) * hitColor.G) + CapMin(tintColor.G - 20, 0) * 255 / 205, 255),
                    Cap((int)((1 - brightness) * hitColor.B) + CapMin(tintColor.B - 20, 0) * 255 / 205, 255)
                );
            return result;
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
