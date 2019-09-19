using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ShaderContract]
    [StaticResource]
    public class ComputeTextureScalar : IComputeScalar
    {
        private ColorChannel channel;
        private GraphicsBuffer<ColorChannel>? colorChannelBuffer;

        public ComputeTextureScalar()
        {
        }

        public ComputeTextureScalar(Texture texture)
        {
            Texture = texture;
        }

        public ComputeTextureScalar(Texture texture, ColorChannel channel) : this(texture)
        {
            Channel = channel;
        }

        public Texture? Texture { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (Texture != null)
            {
                context.ShaderResourceViews.Add(Texture);
            }

            colorChannelBuffer ??= GraphicsBuffer.New(context.GraphicsDevice, Channel, BufferFlags.ConstantBuffer, GraphicsHeapType.Upload).DisposeBy(context.GraphicsDevice);
            context.ConstantBufferViews.Add(colorChannelBuffer);
        }

        #region Shader

#nullable disable
        [ShaderMember] public Texture2DResource ScalarTexture;
#nullable enable

        [ConstantBufferView] public ColorChannel Channel
        {
            get => channel;
            set
            {
                channel = value;
                colorChannelBuffer?.SetData(channel);
            }
        }

        [ShaderMember]
        public float Compute()
        {
            Vector4 color = ScalarTexture.Sample(Texturing.Sampler, Texturing.TexCoord);
            return color[(int)Channel];
        }

        #endregion
    }
}
