using System.Numerics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Shaders;

namespace DirectX12Game
{
    [StaticResource]
    public class DissolveShader : IColorShader
    {
        public void Accept(ShaderGeneratorContext context)
        {
            MainTexture.Accept(context);
            DissolveTexture.Accept(context);
            DissolveStrength.Accept(context);
        }

        public IColorShader MainTexture { get; set; } = new ColorShader(Vector4.One);

        public IScalarShader DissolveTexture { get; set; } = new ScalarShader(1.0f);

        public IScalarShader DissolveStrength { get; set; } = new ScalarShader(0.5f);

        [ShaderMethod]
        public Vector4 ComputeColor(in SamplingContext context)
        {
            Vector4 colorBase = MainTexture.ComputeColor(context);

            if (DissolveTexture.ComputeScalar(context) <= DissolveStrength.ComputeScalar(context))
            {
                colorBase.W = 0;
            }

            return colorBase;
        }
    }
}
