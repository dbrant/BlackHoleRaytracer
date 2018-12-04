using System;
using System.Drawing;

namespace BlackHoleRaytracer.Hitable
{
    public class CheckeredDisk : Disk
    {
        private Color topColor1;
        private Color topColor2;
        private Color bottomColor1;
        private Color bottomColor2;

        public CheckeredDisk(double radiusInner, double radiusOuter, Color topColor1, Color topColor2, Color bottomColor1, Color bottomColor2)
            : base(radiusInner, radiusOuter)
        {
            this.topColor1 = topColor1;
            this.topColor2 = topColor2;
            this.bottomColor1 = bottomColor1;
            this.bottomColor2 = bottomColor2;
        }

        protected override Color GetColor(int side, double r, double theta, double phi)
        {
            var m1 = Util.DoubleMod(phi, 1.04719);
            bool check = (m1 < 0.52359);
            return side == -1 ? (check ? topColor1 : topColor2) : (check ? bottomColor1 : bottomColor2);
        }
    }
}
