using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using Nito.AsyncEx;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialGenerator
    {
        private static readonly ConcurrentDictionary<Guid, AsyncLock> locks = new ConcurrentDictionary<Guid, AsyncLock>();

        public static async Task<Material> GenerateAsync(MaterialDescriptor descriptor, MaterialGeneratorContext context)
        {
            using (await locks.GetOrAdd(descriptor.MaterialId, new AsyncLock()).LockAsync())
            {
                context.PushMaterialDescriptor(descriptor);

                for (int passIndex = 0; passIndex < context.PassCount; passIndex++)
                {
                    MaterialPass materialPass = context.PushPass();

                    descriptor.Visit(context);

                    materialPass.PipelineState = await context.CreateGraphicsPipelineStateAsync();

                    if (context.ConstantBuffers.Count > 0)
                    {
                        materialPass.ConstantBufferDescriptorSet = new DescriptorSet(context.GraphicsDevice, context.ConstantBuffers);
                    }

                    if (context.Samplers.Count > 0)
                    {
                        materialPass.SamplerDescriptorSet = new DescriptorSet(context.GraphicsDevice, context.Samplers);
                    }

                    if (context.Textures.Count > 0)
                    {
                        materialPass.TextureDescriptorSet = new DescriptorSet(context.GraphicsDevice, context.Textures);
                    }

                    context.PopPass();
                }

                context.PopMaterialDescriptor();

                return context.Material;
            }
        }
    }
}
