using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetVisibilitySmithSchlickGgx : IMaterialSpecularMicrofacetVisibilityFunction
    {
        public void Visit(MaterialGeneratorContext context)
        {
        }

        [ShaderMember]
        [ShaderMethod]
        public float Compute()
        {
            return BrdfMicrofacet.VisibilitySmithSchlickGgx(MaterialPixelStream.AlphaRoughness, LightStream.NDotL, MaterialPixelStream.NDotV);
        }
    }
}
