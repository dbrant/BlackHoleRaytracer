using System.Drawing;
using System.Numerics;

namespace BlackHoleRaytracer.Gpu
{
    public enum GpuHitableKind
    {
        Sky,
        Horizon,
        Disk,
        Sphere,
        ReflectiveSphere
    }

    /// <summary>
    /// Location of a texture within the flat texture buffer uploaded to the GPU.
    /// A Width of zero means there is no texture.
    /// </summary>
    public struct GpuTexture
    {
        public int Offset;
        public int Width;
        public int Height;

        public readonly bool IsValid => Width > 0;
    }

    /// <summary>
    /// Flattened, GPU-friendly description of a hitable. GPU kernels cannot make
    /// virtual calls, so every hitable is converted to one of these, and the kernel
    /// switches on Kind. Only the fields relevant to each Kind are used.
    /// </summary>
    public struct GpuHitable
    {
        public GpuHitableKind Kind;

        // Sphere, ReflectiveSphere
        public Float3 Center;
        public float RadiusSqr;

        // Sky (OuterRadiusSqr only), Disk
        public float OuterRadiusSqr;
        public double InnerRadius;
        public double OuterRadius;

        // Checkered colors (ARGB). Spheres use Color1/Color2; disks use all four
        // (top1, top2, bottom1, bottom2).
        public int Color1;
        public int Color2;
        public int Color3;
        public int Color4;

        // Horizon. Stored as an int, since kernel parameters must be blittable (bool isn't).
        private int checkered;
        public bool Checkered { readonly get => checkered != 0; set => checkered = value ? 1 : 0; }

        public GpuTexture Texture;
        public double TextureOffset;

        public static GpuHitable Sky(double radius, GpuTexture texture)
        {
            return new GpuHitable
            {
                Kind = GpuHitableKind.Sky,
                OuterRadiusSqr = (float)(radius * radius),
                Texture = texture
            };
        }

        public static GpuHitable Horizon(bool checkered, GpuTexture texture)
        {
            return new GpuHitable
            {
                Kind = GpuHitableKind.Horizon,
                Checkered = checkered,
                Texture = texture
            };
        }

        public static GpuHitable Disk(double radiusInner, double radiusOuter, Color top1, Color top2, Color bottom1, Color bottom2, GpuTexture texture = default)
        {
            return new GpuHitable
            {
                Kind = GpuHitableKind.Disk,
                InnerRadius = radiusInner,
                OuterRadius = radiusOuter,
                Color1 = top1.ToArgb(),
                Color2 = top2.ToArgb(),
                Color3 = bottom1.ToArgb(),
                Color4 = bottom2.ToArgb(),
                Texture = texture
            };
        }

        public static GpuHitable Sphere(Vector3 center, float radius, Color color1, Color color2, GpuTexture texture = default, double textureOffset = 0)
        {
            return new GpuHitable
            {
                Kind = GpuHitableKind.Sphere,
                Center = center,
                RadiusSqr = radius * radius,
                Color1 = color1.ToArgb(),
                Color2 = color2.ToArgb(),
                Texture = texture,
                TextureOffset = textureOffset
            };
        }

        public static GpuHitable ReflectiveSphere(Vector3 center, float radius)
        {
            return new GpuHitable
            {
                Kind = GpuHitableKind.ReflectiveSphere,
                Center = center,
                RadiusSqr = radius * radius
            };
        }
    }
}
