using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialGenerator
    {
        public static async Task<Material> GenerateAsync(MaterialDescriptor descriptor, MaterialGeneratorContext context)
        {
            context.PushMaterialDescriptor(descriptor);

            for (int passIndex = 0; passIndex < context.PassCount; passIndex++)
            {
                MaterialPass materialPass = context.PushPass();

                context.Visit(descriptor.Attributes);

                InputElementDescription[] inputElements = new[]
                {
                    new InputElementDescription("Position", 0, PixelFormat.R32G32B32Float, 0),
                    new InputElementDescription("Normal", 0, PixelFormat.R32G32B32Float, 1),
                    new InputElementDescription("Tangent", 0, PixelFormat.R32G32B32A32Float, 2),
                    new InputElementDescription("TexCoord", 0, PixelFormat.R32G32Float, 3)
                };

                materialPass.PipelineState = await context.CreateGraphicsPipelineStateAsync(inputElements);
                materialPass.ShaderResourceViewDescriptorSet = context.CreateShaderResourceViewDescriptorSet();
                materialPass.SamplerDescriptorSet = context.CreateSamplerDescriptorSet();

                context.PopPass();
            }

            context.PopMaterialDescriptor();

            return context.Material;
        }
    }
}
