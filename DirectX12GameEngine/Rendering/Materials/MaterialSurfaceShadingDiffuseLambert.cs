using System.Numerics;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialSurfaceShadingDiffuseLambert : IMaterialSurfaceShading
    {
        public void Visit(MaterialGeneratorContext context)
        {
        }

        #region Shader

        [ShaderMethod]
        public Vector3 ComputeDirectLightContribution()
        {
            return MaterialPixelStream.MaterialDiffuseVisible * LightStream.LightColorNDotL;
        }

        [ShaderMethod]
        public Vector3 ComputeEnvironmentLightContribution()
        {
            return default;
        }

        #endregion
    }
}
