using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BlackHoleRaytracer.Helpers
{
    public class MemHelper
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
    }
}
