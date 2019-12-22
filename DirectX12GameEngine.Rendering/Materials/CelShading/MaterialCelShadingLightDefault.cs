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

        public void Visit(MaterialGeneratorContext context)
        {
            isBlackAndWhiteBuffer ??= GraphicsBuffer.New(context.GraphicsDevice, IsBlackAndWhite, ResourceFlags.ConstantBuffer, GraphicsHeapType.Upload).DisposeBy(context.GraphicsDevice);
            context.ConstantBufferViews.Add(isBlackAndWhiteBuffer);
        }

        [ConstantBufferView]
        public bool IsBlackAndWhite { get; set; }

        [ShaderMember]
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
