using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ConstantBufferResource]
    public class ComputeScalar : IComputeScalar
    {
        private Texture? valueBuffer;

        public ComputeScalar()
        {
        }

        public ComputeScalar(float value)
        {
            Value = value;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            valueBuffer ??= Texture.CreateConstantBufferView(context.GraphicsDevice, Value).DisposeBy(context.GraphicsDevice);
            context.ConstantBuffers.Add(valueBuffer);
        }

        #region Shader

        [ShaderResource] public float Value { get; set; }

        [ShaderMethod]
        public float Compute()
        {
            return Value;
        }

        #endregion
    }
}
