using System.Numerics;
using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering.Materials
{
    [Texture2DResource]
    public class ComputeTextureColor : MaterialShader, IComputeColor
    {
        public ComputeTextureColor()
        {
        }

        public ComputeTextureColor(Texture texture)
        {
            Texture = texture;
        }

        public Texture? Texture { get; set; }

        public void Visit(Material material)
        {
            if (Texture != null)
            {
                material.Textures.Add(Texture);
            }
        }

        #region Shader

#nullable disable
        public Texture2DResource ColorTexture;
#nullable enable

        [ShaderMethod]
        public Vector4 Compute(Vector2 texCoord)
        {
            return ColorTexture.Sample(Sampler, texCoord);
        }

        #endregion
    }
}
