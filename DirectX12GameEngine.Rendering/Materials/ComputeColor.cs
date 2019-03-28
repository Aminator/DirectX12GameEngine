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
        private Texture? colorBuffer;

        public ComputeColor()
        {
        }

        public ComputeColor(in Vector4 color)
        {
            Color = color;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            colorBuffer ??= Texture.CreateConstantBufferView(context.GraphicsDevice, Color).DisposeBy(context.GraphicsDevice);
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

                if (colorBuffer != null)
                {
                    MemoryHelper.Copy(color, colorBuffer.MappedResource);
                }
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
