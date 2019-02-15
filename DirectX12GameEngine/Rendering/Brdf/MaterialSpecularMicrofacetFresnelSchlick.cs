using System.Numerics;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetFresnelSchlick : IMaterialSpecularMicrofacetFresnelFunction
    {
        public void Visit(MaterialGeneratorContext context)
        {
        }

        #region Shader

        [ShaderMethod]
        public Vector3 Compute(Vector3 f0)
        {
            return BrdfMicrofacet.FresnelSchlick(f0, MaterialPixelShadingStream.LDotH);
        }

        #endregion
    }
}
