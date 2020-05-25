using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering
{
    [ConstantBufferView]
    public class ScalarShader : IScalarShader
    {
        private float scalarValue;
        private GraphicsResource? valueBuffer;

        public ScalarShader()
        {
        }

        public ScalarShader(float value)
        {
            Value = value;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            valueBuffer ??= GraphicsResource.CreateBuffer(context.GraphicsDevice, Value, ResourceFlags.None, HeapType.Upload);
            context.ConstantBufferViews.Add(valueBuffer.DefaultConstantBufferView);
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
        public float ComputeScalar(in SamplingContext context)
        {
            return Value;
        }
    }
}
