using System;

namespace BlackHoleRaytracer.Mappings
{
    /// <summary>
    /// Maps flat texture onto a spherical surface expressed in spherical coordinates.
    /// </summary>
    class SphericalMapping : IMapping
    {
        private int SizeX;
        private int SizeY;

        public SphericalMapping(int sizex, int sizey)
        {
            this.SizeX = sizex;
            this.SizeY = sizey;
        }

        public void Map(double r, double theta, double phi, out int x, out int y)
        {
            double textureScale = 1.0;

            x = (int)(((phi * textureScale) / (2 * Math.PI)) * SizeX) % SizeX;
            y = (int)((theta * textureScale / Math.PI) * SizeY) % SizeY;

            if (x < 0) { x = this.SizeX + x; }
            if (y < 0) { y = this.SizeY + y; }
        }
    }
}
