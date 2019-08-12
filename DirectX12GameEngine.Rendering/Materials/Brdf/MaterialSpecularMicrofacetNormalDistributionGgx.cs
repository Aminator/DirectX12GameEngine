using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetNormalDistributionGgx : IMaterialSpecularMicrofacetNormalDistributionFunction
    {
        public void Visit(MaterialGeneratorContext context)
        {
        }

        #region Shader

        [ShaderMember]
        public float Compute()
        {
            return BrdfMicrofacet.NormalDistributionGgx(MaterialPixelStream.AlphaRoughness, MaterialPixelShadingStream.NDotH);
        }

        #endregion
    }
}
