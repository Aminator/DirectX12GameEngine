using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ConstantBufferView]
    public class ComputeScalar : IComputeScalar
    {
        private float scalarValue;
        private GraphicsBuffer<float>? valueBuffer;

        public ComputeScalar()
        {
        }

        public ComputeScalar(float value)
        {
            Value = value;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            valueBuffer ??= GraphicsBuffer.Create(context.GraphicsDevice, Value, ResourceFlags.None, GraphicsHeapType.Upload);
            context.ConstantBufferViews.Add(valueBuffer);
        }

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
    }
}
