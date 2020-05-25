using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.CelShading
{
    [StaticResource]
    public class MaterialCelShadingLightDefault : IMaterialCelShadingLightFunction
    {
        private GraphicsResource? isBlackAndWhiteBuffer;

        public void Accept(ShaderGeneratorContext context)
        {
            isBlackAndWhiteBuffer ??= GraphicsResource.CreateBuffer(context.GraphicsDevice, IsBlackAndWhite, ResourceFlags.None, HeapType.Upload);
            context.ConstantBufferViews.Add(isBlackAndWhiteBuffer.DefaultConstantBufferView);
        }

        [ConstantBufferView]
        public bool IsBlackAndWhite { get; set; }

        [ShaderMethod]
        public Vector3 Compute(float lightIn)
        {
            if (IsBlackAndWhite)
            {
                if (lightIn > 0.2f)
                {
                    return Vector3.One;
                }
            }
            else
            {
                if (lightIn > 0.8f)
                {
                    return Vector3.One;
                }
                else if (lightIn > 0.5f)
                {
                    return Vector3.One * 0.8f;
                }
                else if (lightIn > 0.2f)
                {
                    return Vector3.One * 0.3f;
                }
            }

            return Vector3.Zero;
        }
    }
}
