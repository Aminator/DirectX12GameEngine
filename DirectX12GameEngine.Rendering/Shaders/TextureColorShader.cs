using System;
using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering
{
    [StaticResource]
    public class TextureColorShader : IColorShader
    {
        public TextureColorShader()
        {
        }

        public TextureColorShader(Texture? texture, bool convertToLinear = false)
        {
            Texture = texture;
            ConvertToLinear = convertToLinear;
        }

        [IgnoreShaderMember]
        public Texture? Texture { get; set; }

        [IgnoreShaderMember]
        public bool ConvertToLinear { get; set; }

        public void Accept(ShaderGeneratorContext context)
        {
            if (Texture is null) throw new InvalidOperationException();

            ColorTexture = new Texture2D(ShaderResourceView.FromTexture2D(Texture, ConvertToLinear ? Texture.Format.ToSrgb() : Texture.Format));
            context.ShaderResourceViews.Add(ShaderResourceView.FromTexture2D(Texture, ConvertToLinear ? Texture.Format.ToSrgb() : Texture.Format));
        }

        public Texture2D? ColorTexture { get; private set; }

#nullable disable

        [ShaderMethod]
        public Vector4 ComputeColor(in SamplingContext context)
        {
            return ColorTexture.Sample(context.Sampler, context.TextureCoordinate);
        }
    }
}
