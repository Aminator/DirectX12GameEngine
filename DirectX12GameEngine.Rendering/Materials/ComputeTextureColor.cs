using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ShaderContract]
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

        public Texture? Texture { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (Texture != null)
            {
                context.ShaderResourceViews.Add(Texture);
            }
        }

        #region Shader

#nullable disable
        [ShaderMember] public Texture2DResource ColorTexture;
#nullable enable

        [ShaderMember]
        public Vector4 Compute()
        {
            return ColorTexture.Sample(Texturing.Sampler, Texturing.TexCoord);
        }

        #endregion
    }
}
