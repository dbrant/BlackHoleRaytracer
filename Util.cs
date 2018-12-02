using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer
{
    public class Util
    {
        public static int[] getNativeTextureBitmap(Bitmap texture)
        {
            int[] textureBitmap = new int[texture.Width * texture.Height];
            BitmapData bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData.Scan0, textureBitmap, 0, textureBitmap.Length);
            texture.UnlockBits(bmpData);
            return textureBitmap;
        }

        [DllImport("msvcrt.dll", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

        public static Color AddColor(Color hitColor, Color tintColor)
        {
            float brightness = tintColor.GetBrightness();
            var result = Color.FromArgb(
                    (int)Cap((int)((1 - brightness) * hitColor.R) + CapMin(tintColor.R - 20, 0) * 255 / 205, 255),
                    (int)Cap((int)((1 - brightness) * hitColor.G) + CapMin(tintColor.G - 20, 0) * 255 / 205, 255),
                    (int)Cap((int)((1 - brightness) * hitColor.B) + CapMin(tintColor.B - 20, 0) * 255 / 205, 255)
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
    }
}
