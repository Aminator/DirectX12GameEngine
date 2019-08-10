using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ConstantBufferResource]
    public class ComputeScalar : IComputeScalar
    {
        private float scalarValue;
        private Buffer<float>? valueBuffer;

        public ComputeScalar()
        {
        }

        public ComputeScalar(float value)
        {
            Value = value;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            valueBuffer ??= Buffer.Constant.New(context.GraphicsDevice, Value).DisposeBy(context.GraphicsDevice);
            context.ConstantBuffers.Add(valueBuffer);
        }

        #region Shader

        [ShaderResource]
        public float Value
        {
            get => scalarValue;
            set
            {
                scalarValue = value;
                valueBuffer?.SetData(scalarValue);
            }
        }

        [ShaderMethod]
        public float Compute()
        {
            return Value;
        }

        #endregion
    }
}
