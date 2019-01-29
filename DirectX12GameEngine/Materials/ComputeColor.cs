using System.Numerics;

namespace DirectX12GameEngine
{
    [UniformResource]
    public class ComputeColor : Shader, IComputeColor
    {
        private Texture? colorBuffer;

        public ComputeColor(in Vector4 color)
        {
            Color = color;
        }

        public void Visit(Material material)
        {
            colorBuffer ??= Texture.CreateConstantBufferView(material.GraphicsDevice, Color).DisposeBy(material.GraphicsDevice);

            material.Textures.Add(colorBuffer);
        }

        #region Shader

        public Vector4 Color;

        [ShaderMethod]
        public Vector4 Compute(Vector2 texCoord)
        {
            return Color;
        }

        #endregion
    }
}
