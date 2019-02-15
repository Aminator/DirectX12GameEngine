using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class ComputeTextureScalar : IComputeScalar
    {
        public ComputeTextureScalar()
        {
        }

        public ComputeTextureScalar(Texture texture)
        {
            Texture = texture;
        }

        public Texture? Texture { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (Texture != null)
            {
                context.Textures.Add(Texture);
            }
        }

        #region Shader

#nullable disable
        [ShaderResource] public Texture2DResource ScalarTexture;
#nullable enable

        [ShaderMethod]
        public float Compute()
        {
            Vector4 color = ScalarTexture.Sample(Texturing.Sampler, Texturing.TexCoord);
            return color.X;
        }

        #endregion
    }
}
