using System.Numerics;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Shaders;

namespace DirectX12Game
{
    [StaticResource]
    public class DissolveShader : IComputeColor
    {
        public void Visit(MaterialGeneratorContext context)
        {
            MainTexture.Visit(context);
            DissolveTexture.Visit(context);
            DissolveStrength.Visit(context);
        }

        public IComputeColor MainTexture { get; set; } = new ComputeColor(Vector4.One);

        public IComputeScalar DissolveTexture { get; set; } = new ComputeScalar(1.0f);

        public IComputeScalar DissolveStrength { get; set; } = new ComputeScalar(0.5f);

        [ShaderMember]
        public Vector4 Compute()
        {
            Vector4 colorBase = MainTexture.Compute();

            if (DissolveTexture.Compute() <= DissolveStrength.Compute())
            {
                colorBase.W = 0;
            }

            return colorBase;
        }
    }
}
