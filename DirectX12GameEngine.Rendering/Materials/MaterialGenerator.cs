using System;
using System.Linq;
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

                descriptor.Visit(context);

                materialPass.PipelineState = await context.CreateGraphicsPipelineStateAsync();

                var shaderResources = context.ConstantBufferViews.Concat(context.ShaderResourceViews).Concat(context.UnorderedAccessViews);

                if (shaderResources.Count() > 0)
                {
                    materialPass.ShaderResourceViewDescriptorSet = new DescriptorSet(context.GraphicsDevice, shaderResources.Count());
                    materialPass.ShaderResourceViewDescriptorSet.AddConstantBufferViews(context.ConstantBufferViews);
                    materialPass.ShaderResourceViewDescriptorSet.AddShaderResourceViews(context.ShaderResourceViews);
                    materialPass.ShaderResourceViewDescriptorSet.AddUnorderedAccessViews(context.UnorderedAccessViews);
                }

                if (context.Samplers.Count > 0)
                {
                    materialPass.SamplerDescriptorSet = new DescriptorSet(context.GraphicsDevice, context.Samplers.Count, DescriptorHeapType.Sampler);
                    materialPass.SamplerDescriptorSet.AddSamplers(context.Samplers);
                }

                context.PopPass();
            }

            context.PopMaterialDescriptor();

            return context.Material;
        }
    }
}
