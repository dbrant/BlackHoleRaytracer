using System;
using System.Drawing;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class TexturedDisk(double radiusInner, double radiusOuter, Bitmap texture) : Disk(radiusInner, radiusOuter)
    {
        private readonly DiscMapping textureMap = new(radiusInner, radiusOuter, texture.Width, texture.Height);
        private readonly int textureWidth = texture.Width;
        private readonly int[] textureBitmap = Util.getNativeTextureBitmap(texture);

        protected override Color GetColor(int side, double r, double theta, double phi)
        {
            textureMap.Map(r, theta, phi, out int xPos, out int yPos);
            return Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
        }
    }
}
