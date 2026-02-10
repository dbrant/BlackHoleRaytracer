using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class TexturedSphere : Sphere
    {
        private readonly SphericalMapping textureMap;
        private readonly int textureWidth;
        private readonly int[] textureBitmap;
        private double textureOffset = 0;

        public TexturedSphere(double centerX, double centerY, double centerZ, float radius, Bitmap texture)
            : base(centerX, centerY, centerZ, radius)
        {
            textureMap = new SphericalMapping(texture.Width, texture.Height);
            textureWidth = texture.Width;
            textureBitmap = Util.getNativeTextureBitmap(texture);
        }

        public TexturedSphere(double centerX, double centerY, double centerZ, float radius, int[] bitmap, int width, int height)
            : base(centerX, centerY, centerZ, radius)
        {
            textureMap = new SphericalMapping(width, height);
            textureWidth = width;
            textureBitmap = bitmap;
        }

        public TexturedSphere SetTextureOffset(double offset)
        {
            textureOffset = offset;
            return this;
        }

        protected override Color GetColor(double r, double theta, double phi)
        {
            int xPos, yPos;
            textureMap.Map(r, theta, phi + textureOffset, out xPos, out yPos);
            return Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
        }
    }
}
