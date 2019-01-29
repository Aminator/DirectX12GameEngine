using ShaderGen;
using System.Numerics;

namespace DirectX12GameEngine
{
    [Texture2DResource]
    public class ComputeTextureColor : Shader, IComputeColor
    {
        private readonly Texture texture;

        public ComputeTextureColor(Texture texture)
        {
            this.texture = texture;
        }

        public void Visit(Material material)
        {
            material.Textures.Add(texture);
        }

        #region Shader

#nullable disable
        public Texture2DResource ColorTexture;
#nullable enable

        [ShaderMethod]
        public Vector4 Compute(Vector2 texCoord)
        {
            return ColorTexture.Sample(Sampler, texCoord);
        }

        #endregion
    }
}
