using System.Numerics;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ConstantBufferResource]
    public class ComputeColor : IComputeColor
    {
        private Vector4 color;
        private Buffer? colorBuffer;

        public ComputeColor()
        {
        }

        public ComputeColor(in Vector4 color)
        {
            Color = color;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            colorBuffer ??= Buffer.Constant.New(context.GraphicsDevice, Color).DisposeBy(context.GraphicsDevice);
            context.ConstantBuffers.Add(colorBuffer);
        }

        #region Shader

        [ShaderResource]
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

        #endregion
    }
}
