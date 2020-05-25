using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering
{
    [ConstantBufferView]
    public class ColorShader : IColorShader
    {
        private Vector4 color;
        private GraphicsResource? colorBuffer;

        public ColorShader()
        {
        }

        public ColorShader(in Vector4 color)
        {
            Color = color;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            colorBuffer ??= GraphicsResource.CreateBuffer(context.GraphicsDevice, Color, ResourceFlags.None, HeapType.Upload);
            context.ConstantBufferViews.Add(colorBuffer.DefaultConstantBufferView);
        }

        public Vector4 Color
        {
            get => color;
            set
            {
                color = value;
                colorBuffer?.SetData(color);
            }
        }

        [ShaderMethod]
        public Vector4 ComputeColor(in SamplingContext context)
        {
            return Color;
        }
    }
}
