using System.Numerics;

namespace DirectX12GameEngine
{
    public class MaterialAttributes : Shader
    {
        public void Visit(Material material)
        {
            Diffuse?.Visit(material);
        }

        #region Shader

        public IComputeColor? Diffuse { get; set; }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output;
            output.Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

            output.Color += Diffuse.Compute(input.TexCoord);

            return output;
        }

        #endregion Shader
    }
}
