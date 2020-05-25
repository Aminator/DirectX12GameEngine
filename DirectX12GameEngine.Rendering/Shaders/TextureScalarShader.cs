using System;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12GameEngine.Rendering
{
    [StaticResource]
    public class TextureScalarShader : IScalarShader
    {
        private ColorChannel channel;
        private GraphicsResource? colorChannelBuffer;

        public TextureScalarShader()
        {
        }

        public TextureScalarShader(Texture? texture, bool convertToLinear = false) : this(texture, ColorChannel.R, convertToLinear)
        {
        }

        public TextureScalarShader(Texture? texture, ColorChannel channel, bool convertToLinear = false)
        {
            Texture = texture;
            Channel = channel;
            ConvertToLinear = convertToLinear;
        }

        [IgnoreShaderMember]
        public Texture? Texture { get; set; }

        [IgnoreShaderMember]
        public bool ConvertToLinear { get; set; }

        public void Accept(ShaderGeneratorContext context)
        {
            if (Texture is null) throw new InvalidOperationException();

            ScalarTexture = new Texture2D(ShaderResourceView.FromTexture2D(Texture, ConvertToLinear ? Texture.Format.ToSrgb() : Texture.Format));
            context.ShaderResourceViews.Add(ScalarTexture);

            colorChannelBuffer ??= GraphicsResource.CreateBuffer(context.GraphicsDevice, Channel, ResourceFlags.None, HeapType.Upload);
            context.ConstantBufferViews.Add(colorChannelBuffer.DefaultConstantBufferView);
        }

        public Texture2D? ScalarTexture { get; private set; }

        [ConstantBufferView]
        public ColorChannel Channel
        {
            get => channel;
            set
            {
                channel = value;
                colorChannelBuffer?.SetData(channel);
            }
        }

#nullable disable

        [ShaderMethod]
        public float ComputeScalar(in SamplingContext context)
        {
            GraphicsVector4 color = ScalarTexture.Sample(context.Sampler, context.TextureCoordinate);
            return color[(int)Channel];
        }
    }
}
