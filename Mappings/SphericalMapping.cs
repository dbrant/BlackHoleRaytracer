using System;

namespace BlackHoleRaytracer.Mappings
{
    /// <summary>
    /// Maps flat texture onto a spherical surface expressed in spherical coordinates.
    /// </summary>
    class SphericalMapping
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
            x = (int)((phi / (2 * Math.PI)) * SizeX) % SizeX;
            y = (int)((theta / Math.PI) * SizeY) % SizeY;

            if (x < 0) { x = SizeX + x; }
            if (y < 0) { y = SizeY + y; }
        }

        public void MapCartesian(double x, double y, double z, out int u, out int v)
        {
            u = (int)((0.5 + Math.Atan2(z, x) / (2 * Math.PI)) * SizeX) % SizeX;
            v = (int)((0.5 - (Math.Asin(y) / Math.PI)) * SizeY) % SizeY;
            if (u < 0) { u = SizeX + u; }
            if (v < 0) { v = SizeY + v; }
        }
    }
}
