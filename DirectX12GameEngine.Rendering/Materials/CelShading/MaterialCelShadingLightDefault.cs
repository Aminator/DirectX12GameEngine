using System.Numerics;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.CelShading
{
    [StaticResource]
    public class MaterialCelShadingLightDefault : IMaterialCelShadingLightFunction
    {
        private GraphicsBuffer<bool>? isBlackAndWhiteBuffer;

        public void Accept(ShaderGeneratorContext context)
        {
            isBlackAndWhiteBuffer ??= GraphicsBuffer.Create(context.GraphicsDevice, IsBlackAndWhite, ResourceFlags.None, GraphicsHeapType.Upload);
            context.ConstantBufferViews.Add(isBlackAndWhiteBuffer);
        }

        [ConstantBufferView]
        public bool IsBlackAndWhite { get; set; }

        [ShaderMethod]
        public Vector3 Compute(float LightIn)
        {
            if (IsBlackAndWhite)
            {
                if (LightIn > 0.2f)
                {
                    return Vector3.One;
                }
            }
            else
            {
                if (LightIn > 0.8f)
                {
                    return Vector3.One;
                }
                else if (LightIn > 0.5f)
                {
                    return Vector3.One * 0.8f;
                }
                else if (LightIn > 0.2f)
                {
                    return Vector3.One * 0.3f;
                }
            }

            return Vector3.Zero;
        }
    }
}
