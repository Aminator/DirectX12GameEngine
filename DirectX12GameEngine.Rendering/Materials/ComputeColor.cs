using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ConstantBufferView]
    public class ComputeColor : IComputeColor
    {
        private Vector4 color;
        private GraphicsBuffer<Vector4>? colorBuffer;

        public ComputeColor()
        {
        }

        public ComputeColor(in Vector4 color)
        {
            Color = color;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            colorBuffer ??= GraphicsBuffer.Create(context.GraphicsDevice, Color, ResourceFlags.None, GraphicsHeapType.Upload);
            context.ConstantBufferViews.Add(colorBuffer);
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
        public Vector4 Compute()
        {
            return Color;
        }
    }
}
