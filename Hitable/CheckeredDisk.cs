using System;
using System.Drawing;

namespace BlackHoleRaytracer.Hitable
{
    public class CheckeredDisk(double radiusInner, double radiusOuter, Color topColor1, Color topColor2, Color bottomColor1, Color bottomColor2) : Disk(radiusInner, radiusOuter)
    {
        private readonly Color topColor1 = topColor1;
        private readonly Color topColor2 = topColor2;
        private readonly Color bottomColor1 = bottomColor1;
        private readonly Color bottomColor2 = bottomColor2;

        protected override Color GetColor(int side, double r, double theta, double phi)
        {
            var m1 = Util.DoubleMod(phi, 1.04719);
            bool check = (m1 < 0.52359);
            return side == -1 ? (check ? topColor1 : topColor2) : (check ? bottomColor1 : bottomColor2);
        }
    }
}
