using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
                    (materialPass.NativeConstantBufferCpuDescriptorHandle, materialPass.NativeConstantBufferGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.ConstantBuffers);
                    (materialPass.NativeSamplerCpuDescriptorHandle, materialPass.NativeSamplerGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.Samplers);
                    (materialPass.NativeTextureCpuDescriptorHandle, materialPass.NativeTextureGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.Textures);

                    context.PopPass();
                }

                context.PopMaterialDescriptor();

                return context.Material;
            }
        }
    }
}
