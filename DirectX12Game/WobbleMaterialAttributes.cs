using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Shaders;

namespace DirectX12Game
{
    [StaticResource]
    public class WobbleMaterialAttributes : MaterialAttributes
    {
        public override void Visit(MaterialGeneratorContext context)
        {
            base.Visit(context);

            WobbleStrength.Visit(context);
        }

        #region Shader

        [ShaderResource] public IComputeScalar WobbleStrength { get; set; } = new ComputeScalar(2.0f);

        [Shader("vertex")]
        public override VSOutput VSMain(VSInput input)
        {
            float wobbleOffset = WobbleStrength.Compute() * (float)Math.Sin(Globals.TotalTime * 10.0f);
            Vector3 positionWobble = input.Position + input.Normal * new Vector3(wobbleOffset, wobbleOffset, wobbleOffset);

            input.Position = positionWobble;

            return base.VSMain(input);
        }

        #endregion
    }
}
