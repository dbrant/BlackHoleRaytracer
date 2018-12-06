using System;
using System.Drawing;
using System.Numerics;
using BlackHoleRaytracer.Mappings;

namespace BlackHoleRaytracer.Hitable
{
    public class TexturedSphere : Sphere
    {
        private SphericalMapping textureMap;
        private int textureWidth;
        private int[] textureBitmap;
        private double textureOffset = 0;

        public TexturedSphere(double centerX, double centerY, double centerZ, double radius, Bitmap texture)
            : base(centerX, centerY, centerZ, radius)
        {
            textureMap = new SphericalMapping(texture.Width, texture.Height);
            textureWidth = texture.Width;
            textureBitmap = Util.getNativeTextureBitmap(texture);
        }

        public TexturedSphere SetTextureOffset(double offset)
        {
            textureOffset = offset;
            return this;
        }

        protected override Color GetColor(double r, double theta, double phi)
        {
            // and retransform into cartesian coordinates
            double tempX = 0, tempY = 0, tempZ = 0;
            Util.ToCartesian(r, theta, phi + textureOffset, ref tempX, ref tempY, ref tempZ);
            var impactFromCenter = new Vector3((float)tempX, (float)tempY, (float)tempZ);

            int xPos, yPos;
            textureMap.MapCartesian(impactFromCenter.X, impactFromCenter.Y, impactFromCenter.Z, out xPos, out yPos);

            return Color.FromArgb(textureBitmap[yPos * textureWidth + xPos]);
        }
    }
}
