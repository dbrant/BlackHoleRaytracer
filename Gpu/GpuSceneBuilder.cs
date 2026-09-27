using System;
using System.Collections.Generic;
using BlackHoleRaytracer.Hitable;

namespace BlackHoleRaytracer.Gpu
{
    /// <summary>
    /// Converts a list of hitables into flat arrays that can be uploaded to the GPU:
    /// an array of GpuHitable structs, and a single buffer holding all textures.
    /// </summary>
    public class GpuSceneBuilder
    {
        private readonly Dictionary<int[], GpuTexture> textures = new(ReferenceEqualityComparer.Instance);
        private readonly List<int[]> textureBitmaps = [];
        private int textureLength;

        public GpuTexture AddTexture(int[] bitmap, int width)
        {
            if (bitmap == null)
            {
                return default;
            }
            if (!textures.TryGetValue(bitmap, out GpuTexture texture))
            {
                texture = new GpuTexture { Offset = textureLength, Width = width, Height = bitmap.Length / width };
                textures[bitmap] = texture;
                textureBitmaps.Add(bitmap);
                textureLength = checked(textureLength + bitmap.Length);
            }
            return texture;
        }

        public static (GpuHitable[] hitables, int[] textureBuffer) Build(IReadOnlyList<IHitable> hitables)
        {
            var builder = new GpuSceneBuilder();
            var gpuHitables = new GpuHitable[hitables.Count];
            for (int i = 0; i < hitables.Count; i++)
            {
                gpuHitables[i] = hitables[i].ToGpu(builder);
            }

            // Never allocate a zero-length buffer, even if there are no textures.
            var textureBuffer = new int[Math.Max(1, builder.textureLength)];
            int offset = 0;
            foreach (var bitmap in builder.textureBitmaps)
            {
                Array.Copy(bitmap, 0, textureBuffer, offset, bitmap.Length);
                offset += bitmap.Length;
            }
            return (gpuHitables, textureBuffer);
        }
    }
}
