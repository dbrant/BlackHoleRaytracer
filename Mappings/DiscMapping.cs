using System;

namespace BlackHoleRaytracer.Mappings
{
    /// <summary>
    /// Mapping of spherical coordinates of a disc onto a flat texture.
    /// </summary>
    class DiscMapping(double rMin, double rMax, int sizex, int sizey)
    {
        private readonly double rMin = rMin;
        private readonly double rMax = rMax;
        private readonly int sizeX = sizex;
        private readonly int sizeY = sizey;

        public void Map(double r, double theta, double phi, out int x, out int y)
        {
            if (r < rMin || r > rMax)
            {
                x = 0;
                y = sizeY;
            }

            x = (int)(phi / (2 * Math.PI) * sizeX) % sizeX;
            if (x < 0) { x = sizeX + x; }
            y = (int)((r - rMin) / (rMax - rMin) * sizeY);
            if (y > sizeY - 1) { y = sizeY - 1; }
        }
    }
}
