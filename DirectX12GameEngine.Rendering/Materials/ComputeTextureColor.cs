using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class ComputeTextureColor : IComputeColor
    {
        public ComputeTextureColor()
        {
        }

        public ComputeTextureColor(Texture texture)
        {
            Texture = texture;
        }

        [IgnoreShaderMember]
        public Texture? Texture { get; set; }

        public void Accept(ShaderGeneratorContext context)
        {
            if (Texture != null)
            {
                context.ShaderResourceViews.Add(Texture);
            }
        }

#nullable disable
        public Texture2DResource ColorTexture;
#nullable restore

        [ShaderMethod]
        public Vector4 Compute()
        {
            return ColorTexture.Sample(Texturing.Sampler, Texturing.TexCoord);
        }
    }
}
