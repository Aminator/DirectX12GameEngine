using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class ComputeTextureScalar : IComputeScalar
    {
        private Texture? colorChannelBuffer;

        public ComputeTextureScalar()
        {
        }

        public ComputeTextureScalar(Texture texture, ColorChannel channel = ColorChannel.R)
        {
            Texture = texture;
            Channel = channel;
        }

        public Texture? Texture { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (Texture != null)
            {
                context.Textures.Add(Texture);
            }

            colorChannelBuffer ??= Texture.CreateConstantBufferView(context.GraphicsDevice, Channel).DisposeBy(context.GraphicsDevice);
            context.ConstantBuffers.Add(colorChannelBuffer);
        }

        #region Shader

#nullable disable
        [ShaderResource] public Texture2DResource ScalarTexture;
#nullable enable

        [ConstantBufferResource] public ColorChannel Channel { get; set; } = ColorChannel.R;

        [ShaderMethod]
        public float Compute()
        {
            Numerics.Vector4 color = ScalarTexture.Sample(Texturing.Sampler, Texturing.TexCoord);
            return color[(int)Channel];
        }

        #endregion
    }
}
