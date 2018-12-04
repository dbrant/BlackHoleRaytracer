using System;
using System.Drawing;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class TexturedDisk : Disk
    {
        private DiscMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;

        public TexturedDisk(double radiusInner, double radiusOuter, Bitmap texture)
            : base(radiusInner, radiusOuter)
        {
            textureMap = new DiscMapping(radiusInner, radiusOuter, texture.Width, texture.Height);
            textureWidth = texture.Width;
            textureBitmap = Util.getNativeTextureBitmap(texture);
        }

        protected override Color GetColor(int side, double r, double theta, double phi)
        {
            int xPos, yPos;
            textureMap.Map(r, theta, phi, out xPos, out yPos);
            return Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
        }
    }
}
